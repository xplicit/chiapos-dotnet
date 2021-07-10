using System;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Util
    {
        public static int Cdiv(int a, int b) { return (a + b - 1) / b; }
        public static ulong Cdiv(ulong a, int b) { return (a + (ulong)b - 1) / (ulong)b; }

        public static int ByteAlign(int num_bits) { return (num_bits + (8 - ((num_bits) % 8)) % 8); }

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
        public static ulong SliceInt64FromBytes(ReadOnlySpan<byte> bytes, uint start_bit, uint num_bits)
        {
            ulong tmp;
            uint index = 0;

            if (start_bit + num_bits > 64) {
                index = start_bit / 8;
                start_bit %= 8;
            }

            tmp = EightBytesToInt(bytes.Slice((int)index));
            tmp <<= (int)start_bit;
            tmp >>= 64 - (int)num_bits;
            return tmp;
        }
        
        public static ulong SliceInt64FromBytesFull(ReadOnlySpan<byte> bytes, uint start_bit, uint num_bits)
        {
            uint last_bit = start_bit + num_bits;
            ulong r = SliceInt64FromBytes(bytes, start_bit, num_bits);
            if (start_bit % 8 + num_bits > 64)
                r |= (uint) (bytes[(int)(last_bit / 8)] >> (int)(8 - last_bit % 8));
            return r;
        }

        public static UInt128 SliceInt128FromBytes(ReadOnlySpan<byte> bytes, uint start_bit, uint num_bits)
        {
            if (num_bits <= 64)
                return SliceInt64FromBytesFull(bytes, start_bit, num_bits);

            uint num_bits_high = num_bits - 64;
            ulong high = SliceInt64FromBytesFull(bytes, start_bit, num_bits_high);
            ulong low = SliceInt64FromBytesFull(bytes, start_bit + num_bits_high, 64);
            return ((UInt128)high << 64) | low;
        }

        public static bool IntToEightBytes(Span<byte> bytes, ulong value)
        {
            var x = SwapBytes(value);
            return BitConverter.TryWriteBytes(bytes, x);
        }
        
        public static ulong EightBytesToInt(ReadOnlySpan<byte> bytes)
        {
            var result = BitConverter.ToUInt64(bytes);
            return SwapBytes(result);
        }
        
        public static void IntTo16Bytes(Span<byte> result, UInt128 input)
        {
            ulong r = SwapBytes(input.S1);
            BitConverter.TryWriteBytes(result, r);

            r = SwapBytes(input.S0);
            BitConverter.TryWriteBytes(result[8..], r);
        }
        
        // Used to encode deltas object size
        public static void IntToTwoBytesLE(Span<byte> result, ushort input)
        {
            //Check for endiannes
            //BitConverter.TryWriteBytes(result, input);
            result[0] = (byte)(input & 0xff);
            result[1] = (byte)(input >> 8);
        }


        public static ulong SwapBytes(ulong x)
        {
            // swap adjacent 32-bit blocks
            x = (x >> 32) | (x << 32);
            // swap adjacent 16-bit blocks
            x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
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
        
        /*
         * Like memcmp, but only compares starting at a certain bit.
         */
        public static int MemCmpBits(ReadOnlySpan<byte> left_arr, ReadOnlySpan<byte> right_arr, int len, int bits_begin)
        {
            int start_byte = bits_begin / 8;
            byte mask = (byte) ((1 << (8 - (bits_begin % 8))) - 1);
            if ((left_arr[start_byte] & mask) != (right_arr[start_byte] & mask))
            {
                return (left_arr[start_byte] & mask) - (right_arr[start_byte] & mask);
            }

            for (int i = start_byte + 1; i < len; i++)
            {
                if (left_arr[i] != right_arr[i])
                    return left_arr[i] - right_arr[i];
            }

            return 0;
        }


    }
}