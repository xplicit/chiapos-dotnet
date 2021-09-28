using System;
using BenchmarkDotNet.Attributes;

namespace Chiapos.Dotnet.Benchmarks
{
    public class ArrayBenchmarks
    {
        public const int Size1 = 2;
        public const int Size2 = 15153;
        public const int Size3 = 64;
        public const int size1Shift = 1;
        public const int size3Shift = 6;

        public ushort[,,] threeDimArray = new ushort[Size1, Size2, Size3];
        public ushort[] oneDimArray = new ushort[Size1 * Size2 * Size3];
        static int[] oneDimIntArray = new int[Size1 * Size2 * Size3];
        
        [Benchmark(Baseline = true)]
        public void ThreeDimArray()
        {
            ushort result = 0;
            for (int s3 = 0; s3 < Size3; s3++)
            {
                for (int s2 = 0; s2 < Size2; s2++)
                {
                    for (int s1 = 0; s1 < Size1; s1++)
                    {
                        unchecked
                        {
                            result += threeDimArray[s1, s2, s3];
                        }
                    }
                }
            }
        }

        
        [Benchmark]
        public void OneDimSpanOptimized()
        {
            ushort result = 0;
            Span<ushort> span = oneDimArray.AsSpan(0, oneDimArray.Length);
            
            for (int s3 = 0; s3 < Size3; s3++)
            {
                for (int s2 = 0; s2 < Size2; s2++)
                {
                    for (int s1 = 0; s1 < Size1; s1++)
                    {
                        unchecked
                        {
                            result += span[(s2 << (size1Shift + size3Shift)) + (s1 << size3Shift) + s3];
                        }
                    }
                }
            }
        }

        [Benchmark]
        public void OneDimIntSpanOptimized()
        {
            int result = 0;
            Span<int> span = oneDimIntArray.AsSpan(0, oneDimIntArray.Length);
            
            for (int s3 = 0; s3 < Size3; s3++)
            {
                for (int s2 = 0; s2 < Size2; s2++)
                {
                    for (int s1 = 0; s1 < Size1; s1++)
                    {
                        unchecked
                        {
                            result += span[(s2 << (size1Shift + size3Shift)) + (s1 << size3Shift) + s3];
                        }
                    }
                }
            }
        }
        
        [Benchmark]
        public void OneDimArrayOptimized()
        {
            ushort result = 0;
            for (int s3 = 0; s3 < Size3; s3++)
            {
                for (int s2 = 0; s2 < Size2; s2++)
                {
                    for (int s1 = 0; s1 < Size1; s1++)
                    {
                        unchecked
                        {
                            result += oneDimArray[(s2 << (size1Shift + size3Shift)) + (s1 << size3Shift) + s3];
                        }
                    }
                }
            }
        }
        
        [Benchmark]
        public void OneDimIntOptimized()
        {
            int result = 0;
            
            for (int s3 = 0; s3 < Size3; s3++)
            {
                for (int s2 = 0; s2 < Size2; s2++)
                {
                    for (int s1 = 0; s1 < Size1; s1++)
                    {
                        unchecked
                        {
                            result += oneDimIntArray[(s2 << (size1Shift + size3Shift)) + (s1 << size3Shift) + s3];
                        }
                    }
                }
            }
        }
        
        [Benchmark]
        public void OneDimArray()
        {
            ushort result = 0;
            for (int s3 = 0; s3 < Size3; s3++)
            {
                for (int s2 = 0; s2 < Size2; s2++)
                {
                    for (int s1 = 0; s1 < Size1; s1++)
                    {
                        unchecked
                        {
                            result += oneDimArray[Size2 * Size3 *s1 + Size3 * s2 + s3];
                        }
                    }
                }
            }
        }

    }
}