using System;
using System.Buffers;

namespace Chiapos.Dotnet
{
    public class QuickSort
    {
        private static void SortInner(
            byte[] memory,
            ulong memory_len,
            uint L,
            uint bits_begin,
            ulong begin,
            ulong end,
            byte[] pivot_space)
        {
            if (end - begin <= 5)
            {
                for (ulong i = begin + 1; i < end; i++)
                {
                    ulong j = i;
                    Buffer.BlockCopy(memory, (int) (i * L), pivot_space, 0, (int)L);
                    while (j > begin &&
                           Util.MemCmpBits(
                               new ReadOnlySpan<byte>(memory, (int) ((j - 1) * L), (int) (memory_len - (j - 1) * L)),
                               pivot_space, (int) L, (int) bits_begin) > 0)
                    {
                        Buffer.BlockCopy(memory, (int) ((j - 1) * L), memory, (int) (j * L), (int) L);
                        j--;
                    }

                    Buffer.BlockCopy(pivot_space, 0, memory, (int) (j * L), (int) L);
                }

                return;
            }

            ulong lo = begin;
            ulong hi = end - 1;

            Buffer.BlockCopy(memory, (int) (hi * L), pivot_space, 0, (int) L);
            bool left_side = true;

            while (lo < hi)
            {
                if (left_side)
                {
                    if (Util.MemCmpBits(new ReadOnlySpan<byte>(memory, (int) (lo * L), (int) (memory_len - lo * L))
                        , pivot_space, (int) L, (int) bits_begin) < 0)
                    {
                        ++lo;
                    }
                    else
                    {
                        Buffer.BlockCopy(memory, (int) (lo * L), memory, (int) (hi * L), (int) L);
                        --hi;
                        left_side = false;
                    }
                }
                else
                {
                    if (Util.MemCmpBits(new ReadOnlySpan<byte>(memory, (int) (hi * L), (int) (memory_len - hi * L)),
                        pivot_space, (int) L, (int) bits_begin) > 0)
                    {
                        --hi;
                    }
                    else
                    {
                        Buffer.BlockCopy(memory, (int) (hi * L), memory, (int) (lo * L), (int) L);
                        ++lo;
                        left_side = true;
                    }
                }
            }

            Buffer.BlockCopy(pivot_space, 0, memory, (int) (lo * L), (int) L);
            if (lo - begin <= end - lo)
            {
                SortInner(memory, memory_len, L, bits_begin, begin, lo, pivot_space);
                SortInner(memory, memory_len, L, bits_begin, lo + 1, end, pivot_space);
            }
            else
            {
                SortInner(memory, memory_len, L, bits_begin, lo + 1, end, pivot_space);
                SortInner(memory, memory_len, L, bits_begin, begin, lo, pivot_space);
            }
        }

        public static void Sort(
            byte[] memory,
            uint entry_len,
            ulong num_entries,
            uint bits_begin)
        {
            ulong memory_len = entry_len * num_entries;
            var pivot_space = new byte[entry_len];
            SortInner(memory, memory_len, entry_len, bits_begin, 0, num_entries, pivot_space);
        }

    }
}