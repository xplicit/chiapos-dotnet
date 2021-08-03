using System;
using System.Collections.Generic;
using System.Linq;
using chiapos_dotnet;
using Chiapos.Dotnet.Disks;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Phase2
    {
        // Backpropagate takes in as input, a file on which forward propagation has been done.
// The purpose of backpropagate is to eliminate any dead entries that don't contribute
// to final values in f7, to minimize disk usage. A sort on disk is applied to each table,
// so that they are sorted by position.
        public Phase2Results RunPhase2(
            List<FileDisk> tmp_1_disks,
            List<ulong> table_sizes,
            byte k,
            byte[] id,
            string tmp_dirname,
            string filename,
            ulong memory_size,
            uint num_buckets,
            uint log_num_buckets,
            PhaseFlags flags)
        {
            // After pruning each table will have 0.865 * 2^k or fewer entries on
            // average
            byte pos_size = k;
            byte pos_offset_size = (byte) (pos_size + Constants.kOffsetSize);
            byte write_counter_shift = (byte) (128 - k);
            byte pos_offset_shift = (byte) (write_counter_shift - pos_offset_size);
            byte f7_shift = (byte) (128 - k);
            byte t7_pos_offset_shift = (byte) (f7_shift - pos_offset_size);
            byte new_entry_size = (byte)EntrySizes.GetKeyPosOffsetSize(k);

            var new_table_sizes = new List<ulong> {0, 0, 0, 0, 0, 0, 0, table_sizes[7]};

            // Iterates through each table, starting at 6 & 7. Each iteration, we scan
            // the current table twice. In the first scan, we:

            // 1. drop entries marked as false in the current bitfield (except table 7,
            //    where we don't drop anything, this is a special case)
            // 2. mark entries in the next_bitfield that non-dropped entries have
            //    references to

            // The second scan of the table, we update the positions and offsets to
            // reflect the entries that will be dropped in the next table.

            // At the end of the iteration, we transfer the next_bitfield to the current bitfield
            // to use it to prune the next table to scan.

            ulong max_table_size = table_sizes.Max();

            var next_bitfield = new Bitfield(max_table_size);
            var current_bitfield = new Bitfield(max_table_size);

            // table 1 and 7 are special. They are passed on as plain files on disk.
            // Only table 2-6 are passed on as SortManagers, to phase3
            List<SortManager> output_files = Enumerable.Repeat(default(SortManager), 7 - 2).ToList();

            int table_index;
            ulong table_size;
            ulong entry_size;
            BufferedDisk disk;
            
            // note that we don't iterate over table_index=1. That table is special
            // since it contains different data. We'll do an extra scan of table 1 at
            // the end, just to compact it.
            for (table_index = 7; table_index > 1; --table_index)
            {
                Console.WriteLine($"Backpropagating on table {table_index}");

                //Timer scan_timer;

                next_bitfield.Clear();

                table_size = table_sizes[table_index];
                entry_size = Util.Cdiv(k + Constants.kOffsetSize + (table_index == 7 ? k : 0U), 8);

                disk = new BufferedDisk(tmp_1_disks[table_index], table_size * entry_size);

                // read_index is the number of entries we've processed so far (in the
                // current table) i.e. the index to the current entry. This is not used
                // for table 7

                ulong read_cursor = 0;

                for (ulong read_index = 0; read_index < table_size; ++read_index, read_cursor += entry_size)
                {
                    var entry = disk.Read(read_cursor, entry_size);

                    ulong entry_pos_offset = 0;
                    if (table_index == 7)
                    {
                        // table 7 is special, we never drop anything, so just build
                        // next_bitfield
                        entry_pos_offset = Util.SliceInt64FromBytes(entry, k, pos_offset_size);
                    }
                    else
                    {
                        if (!current_bitfield.Get(read_index))
                        {
                            // This entry should be dropped.
                            continue;
                        }

                        entry_pos_offset = Util.SliceInt64FromBytes(entry, 0, pos_offset_size);
                    }

                    ulong entry_pos = entry_pos_offset >> (int) Constants.kOffsetSize;
                    ulong entry_offset = entry_pos_offset & ((1U << (int) Constants.kOffsetSize) - 1);
                    // mark the two matching entries as used (pos and pos+offset)
                    next_bitfield.Set(entry_pos);
                    next_bitfield.Set(entry_pos + entry_offset);
                }

                Console.WriteLine($"scanned table {table_index}");
                //scan_timer.PrintElapsed("scanned time = ");

                Console.WriteLine($"sorting table {table_index}");
                //Timer sort_timer;

                // read the same table again. This time we'll output it to new files:
                // * add sort_key (just the index of the current entry)
                // * update (pos, offset) to remain valid after table_index-1 has been
                //   compacted.
                // * sort by pos
                //
                // As we have to sort two adjacent tables at the same time in phase 3,
                // we can use only a half of memory_size for SortManager. However,
                // table 1 is already sorted, so we can use all memory for sorting
                // table 2.

                var sort_manager = new SortManager(
                    table_index == 2 ? memory_size : memory_size / 2,
                    num_buckets,
                    log_num_buckets,
                    new_entry_size,
                    tmp_dirname,
                    filename + $".p2.t{table_index}",
                    (uint) k,
                    0,
                    SortStrategy.QuicksortLast);

                // as we scan the table for the second time, we'll also need to remap
                // the positions and offsets based on the next_bitfield.
                var index = new BitfieldIndex(next_bitfield);

                read_cursor = 0;
                ulong write_counter = 0;

                for (ulong read_index = 0; read_index < table_size; ++read_index, read_cursor += entry_size)
                {
                    var entry = disk.Read(read_cursor, entry_size);

                    ulong entry_f7 = 0;
                    ulong entry_pos_offset;
                    if (table_index == 7)
                    {
                        // table 7 is special, we never drop anything, so just build
                        // next_bitfield
                        entry_f7 = Util.SliceInt64FromBytes(entry, 0, k);
                        entry_pos_offset = Util.SliceInt64FromBytes(entry, k, pos_offset_size);
                    }
                    else
                    {
                        // skipping
                        if (!current_bitfield.Get(read_index)) continue;

                        entry_pos_offset = Util.SliceInt64FromBytes(entry, 0, pos_offset_size);
                    }

                    ulong entry_pos = entry_pos_offset >> (int) Constants.kOffsetSize;
                    ulong entry_offset = entry_pos_offset & ((1U << (int) Constants.kOffsetSize) - 1);

                    // assemble the new entry and write it to the sort manager

                    // map the pos and offset to the new, compacted, positions and
                    // offsets
                    (entry_pos, entry_offset) = index.Lookup(entry_pos, entry_offset);
                    entry_pos_offset = (entry_pos << (int) Constants.kOffsetSize) | entry_offset;

                    var bytes =new byte[16];
                    if (table_index == 7)
                    {
                        // table 7 is already sorted by pos, so we just rewrite the
                        // pos and offset in-place
                        UInt128 new_entry = (UInt128) entry_f7<< f7_shift;
                        new_entry |= (UInt128) entry_pos_offset << t7_pos_offset_shift;
                        Util.IntTo16Bytes(bytes, new_entry);

                        disk.Write(read_index * entry_size, new ReadOnlySpan<byte>(bytes, 0,(int)entry_size));
                    }
                    else
                    {
                        // The new entry is slightly different. Metadata is dropped, to
                        // save space, and the counter of the entry is written (sort_key). We
                        // use this instead of (y + pos + offset) since its smaller.
                        UInt128 new_entry = (UInt128) write_counter << write_counter_shift;
                        new_entry |= (UInt128) entry_pos_offset << pos_offset_shift;
                        Util.IntTo16Bytes(bytes, new_entry);

                        sort_manager.AddToCache(bytes);
                    }

                    ++write_counter;
                }

                if (table_index != 7)
                {
                    sort_manager.FlushCache();
                    //sort_timer.PrintElapsed("sort time = ");

                    // clear disk caches
                    disk.FreeMemory();
                    sort_manager.FreeMemory();

                    output_files[table_index - 2] = sort_manager;
                    new_table_sizes[table_index] = write_counter;
                }

                current_bitfield.Swap(ref next_bitfield);
                next_bitfield.Clear();

                // The files for Table 1 and 7 are re-used, overwritten and passed on to
                // the next phase. However, table 2 through 6 are all written to sort
                // managers that are passed on to the next phase. At this point, we have
                // to delete the input files for table 2-6 to save disk space.
                // This loop doesn't cover table 1, it's handled below with the
                // FilteredDisk wrapper.
                if (table_index != 7)
                {
                    tmp_1_disks[table_index].Truncate(0);
                }

                if (flags.HasFlag(PhaseFlags.ShowProgress))
                {
                    ProgressNotificator.ShowProgress(2, 8 - table_index, 6);
                }
            }

            // lazy-compact table 1 based on current_bitfield

            table_index = 1;
            table_size = table_sizes[table_index];
            entry_size = (ulong)EntrySizes.GetMaxEntrySize(k, (byte)table_index, false);

            // at this point, table 1 still needs to be compacted, based on
            // current_bitfield. Instead of compacting it right now, defer it and read
            // from it as-if it was compacted. This saves one read and one write pass
            new_table_sizes[table_index] = current_bitfield.Count(0, table_size);
            disk = new BufferedDisk(tmp_1_disks[table_index], table_size * entry_size);

            Console.WriteLine($"table {table_index} new size: {new_table_sizes[table_index]}");

            return new Phase2Results()
            {
                table1 = new FilteredDisk(disk, current_bitfield, entry_size),
                table7 = new BufferedDisk(tmp_1_disks[7], new_table_sizes[7] * new_entry_size),
                output_files = output_files,
                table_sizes = new_table_sizes
            };
        }
    }
}

    