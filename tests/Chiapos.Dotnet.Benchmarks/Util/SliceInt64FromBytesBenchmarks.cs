using System;
using BenchmarkDotNet.Attributes;

namespace Chiapos.Dotnet.Benchmarks
{
    [DisassemblyDiagnoser]
    public class SliceInt64FromBytesBenchmarks
    {
        private byte[] array;
        
        public SliceInt64FromBytesBenchmarks()
        {
            array = new byte[10_000_000];
        }
        
        [Benchmark]
        public void SliceIntNew()
        {
            var bytes = new ReadOnlySpan<byte>(array);
            Util.SliceInt64FromBytes(bytes, 0, 38);
            Util.SliceInt64FromBytes(bytes, 38, 42);
            Util.SliceInt64FromBytes(bytes, 80, 64);
            Util.SliceInt64FromBytes(bytes, 144, 64);
        }

        [Benchmark(Baseline = true)]
        public void SliceIntOld()
        {
            var bytes = new ReadOnlySpan<byte>(array);
            SliceInt64FromBytesOld(bytes, 0, 38);
            SliceInt64FromBytesOld(bytes, 38, 42);
            SliceInt64FromBytesOld(bytes, 80, 64);
            SliceInt64FromBytesOld(bytes, 144, 64);
        }
        
        public static ulong SliceInt64FromBytesOld(ReadOnlySpan<byte> bytes, uint start_bit, uint num_bits)
        {
            ulong tmp;
            uint index = 0;

            if (start_bit + num_bits > 64) {
                index = start_bit / 8;
                start_bit %= 8;
            }

            tmp = Util.EightBytesToInt(bytes.Slice((int)index));
            tmp <<= (int)start_bit;
            tmp >>= 64 - (int)num_bits;
            return tmp;
        }

    }
}