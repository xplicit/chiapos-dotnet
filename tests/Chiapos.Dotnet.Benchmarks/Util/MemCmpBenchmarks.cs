using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Chiapos.Dotnet.Benchmarks
{
    public class MemCmpBenchmarks
    {
        byte[] a =
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
        };
        byte[] b =
        {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x12, 0x12, 0x13, 0x14, 0x15, 0x16,
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16,
        };
        
        [Benchmark(Baseline = true)]
        public void Original()
        {
            MemCmpBits_Original(a, b, 9, 7);
        }
        
        [Benchmark]
        public void Span()
        {
            MemCmpBits_Span(a, b, 9, 7);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MemCmpBits_Original(ReadOnlySpan<byte> left_arr, ReadOnlySpan<byte> right_arr, int len, int bits_begin)
        {
            int start_byte = bits_begin >> 3;
            byte mask = (byte) ((1 << (8 - (bits_begin & 7))) - 1);
            left_arr = left_arr.Slice(start_byte, len - start_byte);
            right_arr = right_arr.Slice(start_byte, len - start_byte);

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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MemCmpBits_Span(ReadOnlySpan<byte> left_arr, ReadOnlySpan<byte> right_arr, int len, int bits_begin)
        {
            int start_byte = bits_begin >> 3;
            left_arr = left_arr.Slice(start_byte, len - start_byte);
            right_arr = right_arr.Slice(start_byte, len - start_byte);
            
            byte mask = (byte) ((1 << (8 - (bits_begin & 7))) - 1);
            if ((left_arr[0] & mask) != (right_arr[0] & mask))
            {
                return (left_arr[0] & mask) - (right_arr[0] & mask);
            }

            if (len == 1)
                return 0;

            return left_arr.Slice(1).SequenceCompareTo(right_arr.Slice(1));
        }

        
        /*
        static unsafe int EqualBytesLongUnrolled (ReadOnlySpan<byte> left_arr, ReadOnlySpan<byte> right_arr, int len, int bits_begin)
        {
            if (left_arr == right_arr)
                return 0;
            
            int start_byte = bits_begin / 8;
            byte mask = (byte) ((1 << (8 - (bits_begin % 8))) - 1);
            if ((left_arr[start_byte] & mask) != (right_arr[start_byte] & mask))
            {
                return (left_arr[start_byte] & mask) - (right_arr[start_byte] & mask);
            }

            if (len == 1)
                return 0;
            
            len--;

            fixed (byte* bytes1 = left_arr.Slice(1), bytes2 = right_arr.Slice(1)) {
                int rem = len % (sizeof(long) * 16);
                long* b1 = (long*)bytes1;
                long* b2 = (long*)bytes2;
                long* e1 = (long*)(bytes1 + len - rem);

                while (b1 < e1) {
                    if (*(b1) != *(b2) || *(b1 + 1) != *(b2 + 1) || 
                        *(b1 + 2) != *(b2 + 2) || *(b1 + 3) != *(b2 + 3) ||
                        *(b1 + 4) != *(b2 + 4) || *(b1 + 5) != *(b2 + 5) || 
                        *(b1 + 6) != *(b2 + 6) || *(b1 + 7) != *(b2 + 7) ||
                        *(b1 + 8) != *(b2 + 8) || *(b1 + 9) != *(b2 + 9) || 
                        *(b1 + 10) != *(b2 + 10) || *(b1 + 11) != *(b2 + 11) ||
                        *(b1 + 12) != *(b2 + 12) || *(b1 + 13) != *(b2 + 13) || 
                        *(b1 + 14) != *(b2 + 14) || *(b1 + 15) != *(b2 + 15))
                        return false;
                    b1 += 16;
                    b2 += 16;
                }

                for (int i = 0; i < rem; i++)
                    if (left_arr [len - 1 - i] != right_arr [len - 1 - i])
                        return false;

                return true;
            }
        }
        */
        
    }
}