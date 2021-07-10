using System;
using Chiapos.Dotnet.Collections;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class ParkBits : BitArray
    {
        public ParkBits(UInt128 value, int length) : base(value, length)
        {
        }

        public ParkBits(ReadOnlySpan<ulong> values, int valueLength) : base(values, valueLength)
        {
        }
    }
}