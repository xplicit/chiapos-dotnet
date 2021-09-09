using System.Linq;
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

    }
}