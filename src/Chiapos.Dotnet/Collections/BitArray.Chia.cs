using System;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet.Collections
{
    public partial class BitArray
    {
        public BitArray(UInt128 value, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            
            m_array = new int[GetInt32ArrayLengthFromBitLength(length)];
            m_length = length;

            m_array[0] = value.I0;
            if (length > 32) m_array[1] = value.I1;
            if (length > 64) m_array[2] = value.I2;
            if (length > 96) m_array[3] = value.I3;
            
            Div32Rem(length, out int extraBits);
            if (extraBits > 0)
            {
                m_array[^1] = (1 << extraBits) - 1;
            }
        }

        public BitArray(ReadOnlySpan<ulong> values, int valueLength)
        {
            if (valueLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(valueLength), valueLength, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            m_length = valueLength * values.Length;
            m_array = new int[GetInt32ArrayLengthFromBitLength(m_length)];

            if (valueLength == 32)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    m_array[i] = (int) values[i];
                }
                return;
            }

            var shift = 0;
            int shiftMask;
            int idx = 0;
            for (int i = 0; i < values.Length; i++)
            {
                ulong value = valueLength == 64 ? values[i] : values[i] & ((1UL << valueLength) - 1);
                
                if (valueLength > 32)
                {
                    if (shift == 0)
                    {
                        m_array[idx++] = (int) value;
                        shift = (shift + valueLength) & 0x1F;
                        shiftMask = (1 << shift) - 1;
                        m_array[idx] = (int)(value >> 32) & shiftMask;
                        if (shift == 0) idx++;
                    }
                    else
                    {
                        // remainder to 32 bit 
                        shiftMask = (1 << (32 - shift)) - 1;
                        m_array[idx++] |= ((int) value & shiftMask) << shift;
                        m_array[idx] = (int) value >> (32 - shift);
                        m_array[idx] |= ((int) (value >> 32) & shiftMask) << shift;
                        if (shift + valueLength > 64)
                        {
                            m_array[++idx] = (int) (value >> (64 - shift));
                        }
                        shift = (shift + valueLength) & 0x1F;
                        if (shift == 0) idx++;
                    }
                }
                else
                {
                    unchecked
                    {
                        shiftMask = (1 << valueLength) - 1;
                    }
                    m_array[idx] |= ((int) value& shiftMask) << shift;
                    if (shift + valueLength > 32)
                    {
                        m_array[++idx] = (int) value >> (32 - shift);
                    }
                    shift = (shift + valueLength) & 0x1F;
                    if (shift == 0) idx++;
                }
            }
        }
        
        public ulong GetValue()
        {
            if (m_array.Length != 2) {
                Console.WriteLine($"Number of 64 bit values is: {m_array.Length/2}");
                Console.WriteLine($"Size of bits is: {m_length}");
                throw new InvalidOperationException($"Number doesn't fit into a 64-bit type. {m_length}");
            }
            return (ulong)m_array[0] + (ulong)m_array[1] << 32;
        }

        public void AppendValue(UInt128 value, int length)
        {
            var newsize = m_length + length;
            //TODO: Remove Extra allocation and copying
            BitArray tmp = new BitArray(newsize);
            CopyTo(tmp.m_array, 0);
            
            Div32Rem(m_length, out int extraBits);

            if (extraBits == 0)
            {
                tmp.m_array[m_array.Length + 0] = value.I0;
                if (length > 32) tmp.m_array[m_array.Length + 1] = value.I1;
                if (length > 64) tmp.m_array[m_array.Length + 2] = value.I2;
                if (length > 96) tmp.m_array[m_array.Length + 3] = value.I3;
            }
            else
            {
                var freeBits = sizeof(int) * 8 - extraBits;
                Div32Rem(tmp.m_length, out int newExtraBits);
                int maskI0, maskI1, maskI2, maskI3;
                
                unchecked
                {
                    // remainder to 32 bit 
                    var shiftMask = (1 << length & 0x1F) - 1;
                    maskI0 = length >= 32 ? (int)0xFFFF_FFFF : shiftMask;
                    maskI1 = length >= 64 ? (int)0xFFFF_FFFF : shiftMask;
                    maskI2 = length >= 96 ? (int)0xFFFF_FFFF : shiftMask;
                    maskI3 = length == 128 ? (int)0xFFFF_FFFF : shiftMask;
                }
                
                //first int
                tmp.m_array[m_array.Length - 1] |= (value.I0 & maskI0) << extraBits;
                if (length > freeBits) tmp.m_array[m_array.Length] = value.I0 >> freeBits;
                if (length > 32) tmp.m_array[m_array.Length] |= (value.I1 & maskI1) << extraBits;
                
                if (length - 32 > freeBits) tmp.m_array[m_array.Length + 1] = value.I1 >> freeBits;
                if (length > 64) tmp.m_array[m_array.Length + 1] |= (value.I2 & maskI2) << extraBits;
                
                if (length - 64 > freeBits) tmp.m_array[m_array.Length + 2] = value.I2 >> freeBits;
                if (length > 96) tmp.m_array[m_array.Length + 2] |= (value.I3 & maskI3) << extraBits;
                
                if (length - 96 > freeBits) tmp.m_array[m_array.Length + 3] = value.I3 >> freeBits;
            }

            m_array = tmp.m_array;
            m_length = tmp.m_length;
        }

        public void ToBytes(byte[] array) => CopyTo(array, 0);
        public void ToBytes(byte[] array, int index) => CopyTo(array, index);
    }
}