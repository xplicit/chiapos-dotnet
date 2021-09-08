using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using chiapos_dotnet;
using Chiapos.Dotnet.Disks;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Phase3
    {
        // This writes a number of entries into a file, in the final, optimized format. The park
// contains a checkpoint value (which is a 2k bits line point), as well as EPP (entries per
// park) entries. These entries are each divided into stub and delta section. The stub bits are
// encoded as is, but the delta bits are optimized into a variable encoding scheme. Since we
// have many entries in each park, we can approximate how much space each park with take. Format
// is: [2k bits of first_line_point]  [EPP-1 stubs] [Deltas size] [EPP-1 deltas]....
// [first_line_point] ...
void WriteParkToFile(
    FileDisk final_disk,
    ulong table_start,
    ulong park_index,
    uint park_size_bytes,
    UInt128 first_line_point,
    List<byte> park_deltas,
    ReadOnlySpan<ulong> park_stubs,
    byte k,
    byte table_index,
    byte[] park_buffer,
    ulong park_buffer_size)
{
    // Parks are fixed size, so we know where to start writing. The deltas will not go over
    // into the next park.
    ulong writer = table_start + park_index * park_size_bytes;
    var index = new Span<byte>(park_buffer);

    first_line_point <<= 128 - 2 * k;
    Util.IntTo16Bytes(index, first_line_point);
    index = index.Slice(EntrySizes.CalculateLinePointSize(k));

    // We use ParkBits instead of Bits since it allows storing more data
    int park_stubs_bits_length = ParkBits.ToBytes(park_stubs, (k - Constants.kStubMinusBits), 
        park_buffer.AsSpan(park_buffer.Length - index.Length));
    int stubs_size = EntrySizes.CalculateStubsSize(k);
    int stubs_valid_size = Util.Cdiv(park_stubs_bits_length, 8);
    index.Slice((int)stubs_valid_size, stubs_size - stubs_valid_size).Fill(0);
    index = index.Slice(stubs_size);

    // The stubs are random so they don't need encoding. But deltas are more likely to
    // be small, so we can compress them
    double R = Constants.kRValues[table_index - 1];
    Span<byte> deltas_start = index.Slice(2);
    int deltas_size = ChiaEncoding.ANSEncodeDeltas(park_deltas, R, deltas_start);

    if (deltas_size == 0) {
        // Uncompressed
        deltas_size = park_deltas.Count;
        Util.IntToTwoBytesLE(index, (ushort)(deltas_size | 0x8000));
        CollectionsMarshal.AsSpan(park_deltas).CopyTo(deltas_start);
    } else {
        // Compressed
        Util.IntToTwoBytesLE(index, (ushort)deltas_size);
    }

    index = index.Slice(2 + deltas_size);
    index.Fill(0);

    final_disk.Write(writer, park_buffer);
}


// Compresses the plot file tables into the final file. In order to do this, entries must be
// reorganized from the (pos, offset) bucket sorting order, to a more free line_point sorting
// order. In (pos, offset ordering), we store two pointers two the previous table, (x, y) which
// are very close together, by storing  (x, y-x), or (pos, offset), which can be done in about k
// + 8 bits, since y is in the next bucket as x. In order to decrease this, We store the actual
// entries from the previous table (e1, e2), instead of pos, offset pointers, and sort the
// entire table by (e1,e2). Then, the deltas between each (e1, e2) can be stored, which require
// around k bits.

// Converting into this format requires a few passes and sorts on disk. It also assumes that the
// backpropagation step happened, so there will be no more dropped entries. See the design
// document for more details on the algorithm.
public Phase3Results RunPhase3(
    byte k,
    FileDisk tmp2_disk /*filename*/,
    Phase2Results res2,
    byte[] id,
    string tmp_dirname,
    string filename,
    uint header_size,
    ulong memory_size,
    uint num_buckets,
    uint log_num_buckets,
    PhaseFlags flags)
{
    byte pos_size = k;
    byte line_point_size = (byte)(2 * k - 1);

    var final_table_begin_pointers = new List<ulong>(Enumerable.Repeat<ulong>(0, 12));
    final_table_begin_pointers[1] = header_size;

    var table_pointer_bytes = new byte[8];
    Util.IntToEightBytes(table_pointer_bytes, final_table_begin_pointers[1]);
    tmp2_disk.Write(header_size - 10 * 8, table_pointer_bytes);

    ulong final_entries_written = 0;
    int right_entry_size_bytes = 0;
    ulong new_pos_entry_size_bytes = 0;

    SortManager L_sort_manager = null;
    SortManager R_sort_manager;

    // These variables are used in the WriteParkToFile method. They are preallocatted here
    // to save time.
    int park_buffer_size = EntrySizes.CalculateLinePointSize(k)
        + EntrySizes.CalculateStubsSize(k) + 2
        + EntrySizes.CalculateMaxDeltasSize(k, 1);
    var park_buffer = new byte[park_buffer_size];

    // Iterates through all tables, starting at 1, with L and R pointers.
    // For each table, R entries are rewritten with line points. Then, the right table is
    // sorted by line_point. After this, the right table entries are rewritten as (sort_key,
    // new_pos), where new_pos is the position in the table, where it's sorted by line_point,
    // and the line_points are written to disk to a final table. Finally, table_i is sorted by
    // sort_key. This allows us to compare to the next table.
    for (int table_index = 1; table_index < 7; table_index++) {
        PerformanceTimer table_timer = new();
        PerformanceTimer computation_pass_1_timer = new();
        Console.WriteLine($"Compressing tables {table_index} and {table_index + 1}");

        // The park size must be constant, for simplicity, but must be big enough to store EPP
        // entries. entry deltas are encoded with variable length, and thus there is no
        // guarantee that they won't override into the next park. It is only different (larger)
        // for table 1
        int park_size_bytes = EntrySizes.CalculateParkSize(k, (byte)table_index);

        IDisk right_disk = res2.disk_for_table(table_index + 1);
        IDisk left_disk = res2.disk_for_table(table_index);

        // Sort key is k bits for all tables. For table 7 it is just y, which
        // is k bits, and for all other tables the number of entries does not
        // exceed 0.865 * 2^k on average.
        uint right_sort_key_size = k;

        int left_entry_size_bytes = EntrySizes.GetMaxEntrySize(k, (byte)table_index, false);
        int p2_entry_size_bytes = EntrySizes.GetKeyPosOffsetSize(k);
        right_entry_size_bytes = EntrySizes.GetMaxEntrySize(k, (byte)(table_index + 1), false);

        ulong left_reader = 0;
        ulong right_reader = 0;
        ulong left_reader_count = 0;
        ulong right_reader_count = 0;
        ulong total_r_entries = 0;

        if (table_index > 1) {
            L_sort_manager?.FreeMemory();
        }

        // We read only from this SortManager during the second pass, so all
        // memory is available
        R_sort_manager = new SortManager(
            memory_size,
            num_buckets,
            log_num_buckets,
            (ushort)right_entry_size_bytes,
            tmp_dirname,
            filename + $".p3.t{table_index + 1}",
            0,
            0,
            SortStrategy.QuicksortLast);

        bool should_read_entry = true;
        List<ulong> left_new_pos = new List<ulong>(Enumerable.Repeat<ulong>(0,(int)Constants.kCachedPositionsSize));

        ulong[,] old_sort_keys = new ulong[Constants.kReadMinusWrite, Constants.kMaxMatchesSingleEntry];
        ulong[,] old_offsets = new ulong[Constants.kReadMinusWrite, Constants.kMaxMatchesSingleEntry];
        ushort[] old_counters = Enumerable.Repeat<ushort>(0, (int) Constants.kReadMinusWrite).ToArray();
        
        bool end_of_right_table = false;
        ulong current_pos = 0;
        ulong end_of_table_pos = 0;
        ulong greatest_pos = 0;

        ReadOnlySpan<byte> left_entry_disk_buf = null;

        ulong entry_sort_key, entry_pos, entry_offset;
        ulong cached_entry_sort_key = 0;
        ulong cached_entry_pos = 0;
        ulong cached_entry_offset = 0;

        // Similar algorithm as Backprop, to read both L and R tables simultaneously
        while (!end_of_right_table || (current_pos - end_of_table_pos <= Constants.kReadMinusWrite)) {
            old_counters[current_pos % Constants.kReadMinusWrite] = 0;

            if (end_of_right_table || current_pos <= greatest_pos) {
                while (!end_of_right_table) {
                    if (should_read_entry) {
                        if (right_reader_count == res2.table_sizes[table_index + 1]) {
                            end_of_right_table = true;
                            end_of_table_pos = current_pos;
                            right_disk.FreeMemory();
                            break;
                        }
                        // The right entries are in the format from backprop, (sort_key, pos,
                        // offset)
                        var right_entry_buf = right_disk.Read(right_reader, (ulong)p2_entry_size_bytes);
                        right_reader += (ulong)p2_entry_size_bytes;
                        right_reader_count++;

                        entry_sort_key =
                            Util.SliceInt64FromBytes(right_entry_buf, 0, right_sort_key_size);
                        entry_pos = Util.SliceInt64FromBytes(
                            right_entry_buf, right_sort_key_size, pos_size);
                        entry_offset = Util.SliceInt64FromBytes(
                            right_entry_buf, right_sort_key_size + pos_size, Constants.kOffsetSize);
                    } else if (cached_entry_pos == current_pos) {
                        entry_sort_key = cached_entry_sort_key;
                        entry_pos = cached_entry_pos;
                        entry_offset = cached_entry_offset;
                    } else {
                        break;
                    }

                    should_read_entry = true;

                    if (entry_pos + entry_offset > greatest_pos) {
                        greatest_pos = entry_pos + entry_offset;
                    }
                    if (entry_pos == current_pos) {
                        ulong old_write_pos = entry_pos % Constants.kReadMinusWrite;
                        old_sort_keys[old_write_pos, old_counters[old_write_pos]] = entry_sort_key;
                        old_offsets[old_write_pos, old_counters[old_write_pos]] =
                            (entry_pos + entry_offset);
                        ++old_counters[old_write_pos];
                    } else {
                        should_read_entry = false;
                        cached_entry_sort_key = entry_sort_key;
                        cached_entry_pos = entry_pos;
                        cached_entry_offset = entry_offset;
                        break;
                    }
                }

                if (left_reader_count < res2.table_sizes[table_index]) {
                    // The left entries are in the new format: (sort_key, new_pos), except for table
                    // 1: (y, x).

                    // TODO: unify these cases once SortManager implements
                    // the ReadDisk interface
                    if (table_index == 1) {
                        left_entry_disk_buf = left_disk.Read(left_reader, (ulong)left_entry_size_bytes);
                        left_reader += (ulong)left_entry_size_bytes;
                    } else {
                        left_entry_disk_buf = L_sort_manager.ReadEntry(left_reader);
                        left_reader += new_pos_entry_size_bytes;
                    }
                    left_reader_count++;
                }

                // We read the "new_pos" from the L table, which for table 1 is just x. For
                // other tables, the new_pos
                if (table_index == 1) {
                    // Only k bits, since this is x
                    left_new_pos[(int)(current_pos % Constants.kCachedPositionsSize)] =
                        Util.SliceInt64FromBytes(left_entry_disk_buf, 0, k);
                } else {
                    // k+1 bits in case it overflows
                    left_new_pos[(int)(current_pos % Constants.kCachedPositionsSize)] =
                        Util.SliceInt64FromBytes(left_entry_disk_buf, right_sort_key_size, k);
                }
            }

            ulong write_pointer_pos = current_pos - Constants.kReadMinusWrite + 1;

            // Rewrites each right entry as (line_point, sort_key)
            if (current_pos + 1 >= Constants.kReadMinusWrite) {
                ulong left_new_pos_1 = left_new_pos[(int)(write_pointer_pos % Constants.kCachedPositionsSize)];
                for (uint counter = 0;
                     counter < old_counters[write_pointer_pos % Constants.kReadMinusWrite];
                     counter++) {
                    ulong left_new_pos_2 = left_new_pos
                        [(int)(old_offsets[(int)(write_pointer_pos % Constants.kReadMinusWrite), (int)counter] %
                         Constants.kCachedPositionsSize)];

                    // A line point is an encoding of two k bit values into one 2k bit value.
                    UInt128 line_point =
                        ChiaEncoding.SquareToLinePoint(left_new_pos_1, left_new_pos_2);

                    if (left_new_pos_1 > ((ulong)1 << k) ||
                        left_new_pos_2 > ((ulong)1 << k)) {
                        Console.WriteLine("left or right positions too large");
                        Console.WriteLine($"{(line_point > ((UInt128)1 << (2 * k)))}");
                        if ((line_point > ((UInt128)1 << (2 * k)))) {
                            Console.WriteLine($"L, R: {left_new_pos_1} {left_new_pos_2}");
                            Console.WriteLine($"Line point: {line_point}");
                            Environment.Exit(1);
                        }
                    }
                    Bits to_write = new Bits(line_point, line_point_size);
                    to_write.AppendValue(old_sort_keys[write_pointer_pos % Constants.kReadMinusWrite, counter],
                        (int)right_sort_key_size);

                    R_sort_manager.AddToCache(to_write);
                    total_r_entries++;
                }
            }
            current_pos += 1;
        }
        computation_pass_1_timer.PrintElapsed("\tFirst computation pass time:");

        // Remove no longer needed file
        left_disk.Truncate(0);

        // Flush cache so all entries are written to buckets
        R_sort_manager.FlushCache();
        R_sort_manager.FreeMemory();

        PerformanceTimer computation_pass_2_timer = new();

        right_reader = 0;
        right_reader_count = 0;
        ulong final_table_writer = final_table_begin_pointers[table_index];

        final_entries_written = 0;

        if (table_index > 1) {
            // Make sure all files are removed
            L_sort_manager.Dispose();
        }

        // In the second pass we read from R sort manager and write to L sort
        // manager, and they both handle table (table_index + 1)'s data. The
        // newly written table consists of (sort_key, new_pos). Add one extra
        // bit for 'new_pos' to the 7-th table as it may have more than 2^k
        // entries.
        new_pos_entry_size_bytes = (ulong)Util.Cdiv(2 * k + (table_index == 6 ? 1 : 0), 8);

        // For tables below 6 we can only use a half of memory_size since it
        // will be sorted in the first pass of the next iteration together with
        // the next table, which will use the other half of memory_size.
        // Tables 6 and 7 will be sorted alone, so we use all memory for them.
        L_sort_manager = new SortManager(
            (table_index >= 5) ? memory_size : (memory_size / 2),
            num_buckets,
            log_num_buckets,
            (ushort)new_pos_entry_size_bytes,
            tmp_dirname,
            filename + $".p3s.t{table_index + 1}",
            0,
            0,
            SortStrategy.QuicksortLast);

        var park_deltas = new List<byte>();
        var park_stubs = new List<ulong>();
        UInt128 checkpoint_line_point = 0;
        UInt128 last_line_point = 0;
        ulong park_index = 0;

        ReadOnlySpan<byte> right_reader_entry_buf;

        // Now we will write on of the final tables, since we have a table sorted by line point.
        // The final table will simply store the deltas between each line_point, in fixed space
        // groups(parks), with a checkpoint in each group.
        int added_to_cache = 0;
        byte sort_key_shift = (byte)(128 - right_sort_key_size);
        byte index_shift = (byte)(sort_key_shift - (k + (table_index == 6 ? 1 : 0)));
        for (ulong index = 0; index < total_r_entries; index++) {
            right_reader_entry_buf = R_sort_manager.ReadEntry(right_reader);
            right_reader += (ulong)right_entry_size_bytes;
            right_reader_count++;

            // Right entry is read as (line_point, sort_key)
            UInt128 line_point = Util.SliceInt128FromBytes(right_reader_entry_buf, 0, line_point_size);
            ulong sort_key =
                Util.SliceInt64FromBytes(right_reader_entry_buf, line_point_size, right_sort_key_size);

            // Write the new position (index) and the sort key
            UInt128 to_write = (UInt128)sort_key << sort_key_shift;
            to_write |= (UInt128)index << index_shift;

            var bytes = new Span<byte>(new byte[16]);
            Util.IntTo16Bytes(bytes, to_write);
            L_sort_manager.AddToCache(bytes);
            added_to_cache++;

            // Every EPP entries, writes a park
            if (index % Constants.kEntriesPerPark == 0) {
                if (index != 0) {
                    WriteParkToFile(
                        tmp2_disk,
                        final_table_begin_pointers[table_index],
                        park_index,
                        (uint)park_size_bytes,
                        checkpoint_line_point,
                        park_deltas,
                        CollectionsMarshal.AsSpan(park_stubs),
                        k,
                        (byte)table_index,
                        park_buffer,
                        (ulong)park_buffer_size);
                    park_index += 1;
                    final_entries_written += (ulong)(park_stubs.Count + 1);
                }
                park_deltas.Clear();
                park_stubs.Clear();

                checkpoint_line_point = line_point;
            }
            UInt128 big_delta = line_point - last_line_point;

            // Since we have approx 2^k line_points between 0 and 2^2k, the average
            // space between them when sorted, is k bits. Much more efficient than storing each
            // line point. This is diveded into the stub and delta. The stub is the least
            // significant (k-kMinusStubs) bits, and largely random/incompressible. The small
            // delta is the rest, which can be efficiently encoded since it's usually very
            // small.

            ulong stub = big_delta & ((1UL << (k - Constants.kStubMinusBits)) - 1);
            byte small_delta = (byte)(big_delta >> (k - Constants.kStubMinusBits));

            //assert(small_delta < 256);

            if ((index % Constants.kEntriesPerPark != 0)) {
                park_deltas.Add(small_delta);
                park_stubs.Add(stub);
            }
            last_line_point = line_point;
        }
        R_sort_manager.Dispose();
        L_sort_manager.FlushCache();

        computation_pass_2_timer.PrintElapsed("\tSecond computation pass time:");

        if (park_deltas.Count > 0) {
            // Since we don't have a perfect multiple of EPP entries, this writes the last ones
            WriteParkToFile(
                tmp2_disk,
                final_table_begin_pointers[table_index],
                park_index,
                (uint)park_size_bytes,
                checkpoint_line_point,
                park_deltas,
                CollectionsMarshal.AsSpan(park_stubs),
                k,
                (byte)table_index,
                park_buffer,
                (ulong)park_buffer_size);
            final_entries_written += (ulong)(park_stubs.Count + 1);
        }

        ChiaEncoding.ANSFree(Constants.kRValues[table_index - 1]);
        Console.WriteLine($"\tWrote {final_entries_written} entries");

        final_table_begin_pointers[table_index + 1] =
            final_table_begin_pointers[table_index] + (park_index + 1) * (ulong)park_size_bytes;

        final_table_writer = header_size - 8 * (10 - (ulong)table_index);
        Util.IntToEightBytes(table_pointer_bytes, final_table_begin_pointers[table_index + 1]);
        tmp2_disk.Write(final_table_writer, table_pointer_bytes);
        final_table_writer += 8;

        table_timer.PrintElapsed("Total compress table time:");

        left_disk.FreeMemory();
        right_disk.FreeMemory();
        if (flags.HasFlag(PhaseFlags.ShowProgress))
        {
            ProgressNotificator.ShowProgress(3, table_index, 6);
        }
    }

    L_sort_manager.FreeMemory();
    //return park_buffer to ArrayPool
    //park_buffer.reset();

    // These results will be used to write table P7 and the checkpoint tables in phase 4.
    return new Phase3Results{
        final_table_begin_pointers = final_table_begin_pointers,
        final_entries_written = final_entries_written,
        right_entry_size_bits = new_pos_entry_size_bytes * 8,
        header_size = header_size,
        table7_sm = L_sort_manager
       
    };
}



    }
}

//#endif