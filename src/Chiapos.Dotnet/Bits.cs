using System;
using Chiapos.Dotnet.Collections;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Bits : BitArray
    {
        public Bits(UInt128 value, int length) : base(value, length)
        {
        }

        public Bits(ReadOnlySpan<ulong> values, int valueLength) : base(values, valueLength)
        {
        }
    }
}