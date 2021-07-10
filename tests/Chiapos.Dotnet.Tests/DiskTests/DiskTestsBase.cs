using System;
using Chiapos.Dotnet.Disks;

namespace Chiapos.Dotnet.Tests
{
    public class DiskTestsBase
    {
        public const int num_test_entries = 2000000;

        public void write_disk_file(FileDisk df)
        {
            uint val = 0;
            for (uint i = 0; i < num_test_entries; ++i) {
                df.Write(i * 4, BitConverter.GetBytes(val));
                ++val;
            }
        }
    }
}