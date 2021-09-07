using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Chiapos.Dotnet.Disks
{
    public class RingBufferDisk
    {
        public byte[] buffer = new byte[1024 * 1024];
        public int writePointer;
        public int readPointer;
        private ulong bytesWritten;

        private object readLock = new();
        private bool shutdown = false;
        private ManualResetEvent finished = new ManualResetEvent(false);

        private string filename;
        private FileStream file;

        public string GetFileName() => filename;

        public ulong GetBytesWritten() => Interlocked.Read(ref bytesWritten);

        public RingBufferDisk(string filename)
        {
            this.filename = filename;
        }

        public void Start()
        {
            Open();
            new Thread( x => Read()).Start();
        }

        public void FlushCache()
        {
            lock (readLock)
            {
                shutdown = true;
                Monitor.PulseAll(readLock);
            }

            finished.WaitOne();
        }

        public void Write(ReadOnlySpan<byte> entry)
        {
            lock (readLock)
            {
                while (readPointer < writePointer && (readPointer + entry.Length) % buffer.Length >= writePointer)
                {
                    Monitor.Wait(readLock);
                }
            }

            lock (readLock)
            {
                if (readPointer + entry.Length > buffer.Length)
                {
                    int firstSpanLength = buffer.Length - readPointer;
                    entry[..firstSpanLength].CopyTo(buffer.AsSpan(readPointer));
                    entry[firstSpanLength..].CopyTo(buffer);
                    readPointer = entry.Length - firstSpanLength;
                }
                else
                {
                    entry.CopyTo(buffer.AsSpan(readPointer));
                    readPointer += entry.Length;
                }
                
                Monitor.PulseAll(readLock);
            }
        }

        public void Read()
        {
            int toPointer;

            while (true)
            {
                lock (readLock)
                {
                    while (writePointer == readPointer && !shutdown)
                    {
                        Monitor.Wait(readLock);
                    }
                    
                    if (shutdown && writePointer == readPointer)
                    {
                        break;
                    }

                    toPointer = readPointer;
                }

                if (writePointer < toPointer)
                {
                    file.Write(buffer.AsSpan(writePointer, toPointer - writePointer));
                }
                else
                {
                    file.Write(buffer.AsSpan(writePointer));
                    file.Write(buffer.AsSpan(0, toPointer));
                }

                lock (readLock)
                {
                    writePointer = toPointer;
                    Monitor.PulseAll(readLock);
                }
            }
            
            file.Close();
            finished.Set();
        }

        public void Open()
        {
            while (file == null)
            {
                try
                {
                    file = File.Open(filename, FileMode.Append, FileAccess.Write);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Could not open {filename}: +{ex.Message}. Retrying in five minutes");
                    Task.Delay(TimeSpan.FromMinutes(5)).Wait();
                }
            }
        }

        public void FreeMemory()
        {
        }
    }
}