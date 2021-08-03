namespace Chiapos.Dotnet
{
    internal class StartupData
    {
        public byte k { get; set; }
        public byte num_threads { get; set; }
        public uint num_buckets { get; set; }
        public ulong num_stripes { get; set; }
        public string tempdir { get; set; }
        public string tempdir2 { get; set; }
        public string finaldir { get; set; }
        public string filename { get; set; }
        public string memo { get; set; }
        public string id { get; set; }
        public uint buffmegabytes { get; set; }
        public bool show_progress { get; set; }
    }
}