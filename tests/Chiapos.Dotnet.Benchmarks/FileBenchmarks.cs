using System.IO;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;

namespace Chiapos.Dotnet.Benchmarks
{
    //[SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 5, targetCount: 5, id: "FastAndDirtyJob")]
    public class FileBenchmarks
    {
        public const string FilePath = "/Volumes/ramdisk";
        public const int length = 2 * 1024 * 1024;
        
        object[] syncRoot = Enumerable.Range(0, 256).Select(_ => new object()).ToArray();

        //[Benchmark]
        public void Write1Files1Threads() => WriteFiles(1, 1, 8);

        [Benchmark(Baseline = true)]
        public void Write128Files4Threads() => WriteFiles(128, 4, 8);

        //[Benchmark()]
        public void Write128Files8Threads() => WriteFiles(128, 8, 8);

        //[Benchmark]
        public void Write128Files16Threads() => WriteFiles(128, 16, 8);

        [Benchmark]
        public void Write128Files16Threads256Buffer() => WriteFiles(128, 16, 256);

        [Benchmark]
        public void Write128Files16Threads4096Buffer() => WriteFiles(128, 16, 4096);
        
        [Benchmark]
        public void Write128Files16Threads256kBuffer() => WriteFiles(128, 16, 256 * 1024);

        //[Benchmark]
        public void Write256Files4Threads() => WriteFiles(256, 4, 8);

        //[Benchmark]
        public void Write256Files8Threads() => WriteFiles(256, 8, 8);

        //[Benchmark]
        public void Write256Files16Threads() => WriteFiles(256, 16, 8);
        
        //[Benchmark]
        public void Write256Files256Threads() => WriteFiles(256, 256, 8);
        
        [Benchmark]
        public void Write256Files256Threads256kBuffer() => WriteFiles(256, 256, 256 * 1024);
        
        [Benchmark]
        public void Write256Files16Threads4096buffer() => WriteFiles(256, 16, 4096);

        [Benchmark]
        public void Write256Files16Threads256kbuffer() => WriteFiles(256, 16, 256 * 1024);
        
        [Benchmark]
        public void Write256Files16Threads256buffer() => WriteFiles(256, 16, 256);
        
        public void WriteFiles(int buckets, int threads, int bufferSize, int entrySize = 8)
        {
            FileStream[] files = new FileStream[buckets];
            int multiplier = 256 / buckets;
            
            for (int i = 0; i < buckets; i++)
            {
                files[i] = File.Open(Path.Combine(FilePath, $"{i}.tmp"), FileMode.Create);
            }

            int semaphore = threads;
            var semaphoreEvt = new ManualResetEvent(false);

            int fileSaveSemaphore = threads;
            var fileSaveSemaphoreEvt = new ManualResetEvent(false);
            
            for (int t = 0; t < threads; t++)
            {
                int curThread = t;
                
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    byte[] buffer = new byte[bufferSize];
                    
                    for (int k = 0; k < (length /entrySize * (buckets / threads) * multiplier); k++)
                    {
                        int n = k & (buckets - 1);

                        lock (syncRoot[n])
                        {
                            files[n].Write(buffer, 0, buffer.Length);
                        }
                    }
                    
                    if (Interlocked.Decrement(ref fileSaveSemaphore) == 0) 
                        fileSaveSemaphoreEvt.Set();

                    fileSaveSemaphoreEvt.WaitOne();
                    
                    for (int i = 0; i < buckets; i++)
                    {
                        if (i % threads == curThread)
                            files[i].Close();
                    }
                    
                    if (Interlocked.Decrement(ref semaphore) == 0) 
                        semaphoreEvt.Set();
                });
            }

            semaphoreEvt.WaitOne();
        }
    }
}