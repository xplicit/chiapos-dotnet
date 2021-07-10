using System;
using System.Buffers;
using Chiapos.Dotnet.Disks;

namespace Chiapos.Dotnet
{
    public class UniformSort
    {
        const ulong BUF_SIZE = 262144;

        static bool IsPositionEmpty(ReadOnlySpan<byte> memory, int entry_len)
        {
            for (int i = 0; i < entry_len; i++)
                if (memory[i] != 0)
                    return false;
            return true;
        }

        public static void SortToMemory(
            FileDisk input_disk,
            ulong input_disk_begin,
            byte[] memory,
            int entry_len,
            ulong num_entries,
            int bits_begin)
        {
            ulong memory_len = Util.RoundSize(num_entries) * (ulong) entry_len;
            var swap_space = new byte[entry_len];
            var buffer = new byte[BUF_SIZE];
            ulong bucket_length = 0;
            // The number of buckets needed (the smallest power of 2 greater than 2 * num_entries).
            while ((1UL << (int) bucket_length) < 2 * num_entries) bucket_length++;
            Array.Clear(memory, 0, (int) memory_len);

            ulong read_pos = input_disk_begin;
            ulong buf_size = 0;
            int buf_ptr = 0;
            ulong swaps = 0;
            for (ulong i = 0; i < num_entries; i++)
            {
                if (buf_size == 0)
                {
                    // If read buffer is empty, read from disk and refill it.
                    buf_size = Math.Min(BUF_SIZE / (ulong) entry_len, num_entries - i);
                    buf_ptr = 0;
                    input_disk.Read(read_pos, buffer, 0, buf_size * (ulong) entry_len);
                    read_pos += buf_size * (ulong) entry_len;
                }

                buf_size--;
                // First unique bits in the entry give the expected position of it in the sorted array.
                // We take 'bucket_length' bits starting with the first unique one.
                int pos =
                    (int) Util.ExtractNum(new ReadOnlySpan<byte>(buffer, buf_ptr, buffer.Length - buf_ptr),
                        (uint) entry_len, (uint) bits_begin, (uint) bucket_length)
                    * entry_len;
                // As long as position is occupied by a previous entry...
                while (!IsPositionEmpty(new ReadOnlySpan<byte>(memory, pos, entry_len), entry_len) &&
                       (ulong) pos < memory_len)
                {
                    // ...store there the minimum between the two and continue to push the higher one.
                    if (Util.MemCmpBits(
                        new ReadOnlySpan<byte>(memory, pos, entry_len),
                        new ReadOnlySpan<byte>(buffer, buf_ptr, entry_len),
                        entry_len, bits_begin) > 0)
                    {
                        Buffer.BlockCopy(memory, pos, swap_space, 0, entry_len);
                        Buffer.BlockCopy(buffer, buf_ptr, memory, pos, entry_len);
                        Buffer.BlockCopy(swap_space, 0, buffer, buf_ptr, entry_len);
                        swaps++;
                    }

                    pos += entry_len;
                }

                // Push the entry in the first free spot.
                Buffer.BlockCopy(buffer, buf_ptr, memory, pos, entry_len);
                buf_ptr += entry_len;
            }

            int entries_written = 0;
            // Search the memory buffer for occupied entries.
            for (int pos = 0;
                (ulong) entries_written < num_entries && (ulong) pos < memory_len;
                pos += entry_len)
            {
                if (!IsPositionEmpty(new ReadOnlySpan<byte>(memory, (int) pos, entry_len), entry_len))
                {
                    // We've found an entry.
                    // write the stored entry itself.
                    Buffer.BlockCopy(memory, pos, memory, entries_written * entry_len, entry_len);
                    entries_written++;
                }
            }

            //assert(entries_written == num_entries);
        }
    }
}