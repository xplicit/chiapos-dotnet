using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Chiapos.Dotnet
{
    public class Chacha8
    {
        static readonly byte[] sigma = Encoding.ASCII.GetBytes("expand 32-byte k");
        static readonly byte[] tau = Encoding.ASCII.GetBytes("expand 16-byte k");
        
        uint[] input = new uint[16];

        public void KeySetup(ReadOnlySpan<byte> k, uint kbits, ReadOnlySpan<byte> iv = default)
        {
            ReadOnlySpan<byte> constants;

            input[4] = BinaryPrimitives.ReadUInt32LittleEndian(k);
            input[5] = BinaryPrimitives.ReadUInt32LittleEndian(k[4..]);
            input[6] = BinaryPrimitives.ReadUInt32LittleEndian(k[8..]);
            input[7] = BinaryPrimitives.ReadUInt32LittleEndian(k[12..]);
            if (kbits == 256) { /* recommended */
                k = k[16..];
                constants = sigma;
            } else { /* kbits == 128 */
                constants = tau;
            }
            input[8] = BinaryPrimitives.ReadUInt32LittleEndian(k);
            input[9] = BinaryPrimitives.ReadUInt32LittleEndian(k[4..]);
            input[10] = BinaryPrimitives.ReadUInt32LittleEndian(k[8..]);
            input[11] = BinaryPrimitives.ReadUInt32LittleEndian(k[12..]);
            input[0] = BinaryPrimitives.ReadUInt32LittleEndian(constants);
            input[1] = BinaryPrimitives.ReadUInt32LittleEndian(constants[4..]);
            input[2] = BinaryPrimitives.ReadUInt32LittleEndian(constants[8..]);
            input[3] = BinaryPrimitives.ReadUInt32LittleEndian(constants[12..]);
            if (iv != default) {
                input[14] = BinaryPrimitives.ReadUInt32LittleEndian(iv);
                input[15] = BinaryPrimitives.ReadUInt32LittleEndian(iv[4..]);
            } else {
                input[14] = 0;
                input[15] = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint PLUS(uint a, uint b) => unchecked(a + b);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint PLUSONE(uint a) => unchecked(a + 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void QUARTERROUND(ref uint a, ref uint b, ref uint c, ref uint d)
        {
            unchecked
            {
                a = a + b;
                d = BitOperations.RotateLeft(d ^ a, 16);
                c = c + d;
                b = BitOperations.RotateLeft(b ^ c, 12);
                a = a + b;
                d = BitOperations.RotateLeft(d ^ a, 8);
                c = c + d;
                b = BitOperations.RotateLeft(b ^ c, 7);
            }
        }

        public void GetKeystream(ulong pos, uint n_blocks, Span<byte> c)
        {
            uint x0, x1, x2, x3, x4, x5, x6, x7, x8, x9, x10, x11, x12, x13, x14, x15;
            uint j0, j1, j2, j3, j4, j5, j6, j7, j8, j9, j10, j11, j12, j13, j14, j15;
            int i;

            j0 = input[0];
            j1 = input[1];
            j2 = input[2];
            j3 = input[3];
            j4 = input[4];
            j5 = input[5];
            j6 = input[6];
            j7 = input[7];
            j8 = input[8];
            j9 = input[9];
            j10 = input[10];
            j11 = input[11];
            j12 = (uint) pos;
            j13 = (uint) (pos >> 32);
            j14 = input[14];
            j15 = input[15];

            while (n_blocks-- > 0)
            {
                x0 = j0;
                x1 = j1;
                x2 = j2;
                x3 = j3;
                x4 = j4;
                x5 = j5;
                x6 = j6;
                x7 = j7;
                x8 = j8;
                x9 = j9;
                x10 = j10;
                x11 = j11;
                x12 = j12;
                x13 = j13;
                x14 = j14;
                x15 = j15;
                for (i = 8; i > 0; i -= 2)
                {
                    QUARTERROUND(ref x0, ref x4, ref x8, ref x12);
                    QUARTERROUND(ref x1, ref x5, ref x9, ref x13);
                    QUARTERROUND(ref x2, ref x6, ref x10, ref x14);
                    QUARTERROUND(ref x3, ref x7, ref x11, ref x15);
                    QUARTERROUND(ref x0, ref x5, ref x10, ref x15);
                    QUARTERROUND(ref x1, ref x6, ref x11, ref x12);
                    QUARTERROUND(ref x2, ref x7, ref x8, ref x13);
                    QUARTERROUND(ref x3, ref x4, ref x9, ref x14);
                }

                x0 = PLUS(x0, j0);
                x1 = PLUS(x1, j1);
                x2 = PLUS(x2, j2);
                x3 = PLUS(x3, j3);
                x4 = PLUS(x4, j4);
                x5 = PLUS(x5, j5);
                x6 = PLUS(x6, j6);
                x7 = PLUS(x7, j7);
                x8 = PLUS(x8, j8);
                x9 = PLUS(x9, j9);
                x10 = PLUS(x10, j10);
                x11 = PLUS(x11, j11);
                x12 = PLUS(x12, j12);
                x13 = PLUS(x13, j13);
                x14 = PLUS(x14, j14);
                x15 = PLUS(x15, j15);

                j12 = PLUSONE(j12);
                if (j12 == 0)
                {
                    j13 = PLUSONE(j13);
                    /* stopping at 2^70 bytes per nonce is user's responsibility */
                }

                BinaryPrimitives.WriteUInt32LittleEndian(c, x0);
                BinaryPrimitives.WriteUInt32LittleEndian(c[4..], x1);
                BinaryPrimitives.WriteUInt32LittleEndian(c[8..], x2);
                BinaryPrimitives.WriteUInt32LittleEndian(c[12..], x3);
                BinaryPrimitives.WriteUInt32LittleEndian(c[16..], x4);
                BinaryPrimitives.WriteUInt32LittleEndian(c[20..], x5);
                BinaryPrimitives.WriteUInt32LittleEndian(c[24..], x6);
                BinaryPrimitives.WriteUInt32LittleEndian(c[28..], x7);
                BinaryPrimitives.WriteUInt32LittleEndian(c[32..], x8);
                BinaryPrimitives.WriteUInt32LittleEndian(c[36..], x9);
                BinaryPrimitives.WriteUInt32LittleEndian(c[40..], x10);
                BinaryPrimitives.WriteUInt32LittleEndian(c[44..], x11);
                BinaryPrimitives.WriteUInt32LittleEndian(c[48..], x12);
                BinaryPrimitives.WriteUInt32LittleEndian(c[52..], x13);
                BinaryPrimitives.WriteUInt32LittleEndian(c[56..], x14);
                BinaryPrimitives.WriteUInt32LittleEndian(c[60..], x15);

                c = c[64..];
            }
        }
    }
}