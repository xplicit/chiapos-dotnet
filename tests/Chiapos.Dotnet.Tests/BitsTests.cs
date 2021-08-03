using System;
using System.Collections;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    [TestFixture]
    public class BitsTests
    {
        public void Slicing_and_Manipulation()
        {
            BitArray bits = new BitArray(1024);
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
            var bitsA = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55};
            var bitsB = new byte[] {0xAF, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};
            
            Bits a = new Bits(new ReadOnlySpan<byte>(bitsA, 0, 5), 32 + 5);
            Bits b = new Bits(bitsB, 64);

            var c = a + b;
            var expected = new byte[] {0x55, 0x55, 0x55, 0b_0101_0101, 0b_111_10101, 0b_010_10101, 0x55, 0x55, 0x55, 0x55, 0x55, 0b_0101_0101, 0b_000_10101};
            AssertBitsArray(c, expected, 64 + 32 + 5);
        }

        [Test]
        public void OperatorPlus_SumOfArraysHaveNoRemainingBits()
        {
            var bitsA = new byte[] {0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55, 0x55};
            var bitsB = new byte[] {0xAF, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA};

            Bits a = new Bits(new ReadOnlySpan<byte>(bitsA, 0, 5), 32 + 5);
            Bits b = new Bits(bitsB,64 - 5);
            
            var c = a + b;
            var expected = new byte[] {0x55, 0x55, 0x55, 0b_0101_0101, 0b_111_10101, 0b_010_10101, 0x55, 0x55, 0x55, 0x55, 0x55, 0b_0101_0101};
            AssertBitsArray(c, expected, 64 + 32);
        }

        private void AssertBitsArray(Bits actual, byte[] expectedArray, int expectedLength)
        {
            Assert.That(actual.Length, Is.EqualTo(expectedLength));

            var actualArray = new byte[expectedLength / sizeof(byte) + 1];
            actual.ToBytes(actualArray);
            
            for (int i = 0; i < expectedArray.Length; i++)
            {
                Assert.That(actualArray[i], Is.EqualTo(expectedArray[i]), $"{i}: actual byte = {actualArray[i]:X}, expected byte = {expectedArray[i]:X}");
            }
        }
    }
}