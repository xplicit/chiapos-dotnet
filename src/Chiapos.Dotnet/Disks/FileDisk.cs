using System;
using System.IO;
using System.Threading;

namespace Chiapos.Dotnet.Disks
{
    public class FileDisk
    {
        private readonly string filename;
        private FileStream file;
        private ulong readPos = 0;
        private ulong writePos = 0;
        private ulong writeMax = 0;
        bool bReading = true;

        [Flags]
        public enum OpenFlags
        {
            WriteFlag,
            RetryOpenFlag
        }

        public FileDisk(string filename)
        {
            this.filename = filename;
        }

        public void Open(OpenFlags flags)
        {
            while (file == null)
            {
                try
                {
                    file = File.Open(filename, FileMode.OpenOrCreate,
                        flags.HasFlag(OpenFlags.WriteFlag) ? FileAccess.ReadWrite : FileAccess.Read);
                }
                catch (Exception ex)
                {
                    if (!flags.HasFlag(OpenFlags.RetryOpenFlag))
                        throw;
                    
                    Console.WriteLine($"Could not open {filename}: +{ex.Message}. Retrying in five minutes");
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                }
            }
        }
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public void Read(ulong begin, byte[] buffer, ulong offset, ulong length)
        {
            Open(OpenFlags.RetryOpenFlag);
#if ENABLE_LOGGING
            disk_log(filename_, op_t::read, begin, length);
#endif
            // Seek, read, and replace into buffer
            ulong bytesRead  = 0;
            while (length > 0)
            {
                try
                {
                    if ((!bReading) || (begin != readPos))
                    {
                        file.Seek((long) begin, SeekOrigin.Begin);
                        bReading = true;
                    }

                    bytesRead = (ulong)file.Read(buffer, (int)offset, (int) length);
                    offset += bytesRead;
                    length -= bytesRead;
                    readPos += bytesRead;
                    begin = readPos;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"Only read {bytesRead} of {length} bytes at offset {begin} from {filename} with length {writeMax}. Error {ex.Message}. Retrying in five monutes");
                    Close();
                    bReading = false;
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                    Open(OpenFlags.RetryOpenFlag);
                }
            }
        }

        public void Write(ulong begin, ReadOnlySpan<byte> buffer)
        {
            Open(OpenFlags.WriteFlag|OpenFlags.RetryOpenFlag);
#if ENABLE_LOGGING
            disk_log(filename_, op_t::write, begin, length);
#endif
            int length = buffer.Length;
            while (length > 0)
            {
                try
                {
                    if ((bReading) || (begin != writePos))
                    {
                        file.Seek((long) begin, SeekOrigin.Begin);
                        bReading = false;
                    }

                    file.Write(buffer);
                    writePos = begin + (ulong)length;
                    writeMax = Math.Max(writeMax, writePos);
                    length = 0;
                }
                catch (Exception ex)
                {
                    writePos = ulong.MaxValue;
                    Console.WriteLine(
                        $"Only wrote bytes of {length} bytes at offset {begin} to {filename} with length {writeMax}. Error {ex.Message}. Retrying in five minutes.");
                    // Close, Reopen, and re-seek the file to recover in case the filesystem
                    // has been remounted.
                    Close();
                    bReading = false;
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                    Open(OpenFlags.WriteFlag | OpenFlags.RetryOpenFlag);
                }
            }
        }
        public void Close()
        {
            file?.Close();
            file = null;
            readPos = 0;
            writePos = 0;
        }

        public void Truncate(ulong newSize)
        {
            if (!file.CanWrite)
            {
                Close();
                Open(OpenFlags.WriteFlag | OpenFlags.RetryOpenFlag);
            }

            try
            {
                file.SetLength((long) newSize);
            }
            finally
            {
                Close();
            }
        }

        public string GetFileName() => filename;

        public ulong GetWriteMax() => writeMax;
    }
}