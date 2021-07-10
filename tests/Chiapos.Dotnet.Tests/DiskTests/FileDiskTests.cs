using System;
using System.IO;
using Chiapos.Dotnet.Disks;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    [TestFixture]
    public class FileDiskTests : DiskTestsBase
    {
        [Test]
        public void FileDisk_Write_Read()
        {
            FileDisk d = new FileDisk("test_file.bin");
            write_disk_file(d);
            var buffer = new byte[4];

            uint val = 0;
            for (uint i = 0; i < num_test_entries; ++i)
            {
                d.Read(i * 4, buffer, 0, 4);
                val = BitConverter.ToUInt32(buffer);
                Assert.That(val, Is.EqualTo(i));
            }

            for (uint i = num_test_entries - 1; i > 0; --i)
            {
                d.Read(i * 4, buffer, 0, 4);
                val = BitConverter.ToUInt32(buffer);
                Assert.That(val, Is.EqualTo(i));
            }

            File.Delete("test_file.bin");
        }
    }
}