namespace Chiapos.Dotnet
{
    internal class StartupData
    {
        public byte k { get; set; }
        public int num_threads { get; set; }
        public int num_buckets { get; set; }
        public int num_stripes { get; set; }
        public string tempdir { get; set; }
        public string tmpdir2 { get; set; }
        public string finaldir { get; set; }
        public string filename { get; set; }
        public string memo { get; set; }
        public string id { get; set; }
        public int buffmegabytes { get; set; }
        public int show_progress { get; set; }
    }
}