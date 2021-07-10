using System;
using System.IO;
using Chiapos.Dotnet.Disks;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    public class FilteredDiskTests : DiskTestsBase
    {
        private FileDisk d;
        
        [SetUp]
        public void Setup()
        {
            d = new FileDisk("test_file.bin");
            write_disk_file(d);
        }
        
        [TearDown]
        public void TearDown()
        {
            File.Delete("test_file.bin");
        }

        [Test]
        public void FilteredDisk_FilterEven()
        { 
            var bd = new BufferedDisk(d, num_test_entries * 4);
            // filter every other entry (starting with 0)
            var filter = new Bitfield(num_test_entries);
            for (uint i = 0; i < num_test_entries; ++i)
            {
                if ((i & 1) == 1) filter.Set(i);
            }

            var fd = new FilteredDisk(bd, filter, 4);

            for (uint i = 0; i < num_test_entries / 2 - 1; ++i)
            {
                var buffer = fd.Read(i * 4, 4);
                var val = BitConverter.ToUInt32(buffer);
                Assert.That(val, Is.EqualTo(i * 2 + 1));
            }

            // don't go all the way down to 0, every backwards read cursor movement will
            // print a warning
            for (uint i = num_test_entries / 2 - 1; i > num_test_entries / 2 + 200; --i)
            {
                var buffer = fd.Read(i * 4, 4);
                var val = BitConverter.ToUInt32(buffer);
                Assert.That(val, Is.EqualTo(i * 2 + 1));
            }
        }

        [Test]
        public void FilteredDisk_FilterOdd()
        {
            var bd = new BufferedDisk(d, num_test_entries * 4);
            // filter every other entry (starting with 0)
            var filter = new Bitfield(num_test_entries);
            for (uint i = 0; i < num_test_entries; ++i)
            {
                if ((i & 1) == 0) filter.Set(i);
            }

            var fd = new FilteredDisk(bd, filter, 4);

            Console.WriteLine("Test 1");
            for (uint i = 0; i < num_test_entries / 2 - 1; ++i)
            {
                var buffer = fd.Read(i * 4, 4);
                var val = BitConverter.ToUInt32(buffer);
                Assert.That(val, Is.EqualTo(i * 2));
            }

            // don't go all the way down to 0, every backwards read cursor movement will
            // print a warning
            for (uint i = num_test_entries / 2 - 1; i > num_test_entries / 2 + 200; --i)
            {
                var buffer = fd.Read(i * 4, 4);
                var val = BitConverter.ToUInt32(buffer);
                Assert.That(val, Is.EqualTo(i * 2));
            }
        }
/*
    SECTION("empty bitfield")
    {
        BufferedDisk bd(&d, num_test_entries * 4);
        bitfield filter(num_test_entries);
        FilteredDisk fd(std::move(bd), std::move(filter), 4);
    }
*/

/*        remove("test_file.bin");
}

         */
    }
}