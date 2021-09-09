using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        public Bits2(ReadOnlySpan<byte> buffer, int startBit, int bitLength)
        {
            m_array = new byte[GetByteArrayLengthFromBitLength(bitLength)];
            m_length = bitLength;

            var span = buffer.Slice(startBit >> BitShiftPerByte,
                 ((startBit + bitLength) >> BitShiftPerByte) - (startBit >> BitShiftPerByte) + 1);
            
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
                ((startBit + bitLength) >> BitShiftPerByte) - (startBit >> BitShiftPerByte) + 1);

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
            if (dstBits <= bitLengthShift)
            {
                int mask = 256 - (1 << (BitsPerByte - dstBits));
                dstSpan[dstIndex] = (byte)((srcBuffer[srcIndex] << (bitLengthShift - dstBits)) & mask);
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
        
        public static int WriteBytesToBuffer(Span<byte> dstBuffer, int startBit, ulong value, int bitLength)
        {
            var dstSpan = dstBuffer.Slice(startBit >> BitShiftPerByte,
                ((startBit + bitLength) >> BitShiftPerByte) - (startBit >> BitShiftPerByte) + 1);

            int currentBit = startBit;
            int startShift = startBit & BitShiftMask;
            int bitLengthShift = bitLength & BitShiftMask;

            Span<byte> srcBuffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(srcBuffer, value << (BitsPerInt64 - bitLength));
            return WriteBytesToBuffer(dstBuffer, startBit, srcBuffer, bitLength);
        }

        public int Length => m_length;
    }
}