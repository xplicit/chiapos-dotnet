using Dirichlet.Numerics;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    public class UtilTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void SliceInt64FromBytes_1_bit()
        {
            byte[] bytes = new byte[9 + 7]{1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0};

            // since we interpret the first 64 bits (8 bytes) as big endian, the
            // first byte is 0x01
            Assert.That(Util.SliceInt64FromBytes(bytes, 0, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 1, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 2, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 3, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 4, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 5, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 6, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 7, 1) == 1);

            // the second byte is 0x2
            Assert.That(Util.SliceInt64FromBytes(bytes, 8, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 9, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 10, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 11, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 12, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 13, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 14, 1) == 1);
            Assert.That(Util.SliceInt64FromBytes(bytes, 15, 1) == 0);

            // the third byte is 0x3
            Assert.That(Util.SliceInt64FromBytes(bytes, 16, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 17, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 18, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 19, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 20, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 21, 1) == 0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 22, 1) == 1);
            Assert.That(Util.SliceInt64FromBytes(bytes, 23, 1) == 1);
        }

        [Test]
        public void SliceInt64FromBytes_8_bits()
        {
            byte[] bytes = new byte[9 + 7]{1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0};

            // since we interpret the first 64 bits (8 bytes) as big endian, the
            // first byte is 0x01
            Assert.That(Util.SliceInt64FromBytes(bytes, 0, 8) == 0b00000001);
            Assert.That(Util.SliceInt64FromBytes(bytes, 1, 8) == 0b00000010);
            Assert.That(Util.SliceInt64FromBytes(bytes, 2, 8) == 0b00000100);
            Assert.That(Util.SliceInt64FromBytes(bytes, 3, 8) == 0b00001000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 4, 8) == 0b00010000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 5, 8) == 0b00100000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 6, 8) == 0b01000000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 7, 8) == 0b10000001);

            Assert.That(Util.SliceInt64FromBytes(bytes,  8, 8) == 0b00000010);
            Assert.That(Util.SliceInt64FromBytes(bytes,  9, 8) == 0b00000100);
            Assert.That(Util.SliceInt64FromBytes(bytes, 10, 8) == 0b00001000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 11, 8) == 0b00010000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 12, 8) == 0b00100000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 13, 8) == 0b01000000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 14, 8) == 0b10000000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 15, 8) == 0b00000001);

            Assert.That(Util.SliceInt64FromBytes(bytes, 16, 8) == 0b00000011);
            Assert.That(Util.SliceInt64FromBytes(bytes, 17, 8) == 0b00000110);
            Assert.That(Util.SliceInt64FromBytes(bytes, 18, 8) == 0b00001100);
            Assert.That(Util.SliceInt64FromBytes(bytes, 19, 8) == 0b00011000);
        }

        [Test]
        public void SliceInt64FromBytes_24_bits()
        {
            byte[] bytes = new byte[9 + 7] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0};
            
            // since we interpret the first 64 bits (8 bytes) as big endian, the
            // first byte is 0x01
            Assert.That(Util.SliceInt64FromBytes(bytes, 0, 24) == 0b00000001_00000010_00000011);
            Assert.That(Util.SliceInt64FromBytes(bytes, 1, 24) == 0b0000001_00000010_00000011_0);
            Assert.That(Util.SliceInt64FromBytes(bytes, 2, 24) == 0b000001_00000010_00000011_00);
            Assert.That(Util.SliceInt64FromBytes(bytes, 3, 24) == 0b00001_00000010_00000011_000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 4, 24) == 0b0001_00000010_00000011_0000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 5, 24) == 0b001_00000010_00000011_00000);
            Assert.That(Util.SliceInt64FromBytes(bytes, 6, 24) == 0b01_00000010_00000011_000001);
            Assert.That(Util.SliceInt64FromBytes(bytes, 7, 24) == 0b1_00000010_00000011_0000010);
        }

        [Test]
        public void SliceInt64FromBytes_Full()
        {
            byte[] bytes = new byte[9 + 7] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 0, 0, 0, 0, 0, 0};

            // since we interpret the first 64 bits (8 bytes) as big endian, the
            // first byte is 0x01
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 0, 64) == 0x0102030405060708ul);
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 1, 64) == 0x0102030405060708ul << 1);
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 2, 64) == 0x0102030405060708ul << 2);
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 3, 64) == 0x0102030405060708ul << 3);
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 4, 64) == 0x1020304050607080ul);
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 5, 64) == ((0x1020304050607080ul << 1) | 0b1));
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 6, 64) == ((0x1020304050607080ul << 2) | 0b10));
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 7, 64) == ((0x1020304050607080ul << 3) | 0b100));
            Assert.That(Util.SliceInt64FromBytesFull(bytes, 8, 64) == 0x0203040506070809ul);
        }

        [Test]
        public void SliceInt128_IncrementDecrement()
        {
            var bytes = new byte[3 + 7] {45, 172, 225, 0, 0, 0, 0, 0, 0, 0};
            
            Assert.That(Util.SliceInt64FromBytes(bytes, 2, 19) == 374172);
            var bytes2 = new byte[1 + 7] {213, 0, 0, 0, 0, 0, 0, 0};
            Assert.That(Util.SliceInt64FromBytes(bytes2, 1, 5) == 21);
            var bytes3 = new byte[17 + 7] {1, 2, 3, 4, 5, 6, 7, 255, 255, 10, 11, 12, 13, 14, 15, 16, 255, 0, 0, 0, 0, 0, 0, 0};
            UInt128.Create(out UInt128 int3, 0xff0a0b0c0d0e0f10, 0x01020304050607ff);
            Assert.That(Util.SliceInt64FromBytes(bytes3, 64, 64), Is.EqualTo((ulong)int3));
            Assert.That(Util.SliceInt64FromBytes(bytes3, 0, 60) == (ulong)(int3 >> 68));
            Assert.That(Util.SliceInt128FromBytes(bytes3, 0, 60) == int3 >> 68);
            Assert.That(Util.SliceInt128FromBytes(bytes3, 7, 64) == int3 >> 57);
            Assert.That(Util.SliceInt128FromBytes(bytes3, 7, 72) == int3 >> 49);
            Assert.That(Util.SliceInt128FromBytes(bytes3, 0, 128) == int3);
            Assert.That(Util.SliceInt128FromBytes(bytes3, 3, 125) == int3);
            Assert.That(Util.SliceInt128FromBytes(bytes3, 2, 125) == int3 >> 1);
            Assert.That(Util.SliceInt128FromBytes(bytes3, 0, 120) == int3 >> 8);
            Assert.That(Util.SliceInt128FromBytes(bytes3, 3, 127) == (int3 << 2 | 3));
        }

        [Test]
        public void RoundPow2_Values()
        {
            Assert.That(Util.RoundPow2(1), Is.EqualTo(1));
            Assert.That(Util.RoundPow2(2), Is.EqualTo(2));
            Assert.That(Util.RoundPow2(3), Is.EqualTo(2));
            Assert.That(Util.RoundPow2(32), Is.EqualTo(32));
            Assert.That(Util.RoundPow2(60), Is.EqualTo(32));
            Assert.That(Util.RoundPow2(1023), Is.EqualTo(512));
            Assert.That(Util.RoundPow2(1024), Is.EqualTo(1024));
            Assert.That(Util.RoundPow2(1025), Is.EqualTo(1024));
        }
    }
}