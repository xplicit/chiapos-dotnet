using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using C5;
using Dirichlet.Numerics;
using FiniteStateEntropy;

namespace Chiapos.Dotnet
{
    public class ChiaEncoding
    {
        public static ConcurrentDictionary<double, FseCompressTableSerialized> compressCache = new();
        public static ConcurrentDictionary<double, FseDecompressTableSerialized> decompressCache = new();
        
            // Calculates x * (x-1) / 2. Division is done before multiplication.
            public static UInt128 GetXEnc(ulong x)
            {
                ulong a = x, b = x - 1;

                if (a % 2 == 0)
                    a /= 2;
                else
                    b /= 2;

                return (ulong) a * b;
            }

            // Encodes two max k bit values into one max 2k bit value. This can be thought of
            // mapping points in a two dimensional space into a one dimensional space. The benefits
            // of this are that we can store these line points efficiently, by sorting them, and only
            // storing the differences between them. Representing numbers as pairs in two
            // dimensions limits the compression strategies that can be used.
            // The x and y here represent table positions in previous tables.
            public static UInt128 SquareToLinePoint(ulong x, ulong y)
            {
                // Always makes y < x, which maps the random x, y  points from a square into a
                // triangle. This means less data is needed to represent y, since we know it's less
                // than x.
                if (y > x)
                {
                    var tmp = y;
                    y = x;
                    x = tmp;
                }
        
                return GetXEnc(x) + y;
            }
        
            // Does the opposite as the above function, deterministicaly mapping a one dimensional
            // line point into a 2d pair. However, we do not recover the original ordering here.
            public static (ulong, ulong) LinePointToSquare(UInt128 index)
            {
                // Performs a square root, without the use of doubles, to use the precision of the
                // uint128_t.
                ulong x = 0;
                for (int i = 63; i >= 0; i--) {
                    ulong new_x = x + (1UL << i);
                    if (GetXEnc(new_x) <= index)
                        x = new_x;
                }
                return (x, (ulong)(index - GetXEnc(x)));
            }
        
            public static List<short> CreateNormalizedCount(double R)
            {
                var dpdf = new List<double>();
                int N = 0;
                double E = 2.718281828459;
                double MIN_PRB_THRESHOLD = 1e-50;
                int TOTAL_QUANTA = 1 << 14;
                double p = 1 - Math.Pow((E - 1) / E, 1.0 / R);
        
                while (p > MIN_PRB_THRESHOLD && N < 255) {
                    dpdf.Add(p);
                    N++;
                    p = (Math.Pow(E, 1.0 / R) - 1) * Math.Pow(E - 1, 1.0 / R);
                    p /= Math.Pow(E, ((N + 1) / R));
                }

                var ans = Enumerable.Repeat<short>(1, N).ToList();

                var pq = new IntervalHeap<int>(new NormalizedComparer(dpdf, ans));
                
                for (int i = 0; i < N; ++i) pq.Add(i);
        
                for (int todo = 0; todo < TOTAL_QUANTA - N; ++todo) {
                    int i = pq.FindMax();
                    pq.DeleteMax();
                    ans[i]++;
                    pq.Add(i);
                }
        
                for (int i = 0; i < N; ++i) {
                    if (ans[i] == 1) {
                        ans[i] = -1;
                    }
                }
                return ans;
            }

            static FseCompressTable FseCreateCTable(int maxSymbolValue, int tableLog)
            {
                if (tableLog > FseBlockCompressor.FSE_TABLELOG_ABSOLUTE_MAX) tableLog = FseBlockCompressor.FSE_TABLELOG_ABSOLUTE_MAX;

                int CTableSize = sizeof(uint) * FseBlockCompressor.FseCompressTableSizeU32(tableLog, maxSymbolValue);
                var buffer = new byte[CTableSize];
                FseCompressTable CTable = FseBlockCompressor.AllocateCTable(buffer, tableLog, maxSymbolValue);

                return CTable;
            }
        
            public static int ANSEncodeDeltas(List<byte> deltas, double R, Span<byte> encoded)
            {
                var cts = compressCache.GetOrAdd(R, _ =>
                {
                    List<short> nCount = CreateNormalizedCount(R);
                    ushort maxSymbolValue = (ushort)(nCount.Count - 1);
                    ushort tableLog = 14;
        
                    if (maxSymbolValue > 255)
                        throw new ArgumentException("maxSymbolValue > 255");

                    FseCompressTable ct = FseCreateCTable(maxSymbolValue, tableLog);
                    
                    FseBlockCompressor.FseBuildCTable(ref ct, CollectionsMarshal.AsSpan(nCount),
                        maxSymbolValue, tableLog);

                    return ct.ToFseCompressTableSerialized();
                });

                var ct = cts.ToFseCompressTable();

               return FseBlockCompressor.FseCompressUsingCTable(encoded, CollectionsMarshal.AsSpan(deltas), ct);
            }
        
            public static void ANSFree(double R)
            {
                // Cache all entries, only free on close
            }
        
            public static byte[] ANSDecodeDeltas(byte[] input, int numDeltas, double R)
            {
                var dts = decompressCache.GetOrAdd(R, _ =>
                {
                    List<short> nCount = CreateNormalizedCount(R);
                    ushort maxSymbolValue = (ushort) (nCount.Count - 1);
                    ushort tableLog = 14;
                    int tableSize = 1 << tableLog;

                    var dt = new FseDecompressTable[tableSize];
                    FseBlockDecompressor.BuildDecodeTable(CollectionsMarshal.AsSpan(nCount),
                        maxSymbolValue, tableLog, dt, out var decompressTableHeader);

                    var dts = new FseDecompressTableSerialized() {Header = decompressTableHeader, Table = dt};
                    return dts;
                });
        
                var deltas = new byte[numDeltas];
                int decodedSymbols = FseBlockDecompressor.DecompressUsingTable(input, deltas, dts.Header, dts.Table);

                for (int i = 0; i < deltas.Length; i++)
                {
                    if (deltas[i] == 0xff) {
                        throw new InvalidOperationException("Bad delta detected");
                    }
                }
                
                return deltas;
            }

    }
}