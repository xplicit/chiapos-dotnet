using System;
using System.Collections.Generic;
using chiapos_dotnet;
using Chiapos.Dotnet.Disks;

namespace Chiapos.Dotnet
{
    public class Phase4
    {

        public void RunPhase4(byte k, byte pos_size, FileDisk tmp2_disk, Phase3Results res,
            PhaseFlags flags, int max_phase4_progress_updates)
        {
            uint P7_park_size = (uint) Util.ByteAlign((k + 1) * (int) Constants.kEntriesPerPark) / 8;
            ulong number_of_p7_parks =
                ((res.final_entries_written == 0 ? 0 : res.final_entries_written - 1) / Constants.kEntriesPerPark) +
                1;

            ulong begin_byte_C1 = res.final_table_begin_pointers[7] + number_of_p7_parks * P7_park_size;

            ulong total_C1_entries = Util.Cdiv(res.final_entries_written, (int) Constants.kCheckpoint1Interval);
            ulong begin_byte_C2 = begin_byte_C1 + (total_C1_entries + 1) * (ulong) (Util.ByteAlign(k) / 8);
            ulong total_C2_entries = Util.Cdiv(total_C1_entries, (int) Constants.kCheckpoint2Interval);
            ulong begin_byte_C3 = begin_byte_C2 + (total_C2_entries + 1) * (ulong) (Util.ByteAlign(k) / 8);

            uint size_C3 = (uint) EntrySizes.CalculateC3Size(k);
            ulong end_byte = begin_byte_C3 + (total_C1_entries) * size_C3;

            res.final_table_begin_pointers[8] = begin_byte_C1;
            res.final_table_begin_pointers[9] = begin_byte_C2;
            res.final_table_begin_pointers[10] = begin_byte_C3;
            res.final_table_begin_pointers[11] = end_byte;

            ulong plot_file_reader = 0;
            ulong final_file_writer_1 = begin_byte_C1;
            ulong final_file_writer_2 = begin_byte_C3;
            ulong final_file_writer_3 = res.final_table_begin_pointers[7];

            ulong prev_y = 0;
            List<Bits> C2 = new();
            ulong num_C1_entries = 0;
            List<byte> deltas_to_write = new();
            uint right_entry_size_bytes = (uint) (res.right_entry_size_bits / 8);

            ReadOnlySpan<byte> right_entry_buf;
            var C1_entry_buf = new byte[Util.ByteAlign(k) / 8];
            var C3_entry_buf = new byte[size_C3];
            var P7_entry_buf = new byte[P7_park_size];

            Console.WriteLine("\tStarting to write C1 and C3 tables");

            ParkBits to_write_p7 = null;
            int progress_update_increment = (int) (res.final_entries_written / (uint) max_phase4_progress_updates);

            // We read each table7 entry, which is sorted by f7, but we don't need f7 anymore. Instead,
            // we will just store pos6, and the deltas in table C3, and checkpoints in tables C1 and C2.
            for (ulong f7_position = 0; f7_position < res.final_entries_written; f7_position++)
            {
                right_entry_buf = res.table7_sm.ReadEntry(plot_file_reader);

                plot_file_reader += right_entry_size_bytes;
                ulong entry_y = Util.SliceInt64FromBytes(right_entry_buf, 0, k);
                ulong entry_new_pos = Util.SliceInt64FromBytes(right_entry_buf, k, pos_size);

                Bits entry_y_bits = new Bits(entry_y, k);

                if (f7_position == 0)
                {
                    to_write_p7 = new ParkBits(entry_new_pos, k + 1);
                }
                else
                {
                    if (f7_position % Constants.kEntriesPerPark == 0)
                    {
                        Array.Clear(P7_entry_buf, 0, (int) P7_park_size);
                        to_write_p7.ToBytes(P7_entry_buf);
                        tmp2_disk.Write(final_file_writer_3,
                            new ReadOnlySpan<byte>(P7_entry_buf, 0, (int) P7_park_size));
                        final_file_writer_3 += P7_park_size;
                        to_write_p7 = new ParkBits(entry_new_pos, k + 1);
                    }
                    else
                    {
                        to_write_p7.AppendValue(entry_new_pos, k + 1);
                    }
                }

                if (f7_position % Constants.kCheckpoint1Interval == 0)
                {
                    entry_y_bits.ToBytes(C1_entry_buf);
                    tmp2_disk.Write(final_file_writer_1,
                        new ReadOnlySpan<byte>(C1_entry_buf, 0, Util.ByteAlign(k) / 8));
                    final_file_writer_1 += (ulong) Util.ByteAlign(k) / 8;
                    if (num_C1_entries > 0)
                    {
                        final_file_writer_2 = begin_byte_C3 + (num_C1_entries - 1) * size_C3;
                        int num_bytes =
                            ChiaEncoding.ANSEncodeDeltas(deltas_to_write, Constants.kC3R,
                                new Span<byte>(C3_entry_buf, 2, C3_entry_buf.Length - 2)) + 2;

                        // We need to be careful because deltas are variable sized, and they need to fit
                        //assert(size_C3 * 8 > num_bytes);

                        // Write the size
                        Util.IntToTwoBytes(C3_entry_buf, (ushort) (num_bytes - 2));

                        tmp2_disk.Write(final_file_writer_2, new ReadOnlySpan<byte>(C3_entry_buf, 0, num_bytes));
                        final_file_writer_2 += (ulong) num_bytes;
                    }

                    prev_y = entry_y;
                    if (f7_position % (Constants.kCheckpoint1Interval * Constants.kCheckpoint2Interval) == 0)
                    {
                        C2.Add(entry_y_bits);
                    }

                    deltas_to_write.Clear();
                    ++num_C1_entries;
                }
                else
                {
                    deltas_to_write.Add((byte) (entry_y - prev_y));
                    prev_y = entry_y;
                }

                if (flags.HasFlag(PhaseFlags.ShowProgress) && f7_position % (uint) progress_update_increment == 0)
                {
                    ProgressNotificator.ShowProgress(4, (long) f7_position, (long) res.final_entries_written);
                }
            }

            ChiaEncoding.ANSFree(Constants.kC3R);
            res.table7_sm.Reset();

            // Writes the final park to disk
            Array.Clear(P7_entry_buf, 0, (int) P7_park_size);
            to_write_p7.ToBytes(P7_entry_buf);

            tmp2_disk.Write(final_file_writer_3, new ReadOnlySpan<byte>(P7_entry_buf, 0, (int) P7_park_size));
            final_file_writer_3 += P7_park_size;

            if (deltas_to_write.Count > 0)
            {
                int num_bytes = ChiaEncoding.ANSEncodeDeltas(deltas_to_write, Constants.kC3R,
                    new Span<byte>(C3_entry_buf, +2, C3_entry_buf.Length - 2));
                Array.Clear(C3_entry_buf, num_bytes + 2, (int) size_C3 - (num_bytes + 2));
                final_file_writer_2 = begin_byte_C3 + (num_C1_entries - 1) * size_C3;

                // Write the size
                Util.IntToTwoBytes(C3_entry_buf, (ushort) num_bytes);

                tmp2_disk.Write(final_file_writer_2, new ReadOnlySpan<byte>(C3_entry_buf, 0, (int) size_C3));
                final_file_writer_2 += size_C3;
                ChiaEncoding.ANSFree(Constants.kC3R);
            }

            new Bits(0, Util.ByteAlign(k)).ToBytes(C1_entry_buf);
            tmp2_disk.Write(final_file_writer_1, new ReadOnlySpan<byte>(C1_entry_buf, 0, Util.ByteAlign(k) / 8));
            final_file_writer_1 += (ulong) Util.ByteAlign(k) / 8;
            Console.WriteLine("\tFinished writing C1 and C3 tables");
            Console.WriteLine("\tWriting C2 table");

            foreach (Bits C2_entry in C2)
            {
                C2_entry.ToBytes(C1_entry_buf);
                tmp2_disk.Write(final_file_writer_1, new ReadOnlySpan<byte>(C1_entry_buf, 0, Util.ByteAlign(k) / 8));
                final_file_writer_1 += (ulong) Util.ByteAlign(k) / 8;
            }

            new Bits(0, Util.ByteAlign(k)).ToBytes(C1_entry_buf);
            tmp2_disk.Write(final_file_writer_1, new ReadOnlySpan<byte>(C1_entry_buf, 0, Util.ByteAlign(k) / 8));
            final_file_writer_1 += (ulong) Util.ByteAlign(k) / 8;
            Console.WriteLine("\tFinished writing C2 table");

            //TODO: Release buffers to ArrayPool
            //delete[] C3_entry_buf;
            //delete[] C1_entry_buf;
            //delete[] P7_entry_buf;

            final_file_writer_1 = res.header_size - 8 * 3;
            byte[] table_pointer_bytes = new byte[8];

            // Writes the pointers to the start of the tables, for proving
            for (int i = 8; i <= 10; i++)
            {
                Util.IntToEightBytes(table_pointer_bytes, res.final_table_begin_pointers[i]);
                tmp2_disk.Write(final_file_writer_1, new ReadOnlySpan<byte>(table_pointer_bytes, 0, 8));
                final_file_writer_1 += 8;
            }
            tmp2_disk.Close();

            Console.WriteLine("\tFinal table pointers:");

            for (int i = 1; i <= 10; i++)
            {
                Console.Write($"\t{(i < 8 ? "P" : "C")}{(i < 8 ? i : i - 7)}");
                Console.WriteLine($": 0x{res.final_table_begin_pointers[i]:X}");
            }
        }
    }
}