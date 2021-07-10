using System.Collections.Generic;

namespace Chiapos.Dotnet
{
    public class Phase3Results
    {
        // Pointers to each table start byet in the final file
        public List<ulong> final_table_begin_pointers { get; set; }
        // Number of entries written for f7
        public ulong final_entries_written { get; set; }
        public ulong right_entry_size_bits { get; set; }

        public uint header_size { get; set; }
        public SortManager table7_sm { get; set; }
    }
}