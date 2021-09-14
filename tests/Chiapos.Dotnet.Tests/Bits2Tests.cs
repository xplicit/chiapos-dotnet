using System;
using System.Linq;
using Dirichlet.Numerics;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    [TestFixture]
    public class Bits2Tests
    {
        [Test]
        public void CanConstructFromBytes_WithSlice_StartBitShiftLessThanEndBitShift()
        {
            byte[] buffer = { 0x01, 0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x00, 0x00, 0x00, 0x00 };
            byte[] expected = { 0xAC, 0xCF, 0x13, 0x57, 0x80 };
            byte[] aaa = new byte[16];

            var x = Util.SliceInt64FromBytes(buffer, 25, 35);
            Bits g = new Bits(x, 35);
            g.ToBytes(aaa);

            Bits2 g2 = new Bits2(buffer, 25, 35);
            AssertBitsArray(g2, expected, 35);
        }
        
        [Test]
        public void CanConstructFromBytes_WithSlice_StartBitShiftGreaterThanEndBitShift()
        {
            byte[] buffer = { 0x01, 0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x00, 0x00, 0x00, 0x00 };
            byte[] expected = { 0x15, 0x99, 0xE2, 0x6A, 0xE0 };

            Bits2 g2 = new Bits2(buffer, 22, 35);
            AssertBitsArray(g2, expected, 35);
        }
        
        [Test]
        public void CanConstructFromBytes_WithSlice_Returns24Bit()
        {
            byte[] buffer = { 0x01, 0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x00, 0x00, 0x00, 0x00 };
            byte[] expected = { 0x15, 0x99, 0xE2 };

            Bits2 g2 = new Bits2(buffer, 22, 24);
            AssertBitsArray(g2, expected, 24);
        }

        [Test]
        public void CanConstructFromBytes_WithSlice_StartIsRoundToZeroBits()
        {
            byte[] buffer = { 0x01, 0x12, 0x34, 0x56, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x00, 0x00, 0x00, 0x00 };
            byte[] expected = { 0x56, 0x67, 0x89, 0x80 };

            Bits2 g2 = new Bits2(buffer, 24, 26);
            AssertBitsArray(g2, expected, 26);
        }
        
        [Test]
        public void CanWriteBytes_WithSlice_LengthShiftIsFitInLastByte()
        {
            byte[] actual = new byte[8];
            byte[] buffer = { 0xB5, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x00, 0x00, 0x00, 0x00 };
            byte[] expected = { 0x00, 0x00, 0x02, 0xD5, 0x9E, 0x26, 0xAF, 0x20 };

            Bits2 g2 = new Bits2(buffer, 2, 35);

            Bits2.WriteBytesToBuffer(actual, 22, buffer, 37);
            Assert.That(actual.SequenceEqual(expected));
        }

        [Test]
        public void CanWriteBytes_WithSlice_LengthShiftIsNotFitInLastByte()
        {
            byte[] actual = new byte[9];
            byte[] buffer = { 0xB5, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x00, 0x00, 0x00, 0x00 };
            byte[] expected = { 0x00, 0x00, 0x00, 0x2D, 0x59, 0xE2, 0x6A, 0xF3, 0x60 };

            Bits2 g2 = new Bits2(buffer, 6, 35);

            Bits2.WriteBytesToBuffer(actual, 26, buffer, 41);
            Assert.That(actual.SequenceEqual(expected));
        }

        [Test]
        public void CanWriteBytes_WithSlice_StartShiftIsRoundToByte()
        {
            byte[] actual = new byte[7];
            byte[] buffer = { 0xB5, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x00, 0x00, 0x00, 0x00 };
            byte[] expected = { 0x00, 0x00, 0xB5, 0x67, 0x89, 0xAB, 0xC0 };

            Bits2.WriteBytesToBuffer(actual, 16, buffer, 36);
            Assert.That(actual.SequenceEqual(expected));
        }
                
        [Test]
        public void Slice_and_Manipulating()
        {
            var buffer = new byte[32];
            Bits g = new Bits(0b_0110011_11010111, 15);
            g.ToBytes(buffer);
            
            var x = new Bits2(buffer, 4, 9 - 4);
            Assert.That(x.GetValue(), Is.EqualTo(0b_01111));
            
            x = new Bits2(buffer, 9, 15 - 9);
            Assert.That(x.GetValue(), Is.EqualTo(0b_010111));
            x = new Bits2(buffer, 0, 9);
            Assert.That(x.GetValue(), Is.EqualTo(0b_011001111));
            x = new Bits2(buffer, 9, 10 - 9);
            Assert.That(x.GetValue(), Is.EqualTo(0));
            //TODO: add support for empty slices
            //x = g.Slice(9, 9);

            Bits g1 = new Bits(0x01020408_10204080, 64);
            g1.ToBytes(buffer);
            var x1 = new Bits2(buffer, 8, 56 - 8);
            Assert.That(x1.GetValue(), Is.EqualTo(0x020408_102040));
        }

        [Test]
        public void Slice_CanSlice128bitArray()
        {
            var x = new byte[32];
            
            Bits g2 = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 128);
            g2.ToBytes(x);

            Bits2 actual = new Bits2(x, 8, 120 - 8);

            var expected = new byte[] { 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41 };
            AssertBitsArray(actual, expected, 120 - 8);
        }

        [Test]
        public void Slice_CanSliceArraysFitIn64Bit()
        {
            var buffer = new byte[]
            {
                0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,
                0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                0x82, 0x80
            };
            
            var x = new Bits2(buffer,   32, 56);
            var expected = new byte[] { 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14 };
            AssertBitsArray(x, expected, 56);

            x = new Bits2(buffer, 80, 56);
            expected = new byte[] { 0x14, 0x18, 0x11, 0x21, 0x41, 0x81, 0x82 };
            AssertBitsArray(x, expected, 56);
            
            x = new Bits2(buffer, 128, 8);
            expected = new byte[] { 0x82 };
            AssertBitsArray(x, expected, 8);
            
            x = new Bits2(buffer , 132, 4);
            expected = new byte[] { 0x20 };
            AssertBitsArray(x, expected, 4);
        }

        [Test]
        public void Slice_CanSliceArraysFitIn128Bit()
        {
            var buffer = new byte[]
                {
                    0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,
                    0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                    0x82, 0x80
                };
            
            var x3 = new Bits2(buffer, 16, 128 + 8 - 16);
            var expected = new byte[]
            {
                0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12,
                0x14, 0x18, 0x11, 0x21, 0x41, 0x81, 0x82
            };
            AssertBitsArray(x3, expected, 128 + 8 - 16);
        }
        
        [Test]
        public void Slice_CanSliceArraysNotFitIn64bit()
        {
            //verified in chiapos
            var buffer = new byte[]
                {
                    0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,
                    0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                    0x82, 0x80
                };
            var x3 = new Bits2(buffer, 4, 128 + 8 - 4);
            var expected = new byte[]
            {
                0x10, 0x20, 0x40, 0x81, 0x02, 0x04, 0x08, 0x01,
                0x11, 0x21, 0x41, 0x81, 0x12, 0x14, 0x18, 0x18,
                0x20
            };
            AssertBitsArray(x3, expected, 128 + 8 - 4);
        }
        
        [Test]
        public void Slice_CanSliceLargeBitsIntoMoreThan64BitValue()
        {
            var x = ((UInt128)0x1E0EEF63E7 << 64) + (UInt128)0x1336D7EB2558F7BA;
            var bits = new Bits(x, 104);
            var buffer = new byte[32];
            bits.ToBytes(buffer);
            
            var actual = new Bits2(buffer, 6, 102 - 6);

            var expected = new byte[] { 0x83, 0xBB, 0xD8, 0xF9, 0xC4, 0xCD, 0xB5, 0xFA, 0xC9, 0x56, 0x3D, 0xEE };
            AssertBitsArray(actual, expected, 102 - 6);
        }
        
        [Test]
        public void GetValue_ShouldReturnConstructorValue()
        {
            ulong expected = 0x6322_3df1_f7ec_dcbe;
            Bits2 x = Bits2.FromUInt128(new UInt128(expected),64);
            var actual = x.GetValue();
            Assert.That(actual, Is.EqualTo(expected));

            var v = 0x01020408;
            x = Bits2.FromUInt128(new UInt128(v), 32);
            actual = x.GetValue();
            
            Assert.That(actual, Is.EqualTo(0x01020408));
        }

        private void AssertBitsArray(Bits2 actual, byte[] expectedArray, int expectedLength)
        {
            Assert.That(actual.Length, Is.EqualTo(expectedLength), "Length differs");

            var actualArray = new byte[Util.Cdiv(expectedLength, 8)];
            actual.ToBytes(actualArray);
            
            for (int i = 0; i < expectedArray.Length; i++)
            {
                Assert.That(actualArray[i], Is.EqualTo(expectedArray[i]), $"index={i}: actual byte = {actualArray[i]:X}, expected byte = {expectedArray[i]:X}");
            }
        }
        
        [Test]
        public void OperatorPlus_BothArraysHaveNoRemainingBits()
        {
            var bitsA = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55};
            var bitsB = new byte[] {0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};

            var a = new Bits2(new ReadOnlySpan<byte>(bitsA, 0, 4), 0, 32);
            var b = new Bits2(new ReadOnlySpan<byte>(bitsB, 0, 4), 0, 32);

            var c = a + b;
            var expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0xAA, 0xAA, 0xAA, 0xAA};
            AssertBitsArray(c, expected, 64);

            a = new Bits2(bitsA, 0, 64);
            b = new Bits2(new ReadOnlySpan<byte>(bitsB, 0, 4), 0, 32);

            c = a + b;
            expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xAA, 0xAA, 0xAA, 0xAA};
            AssertBitsArray(c, expected, 96);
            
            a = new Bits2(bitsA, 0, 64);
            b = new Bits2(bitsB, 0, 64);

            c = a + b;
            expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};
            AssertBitsArray(c, expected, 128);
        }

        [Test]
        public void OperatorPlus_FirstArrayHaveFiveRemainingBits()
        {
            //verified in chiapos
            var bitsA = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55};
            var bitsB = new byte[] {0xAF, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};
            
            var a = new Bits2(new ReadOnlySpan<byte>(bitsA, 0, 5), 0, 32 + 5);
            var b = new Bits2(bitsB, 0, 64);

            var c = a + b;
            var expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x7D, 0x55, 0x55,  0x55, 0x55, 0x55, 0x55, 0x50};
            AssertBitsArray(c, expected, 64 + 32 + 5);
        }

        [Test]
        public void OperatorPlus_SumOfArraysHaveNoRemainingBits()
        {
            //verified in chiapos
            var bitsA = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55};
            var bitsB = new byte[] {0x07, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};

            var a = new Bits2(new ReadOnlySpan<byte>(bitsA, 0, 5), 0, 32 + 5);
            var b = new Bits2(bitsB,0, 64 - 5);
            
            var c = a + b;
            var expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0x50, 0x3D, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55};
            AssertBitsArray(c, expected, 64 + 32);
        }

        [Test]
        public void OperatorPlus_CanSumBitsFromF1Calculator()
        {
            var a = new Bits2(0x373b3dfc4, 35);
            var b = new Bits2(0, 6);
            
            var actual = a + b;
            Assert.That(actual.GetValue(), Is.EqualTo(0xdcecf7f100));
        }

        [Test]
        public void OperatorPlus_CanSumLargeBits()
        {
            var y1 = new Bits2(130, 31);
            var L = Bits2.FromUInt128(((UInt128)0x4CAAF91F5 << 64) + (UInt128)0xE752E38_9DC9A0A72, 64 + 36);
            var R = Bits2.FromUInt128(((UInt128)0xEF61AFE2E << 64) + (UInt128)0x9C198FE_A7C700C76, 64 + 36);

            var actual = y1 + L + R;
            var expected = new byte[] { 
                0x00, 0x00, 0x01, 0x04, 0x99, 0x55, 0xF2, 0x3E,
                0xBC, 0xEA, 0x5C, 0x71, 0x3B, 0x93, 0x41, 0x4E,
                0x5D, 0xEC, 0x35, 0xFC, 0x5D, 0x38, 0x33, 0x1f,
                0xD4, 0xF8, 0xE0, 0x18, 0xEC
            };
            
            AssertBitsArray(actual, expected, 31 + 100 + 100);
        }
        
        [Test]
        public void OperatorPlus_CanSumLargeBitsOfLength75()
        {
            var y1 = new Bits2(24, 31);
            var L = Bits2.FromUInt64Array(new ulong[]{0x59540217762ED3F8, 0x510}, 75);
            var R = Bits2.FromUInt64Array(new ulong[]{0xE808DD3011BC859A, 0x70C}, 75);

            var actual = y1 + L + R;
            var expected = new byte[] { 
                0x00, 0x00, 0x00, 0x30, 0xB2, 0xA8, 0x04, 0x2E,
                0xEC, 0x5D, 0xA7, 0xF1, 0x44, 0x3A, 0x02, 0x37,
                0x4C, 0x04, 0x6F, 0x21, 0x66, 0xB8, 0x60
            };
            
            AssertBitsArray(actual, expected, 31 + 75 + 75);
        }

        [Test]
        public void WriteBytes_UInt64_Slice6_32BitValue()
        {
            var actual = new byte [5];
            actual[0] = 0b_1111_0100;
            byte[] expected = { 0xF7, 0x2A, 0xAA, 0xAA, 0xA8 };

            int bits = Bits2.WriteBytesToBuffer(actual, 6, 0x00000000_CAAAAAAA, 32);
            Assert.That(bits, Is.EqualTo(32 + 6));
            Assert.That(actual.SequenceEqual(expected));
        }
        
        [Test]
        public void WriteBytes_UInt64_Slice0_38BitValue()
        {
            var actual = new byte [5];
            byte[] expected = { 0xFF, 0x2A, 0xAA, 0xAA, 0xA8 };

            int bits = Bits2.WriteBytesToBuffer(actual, 0, 0x0000003F_CAAAAAAA, 38);
            Assert.That(bits, Is.EqualTo(38));
            Assert.That(actual.SequenceEqual(expected));
        }
    }
}