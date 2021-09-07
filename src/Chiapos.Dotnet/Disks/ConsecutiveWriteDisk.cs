using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Chiapos.Dotnet.Disks
{
    public class ConsecutiveWriteDisk
    {
        private string filename;
        private readonly int entrySize;
        
        private Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10 * 1024 * 1024, resumeWriterThreshold: 3 * 1024 * 1024));
        private FileStream file;

        private byte[] tmpBuffer = new byte[1024 * 1024];
        private int tmpBufferWritten = 0;
            
        private ulong bytesWritten;
        private Task savingTask;

        private Semaphore sem = new Semaphore(16, 32);
        
        public string GetFileName() => filename;

        public ulong GetBytesWritten() => Interlocked.Read(ref bytesWritten);
        
        public ConsecutiveWriteDisk(string filename, int entrySize)
        {
            this.filename = filename;
            this.entrySize = entrySize;
        }

        public Memory<byte> GetBuffer()
        {
            return pipe.Writer.GetMemory(512 * 1024);
        }
        
        public async void SendBufferToWrite(int bytesWrittenToBuffer)
        {
            pipe.Writer.Advance(bytesWrittenToBuffer);
            FlushResult result = await pipe.Writer.FlushAsync();
            savingTask ??= WriteFileFromPipeAsync();
        }

        public async Task Close()
        {
            await pipe.Writer.CompleteAsync();
        }
        
        async Task WriteFileFromPipeAsync()
        {
            var reader = pipe.Reader;
            
            while (true)
            {
                ReadResult result = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                while (TryReadEntry(ref buffer, out ReadOnlySequence<byte> entry))
                {
                    // Process the entry.
                    SaveEntryToFile(entry);
                }

                // Tell the PipeReader how much of the buffer has been consumed.
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming.
                if (result.IsCompleted)
                {
                    FlushTempBuffer();
                    break;
                }
            }

            // Mark the PipeReader as complete.
            await reader.CompleteAsync();
        }
        
        bool TryReadEntry(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> entry)
        {
            if (buffer.Length < entrySize)
            {
                entry = default;
                return false;
            }

            entry = buffer.Slice(0, entrySize);
            buffer = buffer.Slice(entrySize);
            return true;
        }

        private void SaveEntryToFile(ReadOnlySequence<byte> entry)
        {
            if (file == null) 
                Open();

            foreach (var memory in entry)
            {
                if (tmpBufferWritten + memory.Length > tmpBuffer.Length)
                {
                    FlushTempBuffer();
                }

                memory.CopyTo(new Memory<byte>(tmpBuffer, tmpBufferWritten, tmpBuffer.Length - tmpBufferWritten));
                tmpBufferWritten += memory.Length;
            }
        }

        private void FlushTempBuffer()
        {
            while (tmpBufferWritten != 0)
            {
                try
                {
                    sem.WaitOne();
                    file.Write(tmpBuffer, 0, tmpBufferWritten);
                    sem.Release();
                    Interlocked.Add(ref bytesWritten, (ulong)tmpBufferWritten);
                    tmpBufferWritten = 0;
                }
                catch (Exception ex)
                {
                    sem.Release();
                    Console.WriteLine(
                        $"Couldn't write {tmpBufferWritten} bytes at offset {file.Position} from {filename}. Error {ex.Message}. Retrying in five minutes");
                    file.Close();
                    file = null;
                    Task.Delay(TimeSpan.FromMinutes(5)).Wait();
                    Open();
                }
            }
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

        public async void WaitSaveData()
        {
            if (savingTask != null)
                await savingTask;
        }
        public async Task FlushCache()
        {
            Close().Wait();
            if (savingTask != null)
                await savingTask;
            file?.Close();
        }

        public void FreeMemory()
        {
            file?.Close();
        }

    }
}