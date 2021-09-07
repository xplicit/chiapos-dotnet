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
                Util.IntTo16BytesOld(buffer, test);
            }
        }
    }
}