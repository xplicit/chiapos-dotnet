using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using chiapos_dotnet;
using Chiapos.Dotnet.Disks;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Phase1
    {
        public static readonly GlobalData globals = new();

        // This is Phase 1, or forward propagation. During this phase, all of the 7 tables,
// and f functions, are evaluated. The result is an intermediate plot file, that is
// several times larger than what the final file will be, but that has all of the
// proofs of space in it. First, F1 is computed, which is special since it uses
// ChaCha8, and each encryption provides multiple output values. Then, the rest of the
// f functions are computed, and a sort on disk happens for each table.
        public List<ulong> RunPhase1(
            List<FileDisk> tmp_1_disks,
            byte k,
            byte[] id,
            string tmp_dirname,
            string filename,
            ulong memory_size,
            uint num_buckets,
            uint log_num_buckets,
            uint stripe_size,
            int num_threads,
            PhaseFlags flags)
        {
            Console.WriteLine($"{Environment.NewLine}Computing table 1");
            globals.stripe_size = stripe_size;
            globals.num_threads = num_threads;
            PerformanceTimer f1_start_time = new();
            F1Calculator f1 = new F1Calculator(k, id);
            ulong x = 0;

            uint t1_entry_size_bytes = (uint) EntrySizes.GetMaxEntrySize(k, 1, true);
            globals.L_sort_manager = new SortManager(
                memory_size,
                num_buckets,
                log_num_buckets,
                (ushort) t1_entry_size_bytes,
                tmp_dirname,
                filename + ".p1.t1",
                0,
                globals.stripe_size);

            // These are used for sorting on disk. The sort on disk code needs to know how
            // many elements are in each bucket.
            var table_sizes = new List<ulong> {0, 0, 0, 0, 0, 0, 0, 0};
            ManualResetEvent[] completion = new ManualResetEvent[num_threads];

#if !SKIPF1
            {
                // Start of parallel execution
                var threads = new List<Thread>();
                for (int i = 0; i < num_threads; i++)
                {
                    var index = i;
                    completion[i] = new ManualResetEvent(false);
                    ThreadPool.QueueUserWorkItem(_ => F1thread(index, k, id, completion[index]));
                }

                ManualResetEvent.WaitAll(completion);

                // end of parallel execution
            }

            globals.L_sort_manager.FlushCache();
            f1_start_time.PrintElapsed("F1 complete, time:");
#endif
            ulong prevtableentries = 1UL << k;
            table_sizes[1] = x + 1;

            // Store positions to previous tables, in k bits.
            byte pos_size = k;
            uint right_entry_size_bytes = 0;

            // For tables 1 through 6, sort the table, calculate matches, and write
            // the next table. This is the left table index.
            for (byte table_index = 1; table_index < 7; table_index++)
            {
                PerformanceTimer table_timer = new();
                byte metadata_size = (byte) (Constants.kVectorLens[table_index + 1] * k);

                // Determines how many bytes the entries in our left and right tables will take up.
                uint entry_size_bytes = (uint) EntrySizes.GetMaxEntrySize(k, table_index, true);
                uint compressed_entry_size_bytes = (uint) EntrySizes.GetMaxEntrySize(k, table_index, false);
                right_entry_size_bytes = (uint) EntrySizes.GetMaxEntrySize(k, (byte) (table_index + 1), true);

                if (flags.HasFlag(PhaseFlags.EnableBitfield) && table_index != 1)
                {
                    // We only write pos and offset to tables 2-6 after removing
                    // metadata
                    compressed_entry_size_bytes = (uint) Util.Cdiv(k + Constants.kOffsetSize, 8);
                    if (table_index == 6)
                    {
                        // Table 7 will contain f7, pos and offset
                        right_entry_size_bytes = (uint) EntrySizes.GetKeyPosOffsetSize(k);
                    }
                }

                Console.WriteLine($"Computing table {table_index + 1}");
                // Start of parallel execution

                FxCalculator f = new FxCalculator(k, (byte)(table_index +1)); // dummy to load static table

                globals.matches = 0;
                globals.left_writer_count = 0;
                globals.right_writer_count = 0;
                globals.right_writer = 0;
                globals.left_writer = 0;

                globals.R_sort_manager = new SortManager(
                    memory_size,
                    num_buckets,
                    log_num_buckets,
                    (ushort) right_entry_size_bytes,
                    tmp_dirname,
                    filename + $".p1.t{table_index + 1}",
                    0,
                    globals.stripe_size);

                globals.L_sort_manager.TriggerNewBucket(0);

                PerformanceTimer computation_pass_timer;

                var td = new ThreadData[num_threads];
                var mutex = new AutoResetEvent[num_threads];

                var phase1_num_threads = 1;
                var threads = new List<Thread>();

                for (int i = 0; i < phase1_num_threads; i++)
                {
                    mutex[i] = new AutoResetEvent(false);
                }

                for (int i = 0; i < phase1_num_threads; i++)
                {
                    td[i] = new ThreadData();
                    td[i].phase1_num_threads = phase1_num_threads;
                    td[i].index = i;
                    td[i].mine = mutex[i];
                    td[i].theirs = mutex[(phase1_num_threads + i - 1) % phase1_num_threads];

                    td[i].prevtableentries = prevtableentries;
                    td[i].right_entry_size_bytes = right_entry_size_bytes;
                    td[i].k = k;
                    td[i].table_index = table_index;
                    td[i].metadata_size = metadata_size;
                    td[i].entry_size_bytes = entry_size_bytes;
                    td[i].pos_size = pos_size;
                    td[i].compressed_entry_size_bytes = compressed_entry_size_bytes;
                    td[i].ptmp_1_disks = tmp_1_disks;
                    var threadData = td[i];

                    var thread = new Thread(() => phase1_thread(threadData));
                    threads.Add(thread);
                    thread.Start();
                }

                mutex[phase1_num_threads - 1].Set();

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                for (int i = 0; i < phase1_num_threads; i++)
                {
                    mutex[i].Dispose();
                }

                // end of parallel execution

                // Total matches found in the left table
                Console.WriteLine($"\tTotal matches: {globals.matches}");

                table_sizes[table_index] = globals.left_writer_count;
                table_sizes[table_index + 1] = globals.right_writer_count;

                // Truncates the file after the final write position, deleting no longer useful
                // working space
                tmp_1_disks[table_index].Truncate(globals.left_writer);
                globals.L_sort_manager.Reset();
                if (table_index < 6)
                {
                    globals.R_sort_manager.FlushCache();
                    globals.L_sort_manager = globals.R_sort_manager;
                }
                else
                {
                    tmp_1_disks[table_index + 1].Truncate(globals.right_writer);
                }

                // Resets variables
                if (globals.matches != globals.right_writer_count)
                {
                    throw new InvalidOperationException(
                        $"Matches do not match with number of write entries {globals.matches} {globals.right_writer_count}");
                }

                prevtableentries = globals.right_writer_count;
                table_timer.PrintElapsed("Forward propagation table time:");
                if (flags.HasFlag(PhaseFlags.ShowProgress))
                {
                    ProgressNotificator.ShowProgress(1, table_index, 6);
                }
            }

            table_sizes[0] = 0;
            globals.R_sort_manager.Reset();
            return table_sizes;
        }

        void F1thread(int index, byte k, byte[] id, ManualResetEvent completion)
        {
            uint entry_size_bytes = 16;
            ulong max_value = 1UL << k;
            ulong right_buf_entries = 1UL << (int) Constants.kBatchSizes;

            ulong[] f1_entries = new ulong[1U << (int) Constants.kBatchSizes];

            F1Calculator f1 = new F1Calculator(k, id);

            byte[] right_writer_buf = new byte[right_buf_entries * entry_size_bytes];

            // Instead of computing f1(1), f1(2), etc, for each x, we compute them in batches
            // to increase CPU efficency.
            for (ulong lp = (ulong) index;
                lp <= 1UL << (k - (int) Constants.kBatchSizes);
                lp = lp + (ulong) globals.num_threads)
            {
                // For each pair x, y in the batch

                ulong right_writer_count = 0;
                ulong x = lp * (1 << (int) Constants.kBatchSizes);

                ulong loopcount = Math.Min(max_value - x, 1UL << (int) Constants.kBatchSizes);

                // Instead of computing f1(1), f1(2), etc, for each x, we compute them in batches
                // to increase CPU efficency.
                f1.CalculateBuckets(x, loopcount, f1_entries);
                for (uint i = 0; i < loopcount; i++)
                {
                    UInt128 entry;

                    entry = (UInt128) f1_entries[i] << (128 - Constants.kExtraBits - k);
                    entry |= (UInt128) x << (128 - Constants.kExtraBits - 2 * k);
                    Util.IntTo16Bytes(
                        new Span<byte>(right_writer_buf, (int) (i * entry_size_bytes), (int) entry_size_bytes),
                        entry);
                    right_writer_count++;
                    x++;
                }

                // Write it out
                for (uint i = 0; i < right_writer_count; i++)
                {
                    globals.L_sort_manager.AddToCache(
                        new ReadOnlySpan<byte>(right_writer_buf, (int) (i * entry_size_bytes),
                            (int) entry_size_bytes));
                }
            }

            completion.Set();
        }

        void phase1_thread(ThreadData ptd)
        {
            PerformanceTimer perfTimer = new();
            
            ulong right_entry_size_bytes = ptd.right_entry_size_bytes;
            byte k = ptd.k;
            byte table_index = ptd.table_index;
            byte metadata_size = ptd.metadata_size;
            uint entry_size_bytes = ptd.entry_size_bytes;
            byte pos_size = ptd.pos_size;
            ulong prevtableentries = ptd.prevtableentries;
            uint compressed_entry_size_bytes = ptd.compressed_entry_size_bytes;
            var ptmp_1_disks = ptd.ptmp_1_disks;
            var num_threads = ptd.phase1_num_threads;

            // Streams to read and right to tables. We will have handles to two tables. We will
            // read through the left table, compute matches, and evaluate f for matching entries,
            // writing results to the right table.
            ulong left_buf_entries = 5000 + (ulong) ((1.1) * (globals.stripe_size));
            ulong right_buf_entries = 5000 + (ulong) ((1.1) * (globals.stripe_size));
            byte[] right_writer_buf = new byte[right_buf_entries * right_entry_size_bytes + 7];
            byte[] left_writer_buf = new byte[left_buf_entries * compressed_entry_size_bytes + 7];

            FxCalculator f = new FxCalculator(k, (byte)(table_index + 1));

            // Stores map of old positions to new positions (positions after dropping entries from L
            // table that did not match) Map ke
            ushort position_map_size = 2000;

            // Should comfortably fit 2 buckets worth of items
            ushort[] L_position_map = new ushort[position_map_size];
            ushort[] R_position_map = new ushort[position_map_size];

            // Start at left table pos = 0 and iterate through the whole table. Note that the left table
            // will already be sorted by y
            ulong totalstripes = (prevtableentries + globals.stripe_size - 1) / globals.stripe_size;
            ulong threadstripes = (totalstripes + (ulong) num_threads - 1) / (ulong) num_threads;
            
            var idx_L = new ushort[10000];
            var idx_R = new ushort[10000];

            for (ulong stripe = 0; stripe < threadstripes; stripe++)
            {
                ulong pos = (stripe * (ulong)num_threads + (ulong)ptd.index) * globals.stripe_size;
                ulong endpos = pos + globals.stripe_size + 1; // one y value overlap
                ulong left_reader = pos * entry_size_bytes;
                ulong left_writer_count = 0;
                ulong stripe_left_writer_count = 0;
                ulong stripe_start_correction = 0xffffffffffffffff;
                ulong right_writer_count = 0;
                ulong matches = 0; // Total matches

                // This is a sliding window of entries, since things in bucket i can match with things in
                // bucket
                // i + 1. At the end of each bucket, we find matches between the two previous buckets.
                var bucket_L = new List<PlotEntry>(1000);
                var bucket_R = new List<PlotEntry>(1000);

                ulong bucket = 0;
                bool end_of_table = false; // We finished all entries in the left table

                ulong ignorebucket = 0xffffffffffffffff;
                bool bMatch = false;
                bool bFirstStripeOvertimePair = false;
                bool bSecondStripOvertimePair = false;
                bool bThirdStripeOvertimePair = false;

                bool bStripePregamePair = false;
                bool bStripeStartPair = false;
                bool need_new_bucket = false;
                bool first_thread = ptd.index % num_threads == 0;
                bool last_thread = ptd.index % num_threads == num_threads - 1;

                ulong L_position_base = 0;
                ulong R_position_base = 0;
                ulong newlpos = 0;
                ulong newrpos = 0;
                List<Tuple<PlotEntry, PlotEntry, ValueTuple<Bits2, Bits2>>> current_entries_to_write = new();
                List<Tuple<PlotEntry, PlotEntry, ValueTuple<Bits2, Bits2>>> future_entries_to_write = new();
                List<PlotEntry> not_dropped = new(); // Pointers are stored to avoid copying entries

                if (pos == 0)
                {
                    bMatch = true;
                    bStripePregamePair = true;
                    bStripeStartPair = true;
                    stripe_left_writer_count = 0;
                    stripe_start_correction = 0;
                }

                ptd.theirs.WaitOne();
                need_new_bucket = globals.L_sort_manager.CloseToNewBucket(left_reader);
                if (need_new_bucket)
                {
                    if (!first_thread)
                    {
                        ptd.theirs.WaitOne();
                    }

                    perfTimer.ResetAndPrintElapsed("\tProcessing bucket");
                    globals.L_sort_manager.TriggerNewBucket(left_reader);
                }

                if (!last_thread)
                {
                    // Do not post if we are the last thread, because first thread has already
                    // waited for us to finish when it starts
                    ptd.mine.Set();
                }

                while (pos < prevtableentries + 1)
                {
                    PlotEntry left_entry = new PlotEntry();
                    if (pos >= prevtableentries)
                    {
                        end_of_table = true;
                        left_entry.y = 0;
                        left_entry.left_metadata = null;
                        left_entry.used = false;
                    }
                    else
                    {
                        // Reads a left entry from disk
                        var left_buf = globals.L_sort_manager.ReadEntry(left_reader);
                        left_reader += entry_size_bytes;

                        left_entry = GetLeftEntry(table_index, left_buf, k, metadata_size, pos_size);
                    }

                    // This is not the pos that was read from disk,but the position of the entry we read,
                    // within L table.
                    left_entry.pos = pos;
                    left_entry.used = false;
                    ulong y_bucket = left_entry.y / Constants.kBC;

                    if (!bMatch)
                    {
                        if (ignorebucket == 0xffffffffffffffff)
                        {
                            ignorebucket = y_bucket;
                        }
                        else
                        {
                            if ((y_bucket != ignorebucket))
                            {
                                bucket = y_bucket;
                                bMatch = true;
                            }
                        }
                    }

                    if (!bMatch)
                    {
                        stripe_left_writer_count++;
                        R_position_base = stripe_left_writer_count;
                        pos++;
                        continue;
                    }
                    
                    // Keep reading left entries into bucket_L and R, until we run out of things
                    if (y_bucket == bucket)
                    {
                        bucket_L.Add(left_entry);
                    }
                    else if (y_bucket == bucket + 1)
                    {
                        bucket_R.Add(left_entry);
                    }
                    else
                    {
                        // cout << "matching! " << bucket << " and " << bucket + 1 << endl;
                        // This is reached when we have finished adding stuff to bucket_R and bucket_L,
                        // so now we can compare entries in both buckets to find matches. If two entries
                        // match, match, the result is written to the right table. However the writing
                        // happens in the next iteration of the loop, since we need to remap positions.
                        int idx_count = 0;

                        if (bucket_L.Count > 0)
                        {
                            not_dropped.Clear();

                            if (bucket_R.Count > 0)
                            {
                                // Compute all matches between the two buckets and save indeces.
                                idx_count = f.FindMatches(bucket_L, bucket_R, idx_L, idx_R);
                                if (idx_count >= 10000)
                                {
                                    Console.WriteLine("sanity check: idx_count exceeded 10000!");
                                    Environment.Exit(0);
                                }

                                // We mark entries as used if they took part in a match.
                                for (int i = 0; i < idx_count; i++)
                                {
                                    bucket_L[idx_L[i]].used = true;
                                    if (end_of_table)
                                    {
                                        bucket_R[idx_R[i]].used = true;
                                    }
                                }
                            }

                            // Adds L_bucket entries that are used to not_dropped. They are used if they
                            // either matched with something to the left (in the previous iteration), or
                            // matched with something in bucket_R (in this iteration).
                            for (int bucket_index = 0; bucket_index < bucket_L.Count; bucket_index++)
                            {
                                PlotEntry L_entry = bucket_L[bucket_index];
                                if (L_entry.used)
                                {
                                    not_dropped.Add(bucket_L[bucket_index]);
                                }
                            }

                            if (end_of_table)
                            {
                                // In the last two buckets, we will not get a chance to enter the next
                                // iteration due to breaking from loop. Therefore to write the final
                                // bucket in this iteration, we have to add the R entries to the
                                // not_dropped list.
                                for (int bucket_index = 0;
                                    bucket_index < bucket_R.Count;
                                    bucket_index++)
                                {
                                    PlotEntry R_entry = bucket_R[bucket_index];
                                    if (R_entry.used)
                                    {
                                        not_dropped.Add(R_entry);
                                    }
                                }
                            }

                            // We keep maps from old positions to new positions. We only need two maps,
                            // one for L bucket and one for R bucket, and we cycle through them. Map
                            // keys are stored as positions % 2^10 for efficiency. Map values are stored
                            // as offsets from the base position for that bucket, for efficiency.
                            var tmp = L_position_map;
                            L_position_map = R_position_map;
                            R_position_map = tmp;
                            L_position_base = R_position_base;
                            R_position_base = stripe_left_writer_count;

                            foreach (PlotEntry entry in not_dropped)
                            {
                                // The new position for this entry = the total amount of thing written
                                // to L so far. Since we only write entries in not_dropped, about 14% of
                                // entries are dropped.
                                R_position_map[entry.pos % position_map_size] =
                                    (ushort) (stripe_left_writer_count - R_position_base);

                                if (bStripeStartPair)
                                {
                                    if (stripe_start_correction == 0xffffffffffffffff)
                                    {
                                        stripe_start_correction = stripe_left_writer_count;
                                    }

                                    if (left_writer_count >= left_buf_entries)
                                    {
                                        throw new InvalidOperationException("Left writer count overrun");
                                    }

                                    var tmp_buf = new Span<byte>(left_writer_buf)
                                        .Slice((int) (left_writer_count * compressed_entry_size_bytes));


                                    left_writer_count++;
                                    // memset(tmp_buf, 0xff, compressed_entry_size_bytes);

                                    // Rewrite left entry with just pos and offset, to reduce working space
                                    ulong new_left_entry;
                                    if (table_index == 1)
                                        new_left_entry = entry.left_metadata.GetValue();
                                    else
                                        new_left_entry = entry.read_posoffset;
                                    new_left_entry <<=
                                        (int) (64 - (table_index == 1 ? k : pos_size + Constants.kOffsetSize));
                                    Util.IntToEightBytes(tmp_buf, new_left_entry);
                                }

                                stripe_left_writer_count++;
                            }

                            // Two vectors to keep track of things from previous iteration and from this
                            // iteration.
                            current_entries_to_write = future_entries_to_write.ToList();
                            future_entries_to_write.Clear();

                            for (int i = 0; i < idx_count; i++)
                            {
                                PlotEntry L_entry = bucket_L[idx_L[i]];
                                PlotEntry R_entry = bucket_R[idx_R[i]];

                                if (bStripeStartPair)
                                    matches++;

                                // Sets the R entry to used so that we don't drop in next iteration
                                R_entry.used = true;
                                // Computes the output pair (fx, new_metadata)
                                ValueTuple<Bits2, Bits2> f_output = f.CalculateBucket(
                                    new Bits2(L_entry.y, k + Constants.kExtraBits),
                                    L_entry.left_metadata,
                                    R_entry.left_metadata);
                                future_entries_to_write.Add(
                                    new Tuple<PlotEntry, PlotEntry, (Bits2, Bits2)>(L_entry, R_entry, f_output));
                            }

                            // At this point, future_entries_to_write contains the matches of buckets L
                            // and R, and current_entries_to_write contains the matches of L and the
                            // bucket left of L. These are the ones that we will write.
                            ushort final_current_entry_size = (ushort) current_entries_to_write.Count;
                            if (end_of_table)
                            {
                                // For the final bucket, write the future entries now as well, since we
                                // will break from loop
                                current_entries_to_write.AddRange(future_entries_to_write);
                            }

                            for (int i = 0; i < current_entries_to_write.Count; i++)
                            {
                                var (L_entry, R_entry, f_output) = current_entries_to_write[i];

                                // Maps the new positions. If we hit end of pos, we must write things in
                                // both final_entries to write and current_entries_to_write, which are
                                // in both position maps.
                                if (!end_of_table || i < final_current_entry_size)
                                {
                                    newlpos =
                                        L_position_map[L_entry.pos % position_map_size] + L_position_base;
                                }
                                else
                                {
                                    newlpos =
                                        R_position_map[L_entry.pos % position_map_size] + R_position_base;
                                }

                                newrpos = R_position_map[R_entry.pos % position_map_size] + R_position_base;

                                // Offset for matching entry
                                if (newrpos - newlpos > (1U << (int) Constants.kOffsetSize) * 97 / 100)
                                {
                                    throw new InvalidOperationException($"Offset too large: {newrpos - newlpos}");
                                }

                                if (right_writer_count >= right_buf_entries)
                                {
                                    throw new InvalidOperationException("Left writer count overrun");
                                }

                                if (bStripeStartPair)
                                {
                                    // We only need k instead of k + kExtraBits bits for the last table
                                    var dstBuffer = right_writer_buf.AsSpan().Slice((int)(right_writer_count * right_entry_size_bytes));
                                    int bits = Bits2.WriteBytesToBuffer(dstBuffer, 0,
                                        f_output.Item1.GetBuffer(), table_index + 1 == 7 ? k : f_output.Item1.Length);
                                    bits = Bits2.WriteBytesToBuffer(dstBuffer, bits, newlpos, pos_size);
                                    bits = Bits2.WriteBytesToBuffer(dstBuffer, bits, newrpos - newlpos,
                                        (int)Constants.kOffsetSize);
                                    if (f_output.Item2 != null)
                                    {
                                        bits = Bits2.WriteBytesToBuffer(dstBuffer, bits, f_output.Item2.GetBuffer(),
                                            f_output.Item2.Length);
                                    }
                                    right_writer_count++;
                                }
                            }
                        }

                        if (pos >= endpos)
                        {
                            if (!bFirstStripeOvertimePair)
                                bFirstStripeOvertimePair = true;
                            else if (!bSecondStripOvertimePair)
                                bSecondStripOvertimePair = true;
                            else if (!bThirdStripeOvertimePair)
                                bThirdStripeOvertimePair = true;
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (!bStripePregamePair)
                                bStripePregamePair = true;
                            else if (!bStripeStartPair)
                                bStripeStartPair = true;
                        }

                        if (y_bucket == bucket + 2)
                        {
                            // We saw a bucket that is 2 more than the current, so we just set L = R, and R
                            // = [entry]
                            bucket_L = bucket_R.ToList();
                            bucket_R.Clear();
                            bucket_R.Add(left_entry);
                            ++bucket;
                        }
                        else
                        {
                            // We saw a bucket that >2 more than the current, so we just set L = [entry],
                            // and R = []
                            bucket = y_bucket;
                            bucket_L.Clear();
                            bucket_L.Add(left_entry);
                            bucket_R.Clear();
                        }
                    }

                    // Increase the read pointer in the left table, by one
                    ++pos;
                }

                // If we needed new bucket, we already waited
                // Do not wait if we are the first thread, since we are guaranteed that everything is written
                if (!need_new_bucket && !first_thread)
                {
                    ptd.theirs.WaitOne();
                }

                uint ysize = (table_index + 1 == 7) ? k : k + (uint) Constants.kExtraBits;
                uint startbyte = ysize / 8;
                uint endbyte = (ysize + pos_size + 7) / 8 - 1;
                ulong shiftamt = (8 - ((ysize + pos_size) % 8)) % 8;
                ulong correction = (globals.left_writer_count - stripe_start_correction) << (int)shiftamt;

                // Correct positions
                for (uint i = 0; i < right_writer_count; i++)
                {
                    ulong posaccum = 0;
                    var entrybuf = new Span<byte>(right_writer_buf).Slice((int) (i * right_entry_size_bytes));

                    for (int j = (int)startbyte; j <= endbyte; j++)
                    {
                        posaccum = (posaccum << 8) | (entrybuf[j]);
                    }

                    posaccum += correction;
                    for (int j = (int)endbyte; j >= startbyte; --j)
                    {
                        entrybuf[j] = (byte)(posaccum & 0xff);
                        posaccum = posaccum >> 8;
                    }
                }

                if (table_index < 6)
                {
                    for (ulong i = 0; i < right_writer_count; i++)
                    {
                        globals.R_sort_manager.AddToCache(
                            new ReadOnlySpan<byte>(right_writer_buf).Slice((int) (i * right_entry_size_bytes)));
                    }
                }
                else
                {
                    // Writes out the right table for table 7
                    ptmp_1_disks[table_index + 1].Write(globals.right_writer,
                        new ReadOnlySpan<byte>(right_writer_buf, 0, (int) (right_writer_count * right_entry_size_bytes)));
                }

                globals.right_writer += right_writer_count * right_entry_size_bytes;
                globals.right_writer_count += right_writer_count;

                ptmp_1_disks[table_index].Write(globals.left_writer,
                    new ReadOnlySpan<byte>(left_writer_buf, 0, (int) (left_writer_count * compressed_entry_size_bytes)));
                globals.left_writer += left_writer_count * compressed_entry_size_bytes;
                globals.left_writer_count += left_writer_count;

                globals.matches += matches;
                ptd.mine.Set();
            }
        }

        PlotEntry GetLeftEntry(
            byte table_index,
        ReadOnlySpan<byte> left_buf,
        byte k,
        byte metadata_size,
        byte pos_size)
        {
            PlotEntry left_entry = new PlotEntry {y = 0, read_posoffset = 0, left_metadata = null};

            uint ysize = (table_index == 7) ? k : k + (uint)Constants.kExtraBits;

            if (table_index == 1) {
                // For table 1, we only have y and metadata
                left_entry.y = Util.SliceInt64FromBytes(left_buf, 0, k + (uint)Constants.kExtraBits);
                left_entry.left_metadata = new Bits2(left_buf, (int)(k + (uint)Constants.kExtraBits), metadata_size);
            } else {
                // For tables 2-6, we we also have pos and offset. We need to read this because
                // this entry will be written again to the table without the y (and some entries
                // are dropped).
                left_entry.y = Util.SliceInt64FromBytes(left_buf, 0, ysize);
                left_entry.read_posoffset =
                    Util.SliceInt64FromBytes(left_buf, ysize, pos_size + Constants.kOffsetSize);
                left_entry.left_metadata =
                    new Bits2(left_buf, (int)(ysize + pos_size + Constants.kOffsetSize), metadata_size);
            }
            return left_entry;
        }
    }
}