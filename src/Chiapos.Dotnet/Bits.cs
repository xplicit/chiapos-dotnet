using System;
using Chiapos.Dotnet.Collections;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Bits : BitArray
    {
        public Bits(BitArray value) : base(value)
        {
        }

        public Bits(UInt128 value, int length) : base(value, length)
        {
        }

        public Bits(ReadOnlySpan<ulong> values, int valueLength) : base(values, valueLength)
        {
        }

        public Bits(ReadOnlySpan<byte> values, int bitsLength) : base(values, bitsLength)
        {
        }

        public new Bits AppendValue(UInt128 value, int length) => (Bits) base.AppendValue(value, length);

        public static Bits operator +(Bits a, Bits b) => new Bits((BitArray) a + (BitArray) b);
    }
}