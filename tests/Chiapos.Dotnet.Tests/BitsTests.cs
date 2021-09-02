using System;
using System.Collections;
using Dirichlet.Numerics;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    [TestFixture]
    public class BitsTests
    {
        [Test]
        public void Slice_and_Manipulating()
        {
            
            Bits g = new Bits(0b_0110011_11010111, 15);
            var x = g.Slice(4, 9);
            Assert.That(x.GetValue(), Is.EqualTo(0b_01111));
            
            x = g.Slice(9, 15);
            Assert.That(x.GetValue(), Is.EqualTo(0b_010111));
            x = g.Slice(0, 9);
            Assert.That(x.GetValue(), Is.EqualTo(0b_011001111));
            x = g.Slice(9, 10);
            Assert.That(x.GetValue(), Is.EqualTo(0));
            //TODO: add support for empty slices
            //x = g.Slice(9, 9);

            Bits g1 = new Bits(0x01020408_10204080, 64);
            var x1 = g1.Slice(8, 56);
            Assert.That(x1.GetValue(), Is.EqualTo(0x020408_102040));
        }

        [Test]
        public void Slice_CanSlice128bitArray()
        {
            Bits g2 = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 128);
            var x2 = g2.Slice(8, 120);

            var expected = new byte[] { 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41 };
            AssertBitsArray(x2, expected, 120 - 8);
        }

        [Test]
        public void Slice_CanSliceArraysFitIn64Bit()
        {
            Bits g = new Bits(
                new byte[]
                {
                    0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,
                    0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                    0x82, 0x80
                }, 128 + 12);
            var x = g.Slice(32, 32 + 56);
            var expected = new byte[] { 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14 };
            AssertBitsArray(x, expected, 56);

            x = g.Slice(80, 80 + 56);
            expected = new byte[] { 0x14, 0x18, 0x11, 0x21, 0x41, 0x81, 0x82 };
            AssertBitsArray(x, expected, 56);
            
            x = g.Slice(128, 128 + 8);
            expected = new byte[] { 0x82 };
            AssertBitsArray(x, expected, 8);
            
            x = g.Slice(132, 132 + 4);
            expected = new byte[] { 0x20 };
            AssertBitsArray(x, expected, 4);
        }

        [Test]
        public void Slice_CanSliceArraysFitIn128Bit()
        {
            Bits g3 = new Bits(
                new byte[]
                {
                    0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,
                    0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                    0x82, 0x80
                }, 128 + 12);
            var x3 = g3.Slice(16, 128 + 8);
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
            Bits g3 = new Bits(
                new byte[]
                {
                    0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80,
                    0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                    0x82, 0x80
                }, 128 + 12);
            var x3 = g3.Slice(4, 128 + 8);
            var expected = new byte[]
            {
                0x10, 0x20, 0x40, 0x81, 0x02, 0x04, 0x08, 0x01,
                0x11, 0x21, 0x41, 0x81, 0x12, 0x14, 0x18, 0x18,
                0x20
            };
            AssertBitsArray(x3, expected, 128 + 4);
        }
        
        [Test]
        public void Slice_CanSliceLargeBitsIntoMoreThan64BitValue()
        {
            var x = ((UInt128)0x1E0EEF63E7 << 64) + (UInt128)0x1336D7EB2558F7BA;
            var bits = new Bits(x, 104);
            var actual = bits.Slice(6, 102);

            var expected = new byte[] { 0x83, 0xBB, 0xD8, 0xF9, 0xC4, 0xCD, 0xB5, 0xFA, 0xC9, 0x56, 0x3D, 0xEE };
            AssertBitsArray(actual, expected, 102 - 6);
        }
        
        /*
            SECTION("Slicing and manipulating")
    {
        Bits g = Bits(13271, 15);
        cout << "G: " << g << endl;
        cout << "G Slice: " << g.Slice(4, 9) << endl;
        cout << "G Slice: " << g.Slice(0, 9) << endl;
        cout << "G Slice: " << g.Slice(9, 10) << endl;
        cout << "G Slice: " << g.Slice(9, 15) << endl;
        cout << "G Slice: " << g.Slice(9, 9) << endl;
        REQUIRE(g.Slice(9, 9) == Bits());

        uint8_t bytes[2];
        g.ToBytes(bytes);
        cout << "bytes: " << static_cast<int>(bytes[0]) << " " << static_cast<int>(bytes[1])
             << endl;
        cout << "Back to Bits: " << Bits(bytes, 2, 16) << endl;

        Bits(256, 9).ToBytes(bytes);
        cout << "bytes: " << static_cast<int>(bytes[0]) << " " << static_cast<int>(bytes[1])
             << endl;
        cout << "Back to Bits: " << Bits(bytes, 2, 16) << endl;

        cout << Bits(640, 11) << endl;
        Bits(640, 11).ToBytes(bytes);
        cout << "bytes: " << static_cast<int>(bytes[0]) << " " << static_cast<int>(bytes[1])
             << endl;

        Bits h = Bits(bytes, 2, 16);
        Bits i = Bits(bytes, 2, 17);
        cout << "H: " << h << endl;
        cout << "I: " << i << endl;

        cout << "G: " << g << endl;
        cout << "size: " << g.GetSize() << endl;

        Bits shifted = (g << 150);

        REQUIRE(shifted.GetSize() == 15);
        REQUIRE(shifted.ToString() == "000000000000000");

        Bits large = Bits(13271, 200);
        REQUIRE(large == ((large << 160)) >> 160);
        REQUIRE((large << 160).GetSize() == 200);

        Bits l = Bits(123287490 & ((1U << 20) - 1), 20);
        l = l + Bits(0, 5);

        Bits m = Bits(5, 3);
        uint8_t buf[1];
        m.ToBytes(buf);
        REQUIRE(buf[0] == (5 << 5));
    }
    SECTION("Park Bits")
    {
        uint32_t const num_bytes = 16000;
        uint8_t buf[num_bytes];
        uint8_t buf_2[num_bytes];
        Util::GetRandomBytes(buf, num_bytes);
        ParkBits my_bits = ParkBits(buf, num_bytes, num_bytes * 8);
        my_bits.ToBytes(buf_2);
        for (uint32_t i = 0; i < num_bytes; i++) {
            REQUIRE(buf[i] == buf_2[i]);
        }
    }

    SECTION("Large Bits")
    {
        uint32_t const num_bytes = 200000;
        uint8_t buf[num_bytes];
        uint8_t buf_2[num_bytes];
        Util::GetRandomBytes(buf, num_bytes);
        LargeBits my_bits = LargeBits(buf, num_bytes, num_bytes * 8);
        my_bits.ToBytes(buf_2);
        for (uint32_t i = 0; i < num_bytes; i++) {
            REQUIRE(buf[i] == buf_2[i]);
        }
    }
}
*/
        [Test]
        public void Uint128Constructor_CanConvertToBytes()
        {
            Bits g = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 128);

            var expected = new byte[] { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81 };
            AssertBitsArray(g, expected, 128);
            
            g = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 120);

            expected = new byte[] { 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81 };
            AssertBitsArray(g, expected, 120);

            g.AppendValue((UInt128)0x22232425_26272829, 64);
            
            expected = new byte[]
            {
                0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29
            };
            AssertBitsArray(g, expected, 120 + 64);
        }
        
        [Test]
        public void Uint128Constructor_ThrowWhenNegativeLength()
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException),
                () => new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, -1));
        }
        
        [Test]
        public void ExptyConstructor_ThrowWhenNegativeLength()
        {
            Assert.Throws(typeof(ArgumentOutOfRangeException),
                () => new Bits(-1));
        }

        [Test]
        public void CanAppendValue()
        {
            Bits g;
            byte[] expected; 
            
            //verified in chiapos
            g = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 120);
            g.AppendValue((UInt128)0x22232425_26272829, 64);
            
            expected = new byte[]
            {
                0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                0X22, 0X23, 0X24, 0X25, 0X26, 0X27, 0X28, 0X29
            };
            AssertBitsArray(g, expected, 120 + 64);
            
            //verified in chiapos
            g = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 120);
            g.AppendValue((UInt128)0x22232425_2627280F, 8);
            
            expected = new byte[]
            {
                0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81, 0x0F
            };
            AssertBitsArray(g, expected, 120 + 8);
            
            //Verified in chiapos
            g = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 120);
            g.AppendValue((UInt128)0x22232425_2627282F, 4);
            
            expected = new byte[]
            {
                0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81, 0xF0
            };
            AssertBitsArray(g, expected, 120 + 4);
            
            //verified in chiapos
            g = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 120);
            g.AppendValue(((UInt128)0x01 << 64) + (UInt128)0x22232425_26272829, 68);
            
            expected = new byte[]
            {
                0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                0x12, 0x22, 0x32, 0x42, 0x52, 0x62, 0x72, 0x82, 0x90
            };
            AssertBitsArray(g, expected, 120 + 68);
            
            g = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 120);
            g.AppendValue(((UInt128)0x717273 << 64) + (UInt128)0x22232425_26272829, 88);
            
            expected = new byte[]
            {
                0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                0x71, 0x72, 0x73, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29
            };
            AssertBitsArray(g, expected, 120 + 88);
            
            //Verified in chiapos
            g = new Bits((((UInt128)0x01020408_10204080) << 64) + (UInt128)0x11121418_11214181, 128);
            g.AppendValue(((UInt128)0x22232425_26272829 << 64) + (UInt128)0x51525354_55565758, 72);
            
            expected = new byte[]
            {
                0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x11, 0x12, 0x14, 0x18, 0x11, 0x21, 0x41, 0x81,
                0x29, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58
            };
            AssertBitsArray(g, expected, 128 + 72);
        }

        [Test]
        public void AppendValue_CanAppendToEmptyBits()
        {
            var g = new Bits(0);
            g.AppendValue(0x01020304_05060708, 32);
            var actual = g.GetValue(); 

            Assert.That(actual, Is.EqualTo(0x05060708));
        }

        [Test]
        public void AppendValue_CanAppend32Bit_WhenDoesNotFitIn64Bit()
        {
            var g = new Bits(0xCAEA3F3, 38);
            g.AppendValue(0xC0, 32);

            var expected = new byte[] { 0x00, 0x32, 0xBA, 0x8F, 0xCC, 0x00, 0x00, 0x03, 0x00 };
            AssertBitsArray(g, expected, 38 + 32);
        }
        
        [Test]
        public void GetValue_ShouldReturnConstructorValue()
        {
            ulong expected = 0x6322_3df1_f7ec_dcbe;
            Bits x = new Bits(new UInt128(expected),64);
            var actual = x.GetValue();

            var v = 0x01020408;
            x = new Bits(new UInt128(v), 32);
            actual = x.GetValue();
            
            Assert.That(actual, Is.EqualTo(0x01020408));
        }
        
        [Test]
        public void GetValue_FromLargeArray_ShouldThrow()
        {
            Bits x = new Bits((new UInt128(0x6322_3df1_f7ec_dcbe) << 64) + new UInt128(0xffff),68);
            Assert.Throws(typeof(InvalidOperationException), () => x.GetValue());
        }
        
        [Test]
        public void OperatorPlus_BothArraysHaveNoRemainingBits()
        {
            var bitsA = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55};
            var bitsB = new byte[] {0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};

            Bits a = new Bits(new ReadOnlySpan<byte>(bitsA, 0, 4), 32);
            Bits b = new Bits(new ReadOnlySpan<byte>(bitsB, 0, 4), 32);

            var c = a + b;
            var expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0xAA, 0xAA, 0xAA, 0xAA};
            AssertBitsArray(c, expected, 64);

            a = new Bits(bitsA, 64);
            b = new Bits(new ReadOnlySpan<byte>(bitsB, 0, 4), 32);

            c = a + b;
            expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0xAA, 0xAA, 0xAA, 0xAA};
            AssertBitsArray(c, expected, 96);
            
            a = new Bits(bitsA, 64);
            b = new Bits(bitsB, 64);

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
            
            Bits a = new Bits(new ReadOnlySpan<byte>(bitsA, 0, 5), 32 + 5);
            Bits b = new Bits(bitsB, 64);

            var c = a + b;
            var expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x7D, 0x55, 0x55,  0x55, 0x55, 0x55, 0x55, 0x50};
            AssertBitsArray(c, expected, 64 + 32 + 5);
        }

        [Test]
        public void OperatorPlus_SumOfArraysHaveNoRemainingBits()
        {
            var bitsA = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55};
            var bitsB = new byte[] {0x07, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};

            Bits a = new Bits(new ReadOnlySpan<byte>(bitsA, 0, 5), 32 + 5);
            Bits b = new Bits(bitsB,64 - 5);
            
            var c = a + b;
            var expected = new byte[] {0x55, 0x55, 0x55, 0x55, 0x57, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};
            AssertBitsArray(c, expected, 64 + 32);
        }

        [Test]
        public void OperatorPlus_CanSumBitsFromF1Calculator()
        {
            var a = new Bits(0x373b3dfc4, 35);
            var b = new Bits(0, 6);
            
            var actual = a + b;
            Assert.That(actual.GetValue(), Is.EqualTo(0xdcecf7f100));
        }

        [Test]
        public void OperatorPlus_CanSumLargeBits()
        {
            var y1 = new Bits(130, 31);
            var L = new Bits(((UInt128)0x4CAAF91F5 << 64) + (UInt128)0xE752E38_9DC9A0A72, 64 + 36);
            var R = new Bits(((UInt128)0xEF61AFE2E << 64) + (UInt128)0x9C198FE_A7C700C76, 64 + 36);

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
            var y1 = new Bits(24, 31);
            var L = new Bits(new ulong[]{0x59540217762ED3F8, 0x510}, 75);
            var R = new Bits(new ulong[]{0xE808DD3011BC859A, 0x70C}, 75);

            var actual = y1 + L + R;
            var expected = new byte[] { 
                0x00, 0x00, 0x00, 0x30, 0xB2, 0xA8, 0x04, 0x2E,
                0xEC, 0x5D, 0xA7, 0xF1, 0x44, 0x3A, 0x02, 0x37,
                0x4C, 0x04, 0x6F, 0x21, 0x66, 0xB8, 0x60
            };
            
            AssertBitsArray(actual, expected, 31 + 75 + 75);
        }

        [Test]
        public void Slice_FromStartToEnd()
        {
            var bits = new byte[] { 0b_00100101, 0b_01000010, 0b_01010000, 0b_01010101, 0b_01010101 };

            Bits x = new Bits(bits, 40);

            
            var actual = x.Slice(16);
            var expected = new byte[] { 0b_01010000, 0b_01010101, 0b_01010101 };
            AssertBitsArray(actual, expected, 24);

            actual = x.Slice(24);
            expected = new byte[] { 0b_01010101, 0b_01010101 };
            AssertBitsArray(actual, expected, 16);
            
            //verified in chiapos
            actual = x.Slice(14);
            expected = new byte[] { 0b_10010100, 0b_00010101, 0b_01010101, 0b_01000000 };
            AssertBitsArray(actual, expected, 26);
        }

        [Test]
        public void Slice_InsideInt()
        {
            var bits = new byte[] { 0b_00100101, 0b_01000010, 0b_01010000, 0b_01010101, 0b_01010101 };
            Bits x = new Bits(bits, 40);
           // AssertBitsArray(x, bits, 40);

           //verified in chiapos
            var actual = x.Slice(14, 14 + 9);
            var expected = new byte[] { 0b_10_010100, 0b_0_0000000}; 
            AssertBitsArray(actual, expected, 9);

            x = new Bits(bits, 40);
            actual = x.Slice(30, 30 + 9);
            expected = new byte[] { 0b_01_010101, 0b_0_0000000 };
            AssertBitsArray(actual, expected, 9);
            
            //verified in chiapos
            x = new Bits(new byte[] {0x84, 0xA0, 0xAA, 0xAA}, 31);
            actual = x.Slice(14, 14 + 9);
            expected = new byte[] { 0b_0_0101010, 0b_1_0000000};
            AssertBitsArray(actual, expected, 9);

            //verified in chiapos
            x = new Bits(525, 35);
            actual = x.Slice(0, 6);
            Assert.That(actual.GetValue(), Is.EqualTo(0));
        }

        [Test]
        public void Slice_StartsFromIntZeroBit()
        {
            var bits = new byte[] { 0b_00100101, 0b_01000010, 0b_01010000, 0b_01010101, 0b_01010101 };
            Bits x = new Bits(bits, 40);

            var actual = x.Slice(32);
            var expected = new byte[] { 0b_01010101 };
            AssertBitsArray(actual, expected, 8);

            //verified in chiapos
            actual = x.Slice(32, 35);
            expected = new byte[] { 0b_01000000 };
            AssertBitsArray(actual, expected, 3);
        }

        [Test]
        public void Slice_CanSliceLargeBits()
        {
            var bits = new byte[]
            {
                0x4f, 0x96, 0xa0, 0x56, 0xad, 0x12, 0xe0, 0xeb,
                0xc4, 0xbc, 0x0a, 0x9f, 0xa6, 0x8b, 0x9d, 0xa0,
                0xb2, 0x8c, 0x4f, 0x70, 0x97, 0x86, 0x23, 0xb0,
                0xc2, 0xd4, 0xf3, 0xe9, 0x0a, 0xc8, 0xc2, 0x0f,
                0xbe, 0x6f, 0x0c, 0x55, 0x20, 0x0f, 0xa7, 0x14,
                0xef, 0xa9, 0x26, 0xa5, 0xa9, 0x1f, 0x20, 0xa8,
                0x25, 0x74, 0x4f, 0x60, 0xe1, 0x71, 0x3c, 0x80,
                0xbe, 0xdc, 0xec, 0xf7, 0xf1, 0x3d, 0x22, 0x63
            };
            
            Bits g = new Bits(bits, 512);
            var x = g.Slice(455, 455 + 35);
            Assert.That(x.GetValue(), Is.EqualTo(0x373b3dfc4));
        }

        [Test]
        public void Slice_CanSlice100bitsInside112bits()
        {
            Bits g = new Bits(((UInt128)0xD1DAD3A8_94B7 << 64) + (UInt128)0x071ACB52A43A061E, 112);

            var actual = g.Slice(7, 107);
            
            //17107438075371556197
            var expected = new byte[] { 0xED, 0x69, 0xD4, 0x4A, 0x5B, 0x83, 0x8D, 0x65, 0xA9, 0x52, 0x1D, 0x03, 0x00 };
            AssertBitsArray(actual, expected, 100);
        }
        
        private void AssertBitsArray(Bits actual, byte[] expectedArray, int expectedLength)
        {
            Assert.That(actual.Length, Is.EqualTo(expectedLength), "Length differs");

            var actualArray = new byte[Util.Cdiv(expectedLength, 8)];
            actual.ToBytes(actualArray);
            
            for (int i = 0; i < expectedArray.Length; i++)
            {
                Assert.That(actualArray[i], Is.EqualTo(expectedArray[i]), $"index={i}: actual byte = {actualArray[i]:X}, expected byte = {expectedArray[i]:X}");
            }
        }
    }
}