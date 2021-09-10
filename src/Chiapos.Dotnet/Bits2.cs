using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    public class Bits2
    {
        private int m_length;
        private byte[] m_array;

        protected const int BitsPerByte = 8;
        protected const int BitShiftPerByte = 3;
        protected const byte BitShiftMask = 7;
        
        protected const int BitsPerInt64 = 64;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetByteArrayLengthFromBitLength(int n)
        {
            Debug.Assert(n >= 0);
            return (int)((uint)(n - 1 + (1 << BitShiftPerByte)) >> BitShiftPerByte);
        }
        
        public Bits2(int length)
        {
            m_array = new byte[GetByteArrayLengthFromBitLength(length)];
            m_length = length;
        }

        public byte[] GetBuffer() => m_array;
        
        public ulong GetValue()
        {
            int shift = m_length & BitShiftMask;

            int endByte = Math.Min(m_array.Length, 8);

            ulong result = 0;
            int byteShift = 0;
            for (int i = endByte - 1; i >= 0; i--)
            {
                result += (ulong)m_array[i] << byteShift;
                byteShift += 8;
            }

            return result >> ((BitsPerByte - shift) & BitShiftMask);
        }

        public Bits2(ulong value, int bitLength)
        {
            m_array = new byte[GetByteArrayLengthFromBitLength(bitLength)];
            m_length = bitLength;

            WriteBytesToBuffer(m_array, 0, value, bitLength);
        }

        public Bits2(ReadOnlySpan<byte> buffer, int startBit, int bitLength)
        {
            m_array = new byte[GetByteArrayLengthFromBitLength(bitLength)];
            m_length = bitLength;

            var span = buffer.Slice(startBit >> BitShiftPerByte,
                 GetByteArrayLengthFromBitLength(startBit + bitLength) - (startBit >> BitShiftPerByte));
            
            //we skip the case when bitLength <=8, because it's not used in plotter.
            //If we need this in future, just add if (bitLength <= 8) {}
            if ((startBit & BitShiftMask) == 0)
            {
                span.CopyTo(m_array);
                if ((bitLength & BitShiftMask) != 0)
                {
                    m_array[^1] &= (byte)(256 - (1 << (BitsPerByte - (bitLength & BitShiftMask))));
                }
                return;
            }

            var startShift = startBit & BitShiftMask;
            int dstIndex = 0;
            int srcIndex = 0;
            int dstBits = bitLength;
            
            while (dstBits > BitsPerByte)
            {
                int left = (byte)(span[srcIndex++] << startShift);
                int right = (byte)(span[srcIndex] >> (BitsPerByte - startShift));
                m_array[dstIndex++] = (byte)(left | right);
                dstBits -= BitsPerByte;
            }

            //we can fit remaining bits into dst bits
            if (dstBits <= BitsPerByte - startShift)
            {
                int mask = 256 - (1 << (BitsPerByte - dstBits));
                m_array[dstIndex] = (byte)((span[srcIndex] << startShift) & mask);
            }
            else
            {
                int mask = 256 - (1 << (BitsPerByte - dstBits));
                int left = span[srcIndex++] << startShift;
                int right = (span[srcIndex] >> (BitsPerByte - startShift)) & mask;
                
                m_array[dstIndex] = (byte)(left | right);
            }
        }

        public void ToBytes(Span<byte> buffer)
        {
            m_array.AsSpan().CopyTo(buffer);
        }

        public static int WriteBytesToBuffer(Span<byte> dstBuffer, int startBit, ReadOnlySpan<byte> srcBuffer, int bitLength)
        {
            var dstSpan = dstBuffer.Slice(startBit >> BitShiftPerByte,
                GetByteArrayLengthFromBitLength(startBit + bitLength) - (startBit >> BitShiftPerByte));

            int currentBit = startBit;
            int startShift = startBit & BitShiftMask;
            int bitLengthShift = bitLength & BitShiftMask;

            if (startShift == 0)
            {
                srcBuffer.Slice(0, dstSpan.Length).CopyTo(dstSpan);
                if (bitLengthShift != 0)
                {
                    dstSpan[^1] &= (byte)(256 - (1 << (BitsPerByte - bitLengthShift)));
                }
                return startBit + bitLength;
            }

            int dstIndex = 0;
            int srcIndex = 0;
            int dstBits = bitLength;

            //out bits can fit into last byte of source
            if (dstBits <= BitsPerByte - startShift)
            {
                int mask = 256 - (1 << (BitsPerByte - dstBits));
                int right = (srcBuffer[srcIndex] >> (BitsPerByte - startShift)) & mask;
                dstSpan[0] |= (byte)right;
                return startBit + bitLength;
            }

            //dstBits do not fit in the last byte
            //copy first bits
            dstSpan[dstIndex++] |= (byte)(srcBuffer[srcIndex] >> startShift);
            dstBits -= BitsPerByte - startShift;

            while (dstBits > BitsPerByte)
            {
                int left = srcBuffer[srcIndex++] << (BitsPerByte - startShift);
                int right = srcBuffer[srcIndex] >> startShift;
                dstSpan[dstIndex++] = (byte)(left | right);
                dstBits -= BitsPerByte;
            }

            //remaining bits are located in one byte
            if (dstBits <= bitLengthShift || bitLengthShift == 0)
            {
                int mask = 256 - (1 << (BitsPerByte - dstBits));
                dstSpan[dstIndex] = (byte)((srcBuffer[srcIndex] << ((bitLengthShift - dstBits) & BitShiftMask)) & mask);
            }
            else
            {
                int mask = 256 - (1 << (BitsPerByte - dstBits));
                int left = srcBuffer[srcIndex++] << (BitsPerByte - (dstBits - bitLengthShift));
                int right = (srcBuffer[srcIndex] >> (dstBits - bitLengthShift)) & mask;
                
                dstSpan[dstIndex] = (byte)(left | right);
            }

            return startBit + bitLength;
        }
        
        public static int WriteBytesToBufferOld(Span<byte> dstBuffer, int startBit, ulong value, int bitLength)
        {
            var dstSpan = dstBuffer.Slice(startBit >> BitShiftPerByte,
                GetByteArrayLengthFromBitLength(startBit + bitLength) - (startBit >> BitShiftPerByte));

            int currentBit = startBit;
            int startShift = startBit & BitShiftMask;
            int bitLengthShift = bitLength & BitShiftMask;

            Span<byte> srcBuffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(srcBuffer, value << (BitsPerInt64 - bitLength));
            return WriteBytesToBuffer(dstBuffer, startBit, srcBuffer, bitLength);
        }
        
        public static int WriteBytesToBuffer(Span<byte> dstBuffer, int startBit, ulong value, int bitLength)
        {
            var dstSpan = dstBuffer.Slice(startBit >> BitShiftPerByte,
                GetByteArrayLengthFromBitLength(startBit + bitLength) - (startBit >> BitShiftPerByte));

            int currentBit = startBit;
            int startShift = startBit & BitShiftMask;
            int bitLengthShift = bitLength & BitShiftMask;

            if (startShift == 0)
            {
                //56+bits. We can write the whole ulong
                if (bitLength > BitsPerInt64 - BitsPerByte)
                {
                    BinaryPrimitives.WriteUInt64BigEndian(dstSpan, value << (BitsPerInt64 - bitLength));
                }
                else
                {
                    int byteShift = bitLength - BitsPerByte;
                    int dstIndex = 0;
                    int length = bitLength;

                    while (length > BitsPerByte)
                    {
                        dstSpan[dstIndex++] = (byte)(value >> byteShift);
                        byteShift -= BitsPerByte;
                        length -= BitsPerByte;
                    }

                    dstSpan[dstIndex] = (byte)(value << (BitsPerByte - length));
                }
                
                return startBit + bitLength;
            }

            Span<byte> srcBuffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(srcBuffer, value << (BitsPerInt64 - bitLength));
            return WriteBytesToBuffer(dstBuffer, startBit, srcBuffer, bitLength);
        }
        
        public static int WriteBytesToBuffer(Span<byte> dstBuffer, int startBit, UInt128 value, int bitLength)
        {
            int bitShift;
            
            if (bitLength > BitsPerInt64)
            {
                bitShift = WriteBytesToBuffer(dstBuffer, startBit, value.S1, bitLength - BitsPerInt64);
                bitShift = WriteBytesToBuffer(dstBuffer, bitShift, value.S0, BitsPerInt64);
            }
            else
            {
                bitShift = WriteBytesToBuffer(dstBuffer, startBit, value.S0, bitLength);
            }

            return bitShift;
        }

        public static Bits2 operator +(Bits2 a, Bits2 b)
        {
            var result = new Bits2(a.m_length + b.m_length);
            WriteBytesToBuffer(result.m_array, 0, a.m_array, a.m_length);
            WriteBytesToBuffer(result.m_array, a.m_length, b.m_array, b.m_length);
            return result;
        }

        public static Bits2 FromUInt64Array(ulong[] values, int bitLength)
        {
            var result = new Bits2(bitLength);
            int bitShift = 0;
            int remainingBits = bitLength;

            foreach (var value in values)
            {
                bitShift = WriteBytesToBuffer(result.m_array, bitShift, value, Math.Min(remainingBits, BitsPerInt64));
                remainingBits -= BitsPerInt64;
            }

            return result;
        }

        public static Bits2 FromUInt128(UInt128 value, int bitLength)
        {
            var result = new Bits2(bitLength);
            if (bitLength > BitsPerInt64)
            {
                int bitShift = WriteBytesToBuffer(result.m_array, 0, value.S1, bitLength - BitsPerInt64);
                WriteBytesToBuffer(result.m_array, bitShift, value.S0, BitsPerInt64);
            }
            else
            {
                WriteBytesToBuffer(result.m_array, 0, value.S0, bitLength);
            }

            return result;
        }

        public int Length => m_length;
    }
}