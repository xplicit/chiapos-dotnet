using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace Chiapos.Dotnet.Benchmarks
{
    public class IsPositionEmptyBenchmarks
    {
        byte[] arr = new byte[1024];
        const int align = 3;
        private const int length = 21;
        
        [Benchmark]
        public void IsPositionEmptyUnsafe()
        {
            Util.IsPositionEmpty(arr.AsSpan(align, length));
        }

        [Benchmark(Baseline = true)]
        public void IsPositionEmptyNaive()
        {
            IsPositionEmpty(arr.AsSpan(align, length));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsPositionEmpty(ReadOnlySpan<byte> memory)
        {
            for (int i = 0; i < memory.Length; i++)
                if (memory[i] != 0)
                    return false;
            return true;
        }
    }
}