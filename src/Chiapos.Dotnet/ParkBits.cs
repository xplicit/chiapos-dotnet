using System;
using System.Buffers.Binary;
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

        public static int ToBytes(ReadOnlySpan<ulong> values, int parkBits, Span<byte> buffer)
        {
            int bitIndex = 0;
            int bitMask = BitsPerByte - 1;
            ulong parkBitMask = (1UL << parkBits) - 1;

            //TODO: This implementaion makes too much uneccessary writes
            //we can merge only parkbits values without rewriting the whole ulong
            for (int i = 0; i < values.Length; i++)
            {
                ulong mask = 256 - (1UL << (BitsPerByte - (bitIndex & bitMask)));
                ulong alignedValue = (values[i] & parkBitMask) << (BitsPerInt64 - parkBits - (bitIndex & bitMask));
                ulong byteValue = ((ulong)buffer[bitIndex >> BitShiftPerByte] & mask) << (BitsPerInt64 - BitsPerByte);
                BinaryPrimitives.WriteUInt64BigEndian(buffer.Slice(bitIndex >> BitShiftPerByte), byteValue | alignedValue);
                bitIndex += parkBits;
            }

            return bitIndex;
        }
    }
}