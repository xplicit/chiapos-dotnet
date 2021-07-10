using System;
using System.IO;
using Chiapos.Dotnet.Disks;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    [TestFixture]
    public class BufferedDiskTests : DiskTestsBase
    {
        [Test]
        public void BufferedDisk_Write_Read()
        {
            FileDisk d = new FileDisk("test_file.bin");
            write_disk_file(d);

            BufferedDisk bd = new BufferedDisk(d, num_test_entries * 4);

            for (uint i = 0; i < num_test_entries; ++i)
            {
                var buffer = bd.Read(i * 4, 4);
                var val = BitConverter.ToUInt32(buffer);
                Assert.That(val, Is.EqualTo(i));
            }

            // don't go all the way down to 0, every backwards read cursor movement will
            // print a warning
            for (uint i = num_test_entries - 1; i > num_test_entries / 2 + 200; --i)
            {
                var buffer = bd.Read(i * 4, 4);
                var val = BitConverter.ToUInt32(buffer);
                Assert.That(val, Is.EqualTo(i));
            }

            File.Delete("test_file.bin");
        }
    }
}