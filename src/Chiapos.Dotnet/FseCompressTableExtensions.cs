using System;
using FiniteStateEntropy;

namespace Chiapos.Dotnet
{
    public static class FseCompressTableExtensions
    {
        public static FseCompressTable ToFseCompressTable(this FseCompressTableSerialized cts)
        {
            var ct = new FseCompressTable
            {
                tableLog = cts.tableLog,
                maxSymbolValue = cts.maxSymbolValue,
                nextStateNumber = new Span<ushort>(cts.nextStateNumber),
                symbolTT = new Span<FseSymbolCompressionTransform>(cts.symbolTT)
            };

            return ct;
        }

        public static FseCompressTableSerialized ToFseCompressTableSerialized(this FseCompressTable ct)
        {
            var cts = new FseCompressTableSerialized()
            {
                tableLog = ct.tableLog,
                maxSymbolValue = ct.maxSymbolValue,
                nextStateNumber = ct.nextStateNumber.ToArray(),
                symbolTT = ct.symbolTT.ToArray()
            };

            return cts;
        }
    }
}