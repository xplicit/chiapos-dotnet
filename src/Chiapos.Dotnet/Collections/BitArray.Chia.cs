using System;
using System.Buffers.Binary;
using System.Diagnostics;
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
                m_array[^1] &= (1 << extraBits) - 1;
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
        
                /*=========================================================================
        ** Allocates space to hold the bit values in bytes. bytes[0] represents
        ** bits 0 - 7, bytes[1] represents bits 8 - 15, etc. The LSB of each byte
        ** represents the lowest index value; bytes[0] & 1 represents bit 0,
        ** bytes[0] & 2 represents bit 1, bytes[0] & 4 represents bit 2, etc.
        **
        ** Exceptions: ArgumentException if bytes == null.
        =========================================================================*/
        public BitArray(ReadOnlySpan<byte> bytes, int length)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            // this value is chosen to prevent overflow when computing m_length.
            // m_length is of type int32 and is exposed as a property, so
            // type of m_length can't be changed to accommodate.
            if (length > int.MaxValue / BitsPerByte)
                throw new ArgumentException(SR.Format(SR.Argument_ArrayTooLarge, BitsPerByte), nameof(bytes));

            if (length > bytes.Length * BitsPerByte)
                throw new NotImplementedException($"Number of bits should be less then number of bytes * {BitsPerByte}. Implement note: add zero bits at start");

            m_array = new int[GetInt32ArrayLengthFromBitLength(length)];
            m_length = length;

            uint totalCount = (uint)bytes.Length / 4;

            ReadOnlySpan<byte> byteSpan = bytes;
            for (int i = 0; i < totalCount; i++)
            {
                m_array[i] = BinaryPrimitives.ReadInt32LittleEndian(byteSpan);
                byteSpan = byteSpan.Slice(4);
            }

            Debug.Assert(byteSpan.Length >= 0 && byteSpan.Length < 4);

            int last = 0;
            switch (byteSpan.Length)
            {
                case 3:
                    last = byteSpan[2] << 16;
                    goto case 2;
                // fall through
                case 2:
                    last |= byteSpan[1] << 8;
                    goto case 1;
                // fall through
                case 1:
                    m_array[totalCount] = last | byteSpan[0];
                    break;
            }

            _version = 0;
        }

        
        public ulong GetValue()
        {
            if (m_array.Length != 2) {
                Console.WriteLine($"Number of 64 bit values is: {m_array.Length/2}");
                Console.WriteLine($"Size of bits is: {m_length}");
                throw new InvalidOperationException($"Number doesn't fit into a 64-bit type. {m_length}");
            }
            return (ulong)m_array[0] + ((ulong)m_array[1] << 32);
        }

        public BitArray AppendValue(UInt128 value, int length)
        {
            var newsize = m_length + length;
            //TODO: Remove Extra allocation and copying
            BitArray tmp = new BitArray(newsize);
            CopyTo(tmp.m_array, 0);
            
            int originalLength = Div32Rem(m_length, out int extraBits);
            if (extraBits != 0) originalLength++;

            if (extraBits == 0)
            {
                tmp.m_array[originalLength + 0] = value.I0;
                if (length > 32) tmp.m_array[originalLength + 1] = value.I1;
                if (length > 64) tmp.m_array[originalLength + 2] = value.I2;
                if (length > 96) tmp.m_array[originalLength + 3] = value.I3;
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
                tmp.m_array[originalLength - 1] |= (value.I0 & maskI0) << extraBits;
                if (length > freeBits) tmp.m_array[originalLength] = value.I0 >> freeBits;
                if (length > 32) tmp.m_array[originalLength] |= (value.I1 & maskI1) << extraBits;
                
                if (length - 32 > freeBits) tmp.m_array[originalLength + 1] = value.I1 >> freeBits;
                if (length > 64) tmp.m_array[originalLength + 1] |= (value.I2 & maskI2) << extraBits;
                
                if (length - 64 > freeBits) tmp.m_array[originalLength + 2] = value.I2 >> freeBits;
                if (length > 96) tmp.m_array[originalLength + 2] |= (value.I3 & maskI3) << extraBits;
                
                if (length - 96 > freeBits) tmp.m_array[originalLength + 3] = value.I3 >> freeBits;
            }

            m_array = tmp.m_array;
            m_length = tmp.m_length;

            return this;
        }

        public BitArray Slice(int start, int end)
        {
            BitArray result = new BitArray(end - start);
            int resultArrayLength = GetInt32ArrayLengthFromBitLength(end - start);
            int srcIndex = Div32Rem(start, out int shiftCount);
            int resultLastIndex = Div32Rem(end - start, out int resultShift);

            if (shiftCount == 0)
            {
                Array.Copy(m_array, srcIndex, result.m_array, 0, resultArrayLength);
            }
            else
            {
                int resultIndex = 0;
            
                for (int i = srcIndex; i < srcIndex + resultArrayLength - 1; i++)
                {
                    uint right = (uint)m_array[i] >> (shiftCount);
                    int left = m_array[i + 1] << (BitsPerInt32 - shiftCount);
                    result.m_array[resultIndex++] = left | (int)right;
                }

                if (shiftCount + resultShift > BitsPerInt32)
                {
                    uint right = (uint)m_array[srcIndex + resultArrayLength - 1] >> (shiftCount);
                    int left = m_array[srcIndex + resultArrayLength] << (BitsPerInt32 - shiftCount);
                    result.m_array[resultIndex] = left | (int)right;
                }
                else
                {
                    uint mask = shiftCount + resultShift == BitsPerInt32 ? 0xFFFFFFFF : 0xFFFFFFFF;
                    uint right = (uint)m_array[srcIndex + resultArrayLength - 1] >> (shiftCount);
                    result.m_array[resultIndex] = (int)(right & mask);
                }
            }

            return result;
        }

        public void ToBytes(byte[] array) => CopyTo(array, 0);
        public void ToBytes(byte[] array, int index) => CopyTo(array, index);

        public static BitArray operator + (BitArray a, BitArray b)
        {
            BitArray result = new BitArray(a.m_length + b.m_length);
            
            a.CopyTo(result.m_array, 0);
            
            var aLength = Div32Rem(a.m_length, out int shiftCount);
            var bLength = (b.m_length + BitsPerInt32 - 1) >> BitShiftPerInt32;
            var resultLength = Div32Rem(result.m_length, out int resultShift);

            if (shiftCount == 0)
            {
                Array.Copy(b.m_array, 0, result.m_array, aLength, bLength);
                return result;
            }

            int resultIndex = aLength;
            result.m_array[resultIndex++] |= b.m_array[0] << shiftCount;
            
            for (int i = 0; i < bLength - 1; i++)
            {
                uint right = (uint)b.m_array[i] >> (BitsPerInt32 - shiftCount);
                int left = b.m_array[i + 1] << shiftCount;
                result.m_array[resultIndex++] = left | (int)right;
            }

            if (resultShift > 0)
            {
                result.m_array[resultIndex] = b.m_array[^1] >> (BitsPerInt32 - shiftCount);
            }

            return result;
        }
    }
}