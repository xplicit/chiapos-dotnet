using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Bits
    {
        private ulong[] m_array;
        private int m_length;
        
        private const int BitsPerInt64 = 64;
        private const int BitsPerByte = 8;

        private const int BitShiftPerInt64 = 6;
        private const int BitShiftPerByte = 3;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetInt64ArrayLengthFromBitLength(int n)
        {
            Debug.Assert(n >= 0);
            return (int)((uint)(n - 1 + (1 << BitShiftPerInt64)) >> BitShiftPerInt64);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetByteArrayLengthFromBitLength(int n)
        {
            Debug.Assert(n >= 0);
            return (int)((uint)(n - 1 + (1 << BitShiftPerByte)) >> BitShiftPerByte);
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Div64Rem(int number, out int remainder)
        {
            uint quotient = (uint)number / 64;
            remainder = number & (64 - 1);
            return (int)quotient;
        }

        public Bits(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            m_array = new ulong[GetInt64ArrayLengthFromBitLength(length)];
            m_length = length;
        }

        public Bits(UInt128 value, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            
            m_array = new ulong[GetInt64ArrayLengthFromBitLength(length)];
            m_length = length;
            Div64Rem(length, out int extraBits);

            if (length <= BitsPerInt64)
            {
                m_array[0] = extraBits > 0 ? value.S0 & ((1UL << extraBits) - 1) : value.S0;
                return;
            }
            
            if (extraBits == 0)
            {
                m_array[0] = value.S1;
                m_array[1] = value.S0;
                return;
            }
            
            m_array[0] = value.S1 << (BitsPerInt64 - extraBits) | value.S0 >> extraBits;
            m_array[1] = value.S0 & ((1UL << extraBits) - 1);
        }

        public Bits(ReadOnlySpan<ulong> values, int valueLength)
        {
            m_length = valueLength;
            m_array = new ulong[GetInt64ArrayLengthFromBitLength(valueLength)];
            values.CopyTo(m_array);
        }

        public Bits(ReadOnlySpan<byte> bytes, int length)
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

            m_array = new ulong[GetInt64ArrayLengthFromBitLength(length)];
            m_length = length;

            uint totalCount = (uint)bytes.Length / 8;

            ReadOnlySpan<byte> byteSpan = bytes;
            for (int i = 0; i < totalCount; i++)
            {
                m_array[i] = BinaryPrimitives.ReadUInt64BigEndian(byteSpan);
                byteSpan = byteSpan[8..];
            }

            Debug.Assert(byteSpan.Length >= 0 && byteSpan.Length < 8);

            ulong last = 0;
            int shift = (byteSpan.Length << BitShiftPerByte) - BitsPerByte;
            int idx = 0;
            switch (byteSpan.Length)
            {
                case 7:
                    last = (ulong)byteSpan[idx++] << shift;
                    shift -= BitsPerByte;
                    goto case 6;
                case 6:
                    last |= (ulong)byteSpan[idx++] << shift;
                    shift -= BitsPerByte;
                    goto case 5;
                case 5:
                    last |= (ulong)byteSpan[idx++] << shift;
                    shift -= BitsPerByte;
                    goto case 4;
                case 4:
                    last |= (ulong)byteSpan[idx++] << shift;
                    shift -= BitsPerByte;
                    goto case 3;
                case 3:
                    last |= (ulong)byteSpan[idx++] << shift;
                    shift -= BitsPerByte;
                    goto case 2;
                // fall through
                case 2:
                    last |= (ulong)byteSpan[idx++] << shift;
                    shift -= BitsPerByte;
                    goto case 1;
                // fall through
                case 1:
                    last |= (ulong)byteSpan[idx] << shift;
                    m_array[totalCount] = last >> ((BitsPerByte - length) & ((1 << BitShiftPerByte) - 1));
                    break;
            }
        }

        public Bits AppendValue(UInt128 value, int length)
        {
            int originalLength = Div64Rem(m_length, out int extraBits);
            if (extraBits != 0) originalLength++;
            
            var newSize = m_length + length;
            //TODO: Remove Extra allocations
            Array.Resize(ref m_array, GetInt64ArrayLengthFromBitLength(newSize));
            m_length = newSize;

            Div64Rem(length, out int lengthShift);
            
            if (extraBits == 0)
            {
                if (length <= BitsPerInt64)
                {
                    m_array[originalLength + 0] = value.S0 & ((1UL << lengthShift) - 1);
                }
                else
                {
                    m_array[originalLength + 0] = value.S1 << (BitsPerInt64 - lengthShift) | value.S0 >> lengthShift;
                    m_array[originalLength + 1] = value.S0 & ((1UL << lengthShift) - 1);
                }
            }
            else
            {
                if (length <= BitsPerInt64)
                {
                    if (length <= BitsPerInt64 - extraBits)
                    {
                        m_array[originalLength - 1] <<= length;
                        m_array[originalLength - 1] |= value.S0 & ((1UL << length) - 1);
                    }
                    else
                    {
                        m_array[originalLength - 1] <<= BitsPerInt64 - extraBits;
                        m_array[originalLength - 1] |= value.S0 >> extraBits;
                        m_array[originalLength] = value.S0 & ((1UL << extraBits) - 1);
                    }
                }
                else
                {
                    m_array[originalLength - 1] <<= BitsPerInt64 - extraBits;

                    if (length - BitsPerInt64 <= BitsPerInt64 - extraBits)
                    {
                        m_array[originalLength - 1] |= (value.S1 & ((1UL << length - BitsPerInt64) - 1)) << (2 * BitsPerInt64 - length - extraBits);
                        if (length - BitsPerInt64 < BitsPerInt64 - extraBits)
                        {
                            m_array[originalLength - 1] |= value.S0 >> (length - BitsPerInt64 + extraBits);
                            m_array[originalLength] = value.S0 & ((1UL << (length - BitsPerInt64 + extraBits)) - 1);
                        }
                        else
                        {
                            m_array[originalLength] = value.S0;
                        }
                    }
                    else
                    {
                        var bitsShift = (length - BitsPerInt64) - (BitsPerInt64 - extraBits);
                        m_array[originalLength - 1] |= value.S1 >> bitsShift;
                        m_array[originalLength] = (value.S1 << BitsPerInt64 - bitsShift) | value.S0 >> bitsShift;
                        m_array[originalLength + 1] = value.S0 & ((1UL << (BitsPerInt64 - bitsShift)) - 1);
                    }
                }
            }

            return this;
        }

        public ulong GetValue()
        {
            if (m_array.Length != 1) {
                Console.WriteLine($"Number of 64 bit values is: {m_array.Length/2}");
                Console.WriteLine($"Size of bits is: {m_length}");
                throw new InvalidOperationException($"Number doesn't fit into a 64-bit type. {m_length}");
            }

            return m_array[0];
        }

        public void ToBytes(byte[] array) => ToBytes(array, 0);

        public void ToBytes(byte[] array, int index)
        {
            var span = array.AsSpan();
            
            int length = Div64Rem(m_length, out int shift);
            for (int i = 0; i < length; i++)
            {
                BinaryPrimitives.WriteUInt64BigEndian(span, m_array[i]);
                span = span[8..];
            }

            if (shift > 0)
            {
                ulong value = m_array[length] << (BitsPerInt64 - shift);
                int startBit = BitsPerInt64 - BitsPerByte;

                while (startBit > BitsPerInt64 - shift)
                {
                    span[0] = (byte)(value >> startBit);
                    span = span[1..];
                    startBit -= BitsPerByte;
                }

                span[0] = (byte)(value >> startBit);
            }
        }

        public static Bits operator + (Bits a, Bits b)
        {
            Bits result = new Bits(a.m_length + b.m_length);
            
            int aLength = GetInt64ArrayLengthFromBitLength(a.m_length);
            Div64Rem(a.m_length, out int aShiftCount);

            Array.Copy(a.m_array, 0, result.m_array, 0, aLength);
            
            var bLength = GetInt64ArrayLengthFromBitLength(b.m_length);
            var bLastIndex = Div64Rem(b.m_length, out int bShiftCount);

            if (aShiftCount == 0)
            {
                Array.Copy(b.m_array, 0, result.m_array, aLength, bLength);
                return result;
            }

            ulong left, right;
            
            int resultIndex = aLength - 1;
            int bIndex = 0;

            if (b.m_length <= BitsPerInt64)
            {
                //All b array can fit into a array
                if (BitsPerInt64 - aShiftCount >= b.m_length)
                {
                    left = a.m_array[resultIndex] << b.m_length;
                    result.m_array[resultIndex] = left | b.m_array[bIndex];
                }
                else
                {
                    int lastShift = aShiftCount - (BitsPerInt64 - b.m_length);
                    left = a.m_array[resultIndex] << (BitsPerInt64 - aShiftCount);
                    right = b.m_array[bIndex] >> lastShift;
                    result.m_array[resultIndex++] = left | right;

                    right = b.m_array[bIndex] & ((1UL << lastShift) - 1);
                    result.m_array[resultIndex] = right;
                }

                return result;
            }

            left = a.m_array[aLength - 1] << (BitsPerInt64 - aShiftCount);
            right = b.m_array[bIndex] >> aShiftCount;
            result.m_array[resultIndex++] = left | right;

            int remainingBits = b.m_length - (BitsPerInt64 - aShiftCount);

            while (remainingBits > 2 * BitsPerInt64)
            {
                left = b.m_array[bIndex++] << (BitsPerInt64 - aShiftCount);
                right = b.m_array[bIndex] >> aShiftCount;
                result.m_array[resultIndex++] = left | right;
                remainingBits -= BitsPerInt64;
            }

            if (remainingBits > BitsPerInt64)
            {
                left = b.m_array[bIndex++] << (BitsPerInt64 - aShiftCount);
                right = b.m_array[bIndex] >> (aShiftCount - (bIndex == bLastIndex ? BitsPerInt64 - bShiftCount : 0));
                result.m_array[resultIndex++] = left | right;
                remainingBits -= BitsPerInt64;
            }

            //Now we have two cases:
            //1. b fits to the last ulong in result array  ((BitsPerInt64 - aShiftCount) bits are free)
            //2. b does not fit in the last ulong and we need to make one additional copy
            
            //all bits in the last ulong of b
            if (remainingBits <= bShiftCount || bShiftCount == 0)
            {
                Debug.Assert(bIndex == ((bShiftCount == 0) ? bLastIndex - 1 : bLastIndex));
                result.m_array[resultIndex] = b.m_array[bIndex] & ((1UL << remainingBits) - 1);
            }
            else //we have two ulongs
            {
                Debug.Assert(bIndex == bLastIndex - 1);
                left = (b.m_array[bIndex++] & ((1UL << (bShiftCount)) - 1)) << bShiftCount;
                right = b.m_array[bIndex];
                result.m_array[resultIndex] = left | right;
            } 
            
            return result;
        }

        public Bits Slice(int start) => Slice(start, m_length);

        public Bits Slice(int start, int end)
        {
            if (end > m_length)
                end = m_length;
            
            Bits result = new Bits(end - start);
            int resultArrayLength = GetInt64ArrayLengthFromBitLength(end - start);
            int startBitIndex = Div64Rem(start, out int startShiftCount);
            Div64Rem(m_length, out int srcLengthShift);
            int srcLength = GetInt64ArrayLengthFromBitLength(m_length);
            int endIndex = Div64Rem(end, out int endShiftCount);
            int resultLastIndex = Div64Rem(end - start, out int resultShift);

            if (startShiftCount == 0)
            {
                Array.Copy(m_array, startBitIndex, result.m_array, 0, resultArrayLength);
                if (resultShift != 0)
                {
                    ulong y = result.m_array[^1] >> (srcLengthShift - resultShift);
                    result.m_array[^1] = y;
                }
            }
            else
            {
                //Source is only 1 64bit ulong
                if (srcLength == 1)
                {
                    ulong value = m_array[0] >> (m_length - endShiftCount);
                    value &= (1UL << (endShiftCount - startShiftCount)) - 1;

                    result.m_array[0] = value;
                }
                else
                {
                    int lastIndex = startBitIndex + resultLastIndex;
                    int dstBits = end - start;
                    
                    //copy all whole ulongs to new array
                    int resultIndex = 0;
                    int srcIndex = startBitIndex;
                    while (dstBits >= 2 * BitsPerInt64)
                    {
                        ulong left = m_array[srcIndex++] << startShiftCount;
                        ulong right = m_array[srcIndex] >> (BitsPerInt64 - startShiftCount);
                        result.m_array[resultIndex++] = left | right;
                        dstBits -= BitsPerInt64;
                    }

                    //end bit is inside whole src ulong
                    /*if (lastIndex == endIndex)
                    {
                        int shiftCount = (endShiftCount - startShiftCount) & 0x3F;
                        ulong mask = shiftCount == 0 ? 0xFFFF_FFFF : (1UL << shiftCount) - 1;
                        
                        result.m_array[resultIndex] = (m_array[lastIndex] >> ( BitsPerInt64 - endShiftCount)) & mask;
                        return result;
                    }*/

                    //last non-whole ulong can fit into resulting ulong (it has startShiftCount bits free)
                    if (endShiftCount <= startShiftCount)
                    {
                        ulong left, right;
                        if (dstBits > BitsPerInt64)
                        {
                            left = m_array[srcIndex++] << startShiftCount;
                            right = m_array[srcIndex] >> (BitsPerInt64 - startShiftCount);
                            result.m_array[resultIndex++] = left | right;
                            dstBits -= BitsPerInt64;
                        }

                        if (endShiftCount == 0)
                        {
                            result.m_array[resultIndex] =m_array[srcIndex] & ((1UL << (BitsPerInt64 - startShiftCount)) - 1);
                        }
                        else
                        {
                            left = m_array[srcIndex++] << startShiftCount;
                            left >>= startShiftCount - endShiftCount;

                            //two cases: src can be last byte or not last byte
                            int lastShift = srcIndex == srcLength - 1 ? srcLengthShift - endShiftCount : BitsPerInt64 - endShiftCount;
                            right = m_array[srcIndex] >> lastShift;

                            result.m_array[resultIndex] = left | right;
                        }
                    }
                    else
                    {
                        //FIXIT: we need to calculate lastShift depending on src last byte
                        int lastShift;
                        
                        if (dstBits > BitsPerInt64)
                        {
                            ulong left = m_array[srcIndex++] << startShiftCount;
                            ulong right = m_array[srcIndex] >> (srcLengthShift - startShiftCount) & 0x3F;
                            result.m_array[resultIndex++] = left | right;
                            dstBits -= BitsPerInt64;
                        }

                        int remainingBits = endShiftCount - startShiftCount;
                        ulong mask = (1UL << remainingBits) - 1;
                        result.m_array[resultIndex] = (m_array[srcIndex] >> ((srcLengthShift - endShiftCount) & 0x3F)) & mask;
                    }
                }
            }

            return result;
        }

        
        public int Length
        {
            get
            {
                return m_length;
            }
            set
            {
              /*  if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_NeedNonNegNum);
                }

                int newints = GetInt32ArrayLengthFromBitLength(value);
                if (newints > m_array.Length || newints + _ShrinkThreshold < m_array.Length)
                {
                    // grow or shrink (if wasting more than _ShrinkThreshold ints)
                    Array.Resize(ref m_array, newints);
                }

                if (value > m_length)
                {
                    // clear high bit values in the last int
                    int last = (m_length - 1) >> BitShiftPerInt32;
                    Div32Rem(m_length, out int bits);
                    if (bits > 0)
                    {
                        m_array[last] &= (1 << bits) - 1;
                    }

                    // clear remaining int values
                    m_array.AsSpan(last + 1, newints - last - 1).Clear();
                }

                m_length = value;
                _version++;
                */
            }
        }

    }
}