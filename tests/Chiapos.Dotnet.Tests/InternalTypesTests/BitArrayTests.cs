using System;
using System.Linq;
using Chiapos.Dotnet.Collections;
using Dirichlet.Numerics;
using Microsoft.Win32.SafeHandles;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests.InternalTypesTests
{
    [TestFixture]
    public class BitArrayTests
    {
        [Test]
        public void Constructor_UInt128_ShouldWork()
        {
            BitArray bits = new BitArray(UInt128.MaxValue, 128);
            var actual = new byte[16];
            
            bits.CopyTo(actual, 0);
            Assert.That(actual, Is.All.EqualTo(255));
            
            UInt128.Create(out var b, 0xFFFF_FFFF, 0xFFFF_FFFF);
            bits = new BitArray(b, 128);
            bits.CopyTo(actual, 0);

            var original0 = BitConverter.GetBytes(b.S0);
            var original1 = BitConverter.GetBytes(b.S1);
            
            for (int i = 0; i < 8; i++)
            {
                Assert.That(actual[i], Is.EqualTo(original0[i]));
                Assert.That(actual[i], Is.EqualTo(original1[i]));
            }
        }

        [Test]
        public void Append_to_Aligned_BitArray()
        {
            //var bits = new BitArray(new[] {false, false, false, true, false, false, true, true});
            var bits = new BitArray(new[] {true, false, false, false, false, false, false, false});
            var bytes = new byte[1];
            bits.CopyTo(bytes, 0);
            
        }

        [Test]
        public void Append_to_NotAligned()
        {
            UInt128.Create(out UInt128 a, 0x5555_5555_5555_5555,0x5555_5555_5555_5555);
            var bitArray = new[] {true};

            var bits = new BitArray(bitArray);
            bits.AppendValue(a, 1);
            Assert.That(bits.Length, Is.EqualTo(2));
            Assert.That(bits.Get(0), Is.True);
            Assert.That(bits.Get(1), Is.True);

            for (int len = 1; len <= 128; len++)
            {
                bits = new BitArray(bitArray);
                bits.AppendValue(a, len);
                Assert.That(bits.Length, Is.EqualTo(bitArray.Length + len), $"Len={len}");
                for (int i = 0; i < bitArray.Length; i++)
                {
                    Assert.That(bits.Get(i), Is.EqualTo(bitArray[i]), $"Len={len} i={i}");
                }
                for (int i = bitArray.Length; i < bits.Length; i++)
                {
                    Assert.That(bits.Get(i), Is.EqualTo((i - bitArray.Length) % 2 == 0), $"Len={len} i={i}");
                }
            }
        }

        [Test]
        public void Create_from_ulong_Array_with_SmallLength()
        {
            var values = Enumerable.Repeat<ulong>(0x5555_5555_5555_5555, 10).ToArray();

            var bits = new BitArray(values, 3);
            for (int i = 0; i < bits.Length; i++)
            {
                Assert.That(bits.Get(i), Is.EqualTo(i % 3 != 1), $"i = {i}");
            }
        }
        
        [Test]
        public void Create_from_ulong_Array_with_LengthMore32()
        {
            const int valueLength = 37;
            var values = Enumerable.Repeat<ulong>(0xFFFF_0000_5555_5555, 10)
                .Select((x, i) => x + (1UL << 32)).ToArray();
            ulong mask = (1UL << valueLength) - 1;

            var bits = new BitArray(values, valueLength);
            for (int i = 0; i < values.Length; i++)
            {
                ulong value = 0;
                for (int j = 0; j < valueLength; j++)
                {
                    value += (bits.Get(i * valueLength + j) ? 1ul : 0ul) << j;
                }
                Assert.That(value, Is.EqualTo(values[i] & mask), $"i = {i}");
            }
        }

    }
}