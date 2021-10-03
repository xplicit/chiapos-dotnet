using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Util
    {
        public static int Cdiv(int a, int b) { return (a + b - 1) / b; }
        public static ulong Cdiv(ulong a, int b) { return (a + (ulong)b - 1) / (ulong)b; }

        public static int ByteAlign(int num_bits) { return (num_bits + (8 - ((num_bits) % 8)) % 8); }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetBucketNumber(ulong value, int valueBitLength, int numberOfBucketBits)
        {
            return (byte)(value >> (valueBitLength - numberOfBucketBits));
        }
        public static ulong ExtractNum(ReadOnlySpan<byte> buffer, uint len, uint begin_bits, uint take_bits)
        {
            if ((begin_bits + take_bits) / 8 > len - 1) {
                take_bits = len * 8 - begin_bits;
            }
            return SliceInt64FromBytes(buffer, begin_bits, take_bits);
        }

        // 'bytes' points to a big-endian 64 bit value (possibly truncated, if
        // (start_bit % 8 + num_bits > 64)). Returns the integer that starts at
        // 'start_bit' that is 'num_bits' long (as a native-endian integer).
        //
        // Note: requires that 8 bytes after the first sliced byte are addressable
        // (regardless of 'num_bits'). In practice it can be ensured by allocating
        // extra 7 bytes to all memory buffers passed to this function.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SliceInt64FromBytes(ReadOnlySpan<byte> bytes, uint start_bit, uint num_bits)
        {
            ulong tmp = BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice((int)start_bit >> 3, 8));
            tmp <<= (int)start_bit & ((1 << 3) - 1);
            tmp >>= 64 - (int)num_bits;
            return tmp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong SliceInt64FromBytesFull(ReadOnlySpan<byte> bytes, uint start_bit, uint num_bits)
        {
            uint last_bit = start_bit + num_bits;
            ulong r = SliceInt64FromBytes(bytes, start_bit, num_bits);
            if (start_bit % 8 + num_bits > 64)
                r |= (uint) (bytes[(int)(last_bit / 8)] >> (int)(8 - last_bit % 8));
            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 SliceInt128FromBytes(ReadOnlySpan<byte> bytes, uint start_bit, uint num_bits)
        {
            if (num_bits <= 64)
                return SliceInt64FromBytesFull(bytes, start_bit, num_bits);

            uint num_bits_high = num_bits - 64;
            ulong high = SliceInt64FromBytesFull(bytes, start_bit, num_bits_high);
            ulong low = SliceInt64FromBytesFull(bytes, start_bit + num_bits_high, 64);
            return ((UInt128)high << 64) | low;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntToEightBytes(Span<byte> bytes, ulong value) =>
            BinaryPrimitives.WriteUInt64BigEndian(bytes.Slice(0, 8), value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong EightBytesToInt(ReadOnlySpan<byte> bytes) =>
            BinaryPrimitives.ReadUInt64BigEndian(bytes.Slice(0, 8));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntToTwoBytes(Span<byte> bytes, ushort value) =>
            BinaryPrimitives.WriteUInt16BigEndian(bytes, value);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntTo16Bytes(Span<byte> result, UInt128 input)
        {
            BinaryPrimitives.TryWriteUInt64BigEndian(result.Slice(0, 8), input.S1);
            BinaryPrimitives.TryWriteUInt64BigEndian(result.Slice(8, 8), input.S0);
        }

       
        // Used to encode deltas object size
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntToTwoBytesLE(Span<byte> result, ushort input)
        {
            //Check for endiannes
            //BitConverter.TryWriteBytes(result, input);
            result[0] = (byte)(input & 0xff);
            result[1] = (byte)(input >> 8);
        }
        
        // The number of memory entries required to do the custom SortInMemory algorithm, given the
        // total number of entries to be sorted.
        public static ulong RoundSize(ulong size)
        {
            size *= 2;
            ulong result = 1;
            while (result < size) result *= 2;
            return result + 50;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsPositionEmpty(ReadOnlySpan<byte> memory)
        {
            int len = memory.Length;

            fixed (byte* memoryPtr = memory)
            {
                byte* ptr = memoryPtr;
                int align = (8 - (int)((ulong)ptr & 7)) & 7;
                for (int i = 0; i < Math.Min(align, len); i++)
                {
                    if (*ptr != 0)
                        return false;
                    ptr++;
                    len--;
                }
                
                while (len >= 8)
                {
                    if (*(long*)ptr != 0)
                        return false;
                    ptr += 8;
                    len -= 8;
                }

                while (len >= 4)
                {
                    if (*(int*)ptr != 0)
                        return false;
                    ptr += 4;
                    len -= 4;
                }

                while (len > 0)
                {
                    if (*ptr != 0)
                        return false;
                    ptr++;
                    len--;
                }
            }
            
            return true;
        }
        
        /*
         * Like memcmp, but only compares starting at a certain bit.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MemCmpBits(ReadOnlySpan<byte> left_arr, ReadOnlySpan<byte> right_arr, int len, int bits_begin)
        {
            int start_byte = bits_begin >> 3;
            byte mask = (byte) ((1 << (8 - (bits_begin & 7))) - 1);
            left_arr = left_arr.Slice(start_byte, len - start_byte);
            right_arr = right_arr.Slice(start_byte, len - start_byte);
            
            if (left_arr[0] != right_arr[0] &&
                (left_arr[0] & mask) == (right_arr[0] & mask))
            {
                Console.WriteLine("1");
            }


            if ((left_arr[0] & mask) != (right_arr[0] & mask))
            {
                return (left_arr[0] & mask) - (right_arr[0] & mask);
            }
            
            //According to benchmarking there is a time win when array length is 12 or more
            if (left_arr.Length > 11)
                return left_arr.Slice(1).SequenceCompareTo(right_arr.Slice(1));
            
            for (int i = 1; i < left_arr.Length; i++)
            {
                if (left_arr[i] != right_arr[i])
                    return left_arr[i] - right_arr[i];
            }

            return 0;
        }
        
        

        public static Dictionary<int,Dictionary<int, int>> swapStats = new(); 

        public static int MemCmpBitsAndSwap(Span<byte> left_arr, Span<byte> right_arr, int len, int bits_begin)
        {
            int start_byte = bits_begin >> 3;
            byte mask = (byte) ((1 << (8 - (bits_begin & 7))) - 1);
            var leftSpan = left_arr.Slice(start_byte, len - start_byte);
            var rightSpan = right_arr.Slice(start_byte, len - start_byte);
#if SWAP_STATS
            Dictionary<int, int> stats;

            if (swapStats.ContainsKey(len))
                stats = swapStats[len];
            else
            {
                stats = new Dictionary<int, int>();
                swapStats.Add(len, stats);
            }
#endif
            
            if ((leftSpan[0] & mask) != (rightSpan[0] & mask))
            {
                if ((leftSpan[0] & mask) > (rightSpan[0] & mask))
                {
                    Span<byte> swapSpace = stackalloc byte[len];
                    left_arr.Slice(0, len).CopyTo(swapSpace);
                    right_arr.Slice(0, len).CopyTo(left_arr);
                    swapSpace.CopyTo(right_arr);

#if SWAP_STATS
                    if (stats.ContainsKey(len))
                        stats[len]++;
                    else
                    {
                        stats.Add(len, 1);
                    }
#endif                    
                    return len;
                }

                return 0;
            }
            
            for (int i = 1; i < leftSpan.Length; i++)
            {
                if (leftSpan[i] != rightSpan[i])
                {
                    if (leftSpan[i] > rightSpan[i])
                    {
                        int swapBytes = leftSpan.Length - i + 1;
                        Span<byte> swapSpace = stackalloc byte[Math.Max(start_byte, swapBytes)];
                        
                        if (start_byte > 0)
                        {
                            left_arr.Slice(0, start_byte).CopyTo(swapSpace);
                            right_arr.Slice(0, start_byte).CopyTo(left_arr);
                            swapSpace.Slice(0, start_byte).CopyTo(right_arr);
                        }

                        leftSpan.Slice(i - 1).CopyTo(swapSpace);
                        rightSpan.Slice(i - 1).CopyTo(leftSpan.Slice(i - 1));
                        swapSpace.Slice(0, swapBytes).CopyTo(rightSpan.Slice(i - 1));
#if SWAP_STATS
                        if (stats.ContainsKey(start_byte))
                            stats[start_byte]++;
                        else
                        {
                            stats.Add(start_byte, 1);
                        }
                        
                        if (stats.ContainsKey(swapBytes))
                            stats[swapBytes]++;
                        else
                        {
                            stats.Add(swapBytes, 1);
                        }
#endif
                        return swapBytes + start_byte;
                    }

                    return 0;
                }
            }

            return 0;
        }

        
        public static uint RoundPow2(uint a)
        {
            uint result = 1;
            while ((a = (a >> 1)) > 0) result = result << 1;
            return result;

            /*
            // https://stackoverflow.com/questions/54611562/truncate-float-to-nearest-power-of-2-in-c-performance
            int exp;
            double frac = frexp(a, &exp);
            if (frac > 0.0)
                frac = 0.5;
            else if (frac < 0.0)
                frac = -0.5;
            double b = ldexp(frac, exp);
            return b;
            */
        }
    }
}