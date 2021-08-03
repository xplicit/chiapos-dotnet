using System;
using Mono.Options;

namespace Chiapos.Dotnet
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var startupData = new StartupData();

            var p = new OptionSet
            {
                {
                    "k|size", "Plot size", v => startupData.k = byte.Parse(v)
                },
                {
                    "r|threads", "Number of threads", v => startupData.num_threads = int.Parse(v)
                },
                {
                    "u|buckets", "Number of buckets", v => startupData.num_buckets = int.Parse(v)
                },
                {
                    "s|stripes", "Size of stripes", v => startupData.num_stripes = int.Parse(v)
                },
                {
                    "t|tempdir", "Temporary directory", v => startupData.tempdir = v
                },
                {
                    "2|tempdir2", "Second temporary directory", v => startupData.tmpdir2 = v
                },
                {
                    "d|finaldir", "Final directory", v => startupData.finaldir = v
                },
                {
                    "f|file", "Filename", v => startupData.filename = v
                },
                {
                    "m|memo", "Memo to insert into the plot", v => startupData.memo = v
                },
                {
                    "i|id", "Unique 32-byte seed for the plot", v => startupData.id = v
                },
                {
                    "b|buffer", "Megabytes to be used as buffer for sorting and plotting", v => startupData.buffmegabytes = int.Parse(v)
                },
                {
                    "p|progress", "Display progress percentage during plotting", v => startupData.show_progress = int.Parse(v)
                }
            };

            var extra = p.Parse(args);
        }
    }
}