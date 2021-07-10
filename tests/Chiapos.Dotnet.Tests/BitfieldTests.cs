using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    [TestFixture]
    public class BitfieldTests
    {
        [Test]
        public void Bitfield_Simple()
        {
            var b = new Bitfield(4);
            Assert.That(!b.Get(0));
            Assert.That(!b.Get(1));
            Assert.That(!b.Get(2));
            Assert.That(!b.Get(3));

            b.Set(0);
            Assert.That(b.Get(0));
            Assert.That(!b.Get(1));
            Assert.That(!b.Get(2));
            Assert.That(!b.Get(3));

            b.Set(1);
            Assert.That(b.Get(0));
            Assert.That(b.Get(1));
            Assert.That(!b.Get(2));
            Assert.That(!b.Get(3));

            b.Set(3);
            Assert.That(b.Get(0));
            Assert.That(b.Get(1));
            Assert.That(!b.Get(2));
            Assert.That(b.Get(3));
        }

        [Test]
        public void Bitfield_Count()
        {
            var b = new Bitfield(512);

            for (uint i = 0; i < 512; ++i)
            {
                Assert.That(b.Count(0, 512) == i);
                Assert.That(!b.Get(i));
                b.Set(i);
                Assert.That(b.Get(i));
            }

            Assert.That(b.Count(0, 512) == 512);
        }

        [Test]
        public void Bitfield_Count_Unaligned()
        {
            var b = new Bitfield(512);

            for (uint i = 0; i < 512; ++i)
            {
                b.Set(i);
            }

            for (uint i = 0; i < 512; ++i)
            {
                Assert.That(b.Count(0, i), Is.EqualTo(i));
            }
        }

        [Test]
        public void Bitfield_index_simple()
        {
            var b = new Bitfield(64);
            b.Set(0);
            b.Set(1);
            b.Set(3);
            var idx = new BitfieldIndex(b);
            Assert.That(idx.Lookup(0, 0) == (0, 0));
            Assert.That(idx.Lookup(0, 1), Is.EqualTo((0, 1)));

            Assert.That(idx.Lookup(0, 3) == (0, 2));

            Assert.That(idx.Lookup(1, 0) == (1, 0));
            Assert.That(idx.Lookup(1, 2) == (1, 1));
            Assert.That(idx.Lookup(3, 0) == (2, 0));
        }

        [Test]
        public void BitfieldIndex_UseIndex()
        {
            var b = new Bitfield(1048576);
            Assert.That(b.Length, Is.EqualTo(1048576));
            b.Set(1048576 - 3);
            b.Set(1048576 - 2);
            b.Set(1048576 - 1);
            var idx = new BitfieldIndex(b);
            Assert.That(idx.Lookup(1048576 - 3, 1) == (0, 1));
            Assert.That(idx.Lookup(1048576 - 2, 1) == (1, 1));
        }

        [Test]
        public void BitfiledIndex_EdgeCases()
        {
            var b = new Bitfield(1048576);
            Assert.That(b.Length, Is.EqualTo(1048576));
            b.Set(0);
            b.Set(BitfieldIndex.kIndexBucket);
            b.Set(BitfieldIndex.kIndexBucket * 2);
            b.Set(1048576 - 1);
            var idx = new BitfieldIndex(b);
            Assert.That(idx.Lookup(0, 0) == (0, 0));
            Assert.That(idx.Lookup(0, BitfieldIndex.kIndexBucket) == (0, 1));
            Assert.That(idx.Lookup(0, BitfieldIndex.kIndexBucket * 2) == (0, 2));
            Assert.That(idx.Lookup(0, 1048576 - 1) == (0, 3));

            Assert.That(idx.Lookup(BitfieldIndex.kIndexBucket, 0) == (1, 0));
            Assert.That(idx.Lookup(BitfieldIndex.kIndexBucket, BitfieldIndex.kIndexBucket) == (1, 1));
            Assert.That(idx.Lookup(BitfieldIndex.kIndexBucket, 1048576 - 1 - BitfieldIndex.kIndexBucket)
                        == (1, 2));

            Assert.That(idx.Lookup(BitfieldIndex.kIndexBucket * 2, 1048576 - 1 - BitfieldIndex.kIndexBucket * 2)
                        == (2, 1));
            Assert.That(idx.Lookup(1048576 - 1, 0) == (3, 0));
        }

        private void test_bitfield_size(ulong size)
        {
            var b = new Bitfield(size);
            b.Set(0);
            b.Set(size - 1);
            var idx = new BitfieldIndex(b);
            Assert.That(idx.Lookup(0, 0) == (0, 0));
            Assert.That(idx.Lookup(0, size - 1) == (0, 1));
            Assert.That(idx.Lookup(size - 1, 0) == (1, 0));
        }

        [Test]
        public void BitfieldIndex_EdgeSizes()
        {
            test_bitfield_size(BitfieldIndex.kIndexBucket - 1);
            test_bitfield_size(BitfieldIndex.kIndexBucket);
            test_bitfield_size(BitfieldIndex.kIndexBucket + 1);
        }
    }
}