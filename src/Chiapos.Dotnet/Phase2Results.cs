using System;
using System.Collections.Generic;
using Chiapos.Dotnet.Disks;

namespace Chiapos.Dotnet
{
    public class Phase2Results
    {
        public IDisk disk_for_table(int table_index)
        {
            if (table_index == 1) return table1;
            else if (table_index == 7) return table7;
            else return output_files[table_index - 2];
        }
        
        public FilteredDisk table1 { get; set; }
        public BufferedDisk table7 { get; set; }
        public List<SortManager> output_files { get; set; }
        public List<ulong> table_sizes { get; set; }
    };
}