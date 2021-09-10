using System;
using BenchmarkDotNet.Attributes;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet.Benchmarks
{
    public class IntTo16BytesBenchmarks
    {
        private UInt128 test = ((UInt128)0x1234567890ABCDEF << 64) + (UInt128)0xFEDCBA0987654321;
        private byte[] buffer = new byte[16];
        
        [Benchmark]
        public void IntTo16Bytes()
        {
            for (int i = 0; i < 1000; i++)
            {
                Util.IntTo16Bytes(buffer, test);
            }
        }
        
        [Benchmark(Baseline = true)]
        public void IntTo16BytesOld()
        {
            for (int i = 0; i < 1000; i++)
            {
                IntTo16BytesOld(buffer, test);
            }
        }
        
        public static void IntTo16BytesOld(Span<byte> result, UInt128 input)
        {
            ulong r = SwapBytes(input.S1);
            BitConverter.TryWriteBytes(result, r);

            r = SwapBytes(input.S0);
            BitConverter.TryWriteBytes(result[8..], r);
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
    }
}