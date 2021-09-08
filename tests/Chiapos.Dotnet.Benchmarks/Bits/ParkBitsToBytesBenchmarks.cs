using System;
using BenchmarkDotNet.Attributes;

namespace Chiapos.Dotnet.Benchmarks.Bits
{
    [MemoryDiagnoser]
    public class ParkBitsToBytesBenchmarks
    {
        ulong[] values = new ulong[1024];
        private byte[] buffer;
        const int parkBitsLength = 22;

        public ParkBitsToBytesBenchmarks()
        {
            ulong mask = (1UL << parkBitsLength) - 1;
            ulong[] values = new ulong[1024];

            Random rnd = new Random(10);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (ulong)rnd.Next() & mask;
            }
            
            buffer = new byte[parkBitsLength * values.Length / 8 + 8];
        }


        [Benchmark(Baseline = true)]
        public void ParkBitsConstructor()
        {
            ParkBits parkBits = new ParkBits();
            foreach (var stub in values)
            {
                parkBits.AppendValue(stub, parkBitsLength);
            }

            parkBits.ToBytes(buffer);
        }

        [Benchmark]
        public void ParkBitsDirectToMemory()
        {
            ParkBits.ToBytes(values, parkBitsLength, buffer);
        }
    }
}