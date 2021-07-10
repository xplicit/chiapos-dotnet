using System;
using FiniteStateEntropy;

namespace Chiapos.Dotnet
{
    public class FseCompressTableSerialized
    {
        public ushort tableLog;
        public ushort maxSymbolValue;
        public ushort[] nextStateNumber;
        public FseSymbolCompressionTransform[] symbolTT;
    }
}