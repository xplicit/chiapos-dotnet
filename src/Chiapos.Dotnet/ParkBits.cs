using System;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class ParkBits : Bits
    {
        public ParkBits() : base(0)
        {
            
        }
        public ParkBits(UInt128 value, int length) : base(value, length)
        {
        }

        public ParkBits(ReadOnlySpan<ulong> values, int valueLength) : base(values, valueLength)
        {
        }
    }
}