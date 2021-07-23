namespace Chiapos.Dotnet
{
    public class GlobalData
    {
        public ulong left_writer_count;
        public ulong right_writer_count;
        public ulong matches;
        public SortManager L_sort_manager;
        public SortManager R_sort_manager;
        public ulong left_writer_buf_entries;
        public ulong left_writer;
        public ulong right_writer;
        public ulong stripe_size;
        public int num_threads;
    }
}