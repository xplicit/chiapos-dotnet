using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Chiapos.Dotnet.Benchmarks.Bits
{
    public class GetValueBenchmarks
    {
        protected const byte BitShiftMask = 7;
        protected const int BitsPerByte = 8;
        protected const int BitsPerInt64 = 64;

        
        [Params(1, 2, 3, 4, 5, 6, 7, 8)]
        public int m_length;
        
        private byte[] m_array = {1, 2, 3, 4, 5, 6, 7, 8};

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetValue8Bytes()
        {
            int shift = m_length & (BitsPerInt64 - 1);
            
            ulong result = BinaryPrimitives.ReadUInt64BigEndian(m_array);
            return result >> ((BitsPerInt64 - shift) & (BitsPerInt64 - 1));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetValueOriginal()
        {
            int shift = m_length & BitShiftMask;

            int endByte = Math.Min(m_length, 8);

            ulong result = 0;
            int byteShift = 0;
            for (int i = endByte - 1; i >= 0; i--)
            {
                result += (ulong)m_array[i] << byteShift;
                byteShift += 8;
            }

            return result >> ((BitsPerByte - shift) & BitShiftMask);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ulong GetValueUnsafe()
        {
            int length = Math.Min(m_length, 8);
            ulong result = 0;

            fixed (byte* memoryPtr = m_array)
            {
                switch (length)
                {
                    case 8:
                        return *(ulong *)memoryPtr;
                    case 7:
                        return *(memoryPtr + 6) + ((ulong) *(ushort *)(memoryPtr + 4) << BitsPerByte)
                                       + ((ulong) *(uint *)memoryPtr << BitsPerByte * 3);
                    case 6:
                        return *(ushort *)(memoryPtr + 4) + ((ulong) *(uint *)memoryPtr << BitsPerByte * 2);
                    case 5:
                        return *(memoryPtr + 4) + ((ulong) * (uint *)memoryPtr << BitsPerByte);
                    case 4:
                        return *(uint *)memoryPtr;
                    case 3:
                        return *(memoryPtr + 2) + ((ulong) * (ushort *)memoryPtr << BitsPerByte);
                    case 2:
                        return * (ushort *)memoryPtr;
                    case 1:
                        return *memoryPtr;
                }
            }

            return 0;
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetValueNew2()
        {
            int shift = m_length & BitShiftMask;
            int length = Math.Min(m_length, 8);
            var span = m_array.AsSpan();
            ulong result = 0;

            switch (length)
            {
                case 8:
                    result =  BinaryPrimitives.ReadUInt64BigEndian(span);
                    break;
                case 7:
                    result =  span[6] + ((ulong)BinaryPrimitives.ReadUInt16BigEndian(span[4..]) << BitsPerByte)
                                      + ((ulong)BinaryPrimitives.ReadUInt32BigEndian(span) << BitsPerByte * 3);
                    break;
                case 6:
                    result =  BinaryPrimitives.ReadUInt16BigEndian(span[4..])
                              + ((ulong)BinaryPrimitives.ReadUInt32BigEndian(span) << BitsPerByte * 2);
                    break;
                case 5:
                    result =  span[4] + ((ulong)BinaryPrimitives.ReadUInt32BigEndian(span) << BitsPerByte);
                    break;
                case 4:
                    result =  BinaryPrimitives.ReadUInt32BigEndian(span);
                    break;
                case 3:
                    result =  span[2] + ((ulong)BinaryPrimitives.ReadUInt16BigEndian(span) << BitsPerByte);
                    break;
                case 2:
                    result =  BinaryPrimitives.ReadUInt16BigEndian(span);
                    break;
                case 1:
                    result =  span[0];
                    break;
            }

            return result >> ((BitsPerByte - shift) & BitShiftMask);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetValueNew()
        {
            int shift = m_length & BitShiftMask;
            int length = Math.Min(m_length, 8);
            var span = m_array.AsSpan();
            ulong result = 0;

            switch (length)
            {
                case 8:
                    result = BinaryPrimitives.ReadUInt64BigEndian(span);
                    break;
                case 7:
                    result = span[6] + ((ulong)span[5] << BitsPerByte) + ((ulong)span[4] << BitsPerByte * 2)
                             + ((ulong)BinaryPrimitives.ReadUInt32BigEndian(span) << BitsPerByte * 3);
                    break;
                case 6:
                    result = span[5] + ((ulong)span[4] << BitsPerByte)
                                     + ((ulong)BinaryPrimitives.ReadUInt32BigEndian(span) << BitsPerByte * 2);
                    break;
                case 5:
                    result = span[4] + ((ulong)BinaryPrimitives.ReadUInt32BigEndian(span) << BitsPerByte);
                    break;
                case 4:
                    result = BinaryPrimitives.ReadUInt32BigEndian(span);
                    break;
                case 3:
                    result = span[2] + ((ulong)span[1] << BitsPerByte) + ((ulong)span[0] << BitsPerByte * 2);
                    break;
                case 2:
                    result = BinaryPrimitives.ReadUInt16BigEndian(span);
                    break;
                case 1:
                    result = span[0];
                    break;
            }
                
            return result >> ((BitsPerByte - shift) & BitShiftMask);
        }

        [Benchmark(Baseline = true)]
        public void GetValue_Original()
        {
            GetValueOriginal();
        }
        
        [Benchmark]
        public void GetValue_New()
        {
            GetValueNew();
        }
        
        [Benchmark]
        public void GetValue_New2()
        {
            GetValueNew2();
        }

        [Benchmark]
        public void GetValue_8Bytes()
        {
            GetValue8Bytes();
        }

        //[Benchmark]
        public void GetValue_Unsafe()
        {
            GetValueUnsafe();
        }
    }
}