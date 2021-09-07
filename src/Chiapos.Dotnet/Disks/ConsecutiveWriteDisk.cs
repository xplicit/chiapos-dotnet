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

        private ulong bytesWritten;
        private Task savingTask;
        
        public string GetFileName() => filename;

        public ulong GetBytesWritten() => Interlocked.Read(ref bytesWritten);
        
        public ConsecutiveWriteDisk(string filename, int entrySize)
        {
            this.filename = filename;
            this.entrySize = entrySize;
        }

        public Memory<byte> GetBuffer()
        {
            return pipe.Writer.GetMemory(entrySize);
        }
        
        public async void SendBufferToWrite(int bytesWrittenToBuffer)
        {
            pipe.Writer.Advance(bytesWrittenToBuffer);
            FlushResult result = await pipe.Writer.FlushAsync();
            savingTask ??= WriteFileFromPipeAsync();
        }

        public async void Close()
        {
            await pipe.Writer.CompleteAsync();
            file?.Close();
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
                try
                {
                    file.Write(memory.Span);
                    Interlocked.Add(ref bytesWritten, (ulong)memory.Span.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Couldn't write {memory.Length} bytes at offset {file.Position} from {filename}. Error {ex.Message}. Retrying in five minutes");
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
            Close();
            if (savingTask != null)
                await savingTask;
        }

        public void FreeMemory()
        {
            file?.Close();
        }

    }
}