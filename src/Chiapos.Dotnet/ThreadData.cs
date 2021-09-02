using System.Collections.Generic;
using System.Threading;
using Chiapos.Dotnet.Disks;

namespace Chiapos.Dotnet
{
    public class ThreadData
    {
        public int index;
        public int phase1_num_threads;
        public AutoResetEvent mine;
        public AutoResetEvent theirs;
        public ulong right_entry_size_bytes;
        public byte k;
        public byte table_index;
        public byte metadata_size;
        public uint entry_size_bytes;
        public byte pos_size;
        public ulong prevtableentries;
        public uint compressed_entry_size_bytes;
        public List<FileDisk> ptmp_1_disks;
    }
}