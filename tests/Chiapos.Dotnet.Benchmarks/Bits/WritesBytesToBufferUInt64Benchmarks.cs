using System;
using BenchmarkDotNet.Attributes;

namespace Chiapos.Dotnet.Benchmarks.Bits
{
    public class WritesBytesToBufferUInt64Benchmarks
    {
        private byte[] buffer = new byte[16];

        [Benchmark]
        public void Write32BitNew()
        {
            var dstBuffer = buffer.AsSpan();
            Bits2.WriteBytesToBuffer(dstBuffer, 6, 0x23456789, 32);
        }

        [Benchmark(Baseline = true)]
        public void Write32BitOld2()
        {
            var dstBuffer = buffer.AsSpan();
            Bits2.WriteBytesToBufferOld2(dstBuffer, 6, 0x23456789, 32);
        }
        
        [Benchmark]
        public void Write32BitOld()
        {
            var dstBuffer = buffer.AsSpan();
            Bits2.WriteBytesToBufferOld(dstBuffer, 6, 0x23456789, 32);
        }
        
        [Benchmark]
        public void Write25BitNew()
        {
            var dstBuffer = buffer.AsSpan();
            Bits2.WriteBytesToBuffer(dstBuffer, 6, 0x23456789, 25);
        }
        
        [Benchmark]
        public void Write25BitOld2()
        {
            var dstBuffer = buffer.AsSpan();
            Bits2.WriteBytesToBufferOld2(dstBuffer, 6, 0x23456789, 25);
        }

    }
}