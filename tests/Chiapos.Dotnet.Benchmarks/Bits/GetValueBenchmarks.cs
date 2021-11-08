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
        
        [Params(1, 2, 3, 4, 5, 6, 7, 8)]
        public int m_length;
        
        private byte[] m_array = {1, 2, 3, 4, 5, 6, 7, 8};

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
        public ulong GetValueNew2()
        {
            int length = Math.Min(m_length, 8);
            var span = m_array.AsSpan();
            ulong result = 0;

            switch (length)
            {
                case 8:
                    return BinaryPrimitives.ReadUInt16LittleEndian(span);
                case 7:
                    return span[6] + ((ulong)BinaryPrimitives.ReadUInt16LittleEndian(span[4..]) << BitsPerByte)
                           + ((ulong)BinaryPrimitives.ReadUInt32LittleEndian(span) << BitsPerByte * 3);
                case 6:
                    return BinaryPrimitives.ReadUInt16LittleEndian(span[4..])
                                   + ((ulong)BinaryPrimitives.ReadUInt32LittleEndian(span) << BitsPerByte * 2);
                case 5:
                    return span[4] + ((ulong)BinaryPrimitives.ReadUInt32LittleEndian(span) << BitsPerByte);
                case 4:
                    return BinaryPrimitives.ReadUInt32LittleEndian(span);
                case 3:
                    return span[2] + ((ulong)BinaryPrimitives.ReadUInt16LittleEndian(span) << BitsPerByte);
                case 2:
                    return BinaryPrimitives.ReadUInt16LittleEndian(span);
                case 1:
                    return span[0];
            }

            return 0;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetValueNew()
        {
            int length = Math.Min(m_length, 8);
            var span = m_array.AsSpan();
            ulong result = 0;

            switch (length)
            {
                case 8:
                    return BinaryPrimitives.ReadUInt16LittleEndian(span);
                case 7:
                    return span[6] + ((ulong)span[5] << BitsPerByte) + ((ulong)span[4] << BitsPerByte * 2)
                           + ((ulong)BinaryPrimitives.ReadUInt32LittleEndian(span) << BitsPerByte * 3);
                case 6:
                    return span[5] + ((ulong)span[4] << BitsPerByte)
                           + ((ulong)BinaryPrimitives.ReadUInt32LittleEndian(span) << BitsPerByte * 2);
                case 5:
                    return span[4] + ((ulong)BinaryPrimitives.ReadUInt32LittleEndian(span) << BitsPerByte);
                case 4:
                    return BinaryPrimitives.ReadUInt32LittleEndian(span);
                case 3:
                    return span[2] + ((ulong)span[1] << BitsPerByte) + ((ulong)span[0] << BitsPerByte * 2);
                case 2:
                    return BinaryPrimitives.ReadUInt16LittleEndian(span);
                case 1:
                    return span[0];
            }

            return 0;
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

    }
}