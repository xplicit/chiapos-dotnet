using System;
using Mono.Options;

namespace Chiapos.Dotnet
{
    class Program
    {
        static void Main(string[] args)
        {
            var startupData = new StartupData();

            var p = new OptionSet
            {
                {
                    "k|size", "Plot size", v => startupData.k = byte.Parse(v)
                },
                {
                    "r|threads", "Number of threads", v => startupData.num_threads = byte.Parse(v)
                },
                {
                    "u|buckets", "Number of buckets", v => startupData.num_buckets = uint.Parse(v)
                },
                {
                    "s|stripes", "Size of stripes", v => startupData.num_stripes = ulong.Parse(v)
                },
                {
                    "t|tempdir", "Temporary directory", v => startupData.tempdir = v
                },
                {
                    "2|tempdir2", "Second temporary directory", v => startupData.tempdir2 = v
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
                    "b|buffer", "Megabytes to be used as buffer for sorting and plotting", v => startupData.buffmegabytes = uint.Parse(v)
                },
                {
                    "p|progress", "Display progress percentage during plotting", v => startupData.show_progress = v != null
                }
            };

            var extra = p.Parse(args);
            
            DiskPlotter plotter = new DiskPlotter();
            plotter.CreatePlotDisk(
                startupData.tempdir,
                startupData.tempdir2,
                startupData.finaldir,
                startupData.filename,
                startupData.k,
                Convert.FromHexString(startupData.memo),
                Convert.FromHexString(startupData.id),
                startupData.buffmegabytes,
                startupData.num_buckets,
                startupData.num_stripes,
                startupData.num_threads,
                false,
               startupData.show_progress);

        }
    }
}