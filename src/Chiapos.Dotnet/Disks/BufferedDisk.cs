using System;

namespace Chiapos.Dotnet.Disks
{
    public class BufferedDisk : IDisk
    {
        private readonly FileDisk disk;
        private ulong filesize;
        
        const ulong write_cache_length = 1024 * 1024;
        const ulong read_ahead = 1024 * 1024;

        // the file offset the read buffer was read from
        ulong read_buffer_start_ = ulong.MaxValue;
        byte[] read_buffer_;
        ulong read_buffer_size_ = 0;

        // the file offset the write buffer should be written back to
        // the write buffer is *only* for contiguous and sequential writes
        ulong write_buffer_start_ = 0;
        byte[] write_buffer_ = new byte[write_cache_length];
        ulong write_buffer_size_ = 0;

        public BufferedDisk(FileDisk disk, ulong filesize)
        {
            this.disk = disk;
            this.filesize = filesize;
        }

        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        static byte[] temp = new byte[128];

        public ReadOnlySpan<byte> Read(ulong begin, ulong length)
        {
            //assert(length < read_ahead);
            NeedReadCache();
            // all allocations need 7 bytes head-room, since
            // SliceInt64FromBytes() may overrun by 7 bytes
            if (read_buffer_start_ <= begin
                && read_buffer_start_ + read_buffer_size_ >= begin + length
                && read_buffer_start_ + read_ahead >= begin + length + 7)
            {
                // if the read is entirely inside the buffer, just return it
                return new ReadOnlySpan<byte>(read_buffer_, (int) (begin - read_buffer_start_),
                    read_buffer_.Length - (int) (begin - read_buffer_start_));
            }
            else if (begin >= read_buffer_start_ || begin == 0 || read_buffer_start_ == ulong.MaxValue)
            {
                // if the read is beyond the current buffer (i.e.
                // forward-sequential) move the buffer forward and read the next
                // buffer-capacity number of bytes.
                // this is also the case we enter the first time we perform a read,
                // where we haven't read anything into the buffer yet. Note that
                // begin == 0 won't reliably detect that case, since we may have
                // discarded the first entry and start at some low offset but still
                // greater than 0
                read_buffer_start_ = begin;
                ulong amount_to_read = Math.Min(filesize - read_buffer_start_, read_ahead);
                disk.Read(begin, read_buffer_, 0, amount_to_read);
                read_buffer_size_ = amount_to_read;
                return new ReadOnlySpan<byte>(read_buffer_);
            }
            else
            {
                // ideally this won't happen
                Console.WriteLine(
                    "Disk read position regressed. It's optimized for forward scans. Performance may suffer\n" +
                    $"   read-offset: {begin} read-length: {length} file-size: {filesize}" +
                    $" read-buffer: [{read_buffer_start_}, {read_buffer_size_}]" +
                    $" file: {disk.GetFileName()}");
                // all allocations need 7 bytes head-room, since
                // SliceInt64FromBytes() may overrun by 7 bytes
                //assert(length <= sizeof(temp) - 7);

                // if we're going backwards, don't wipe out the cache. We assume
                // forward sequential access
                disk.Read(begin, temp, 0, length);
                return temp;
            }
        }

        public void Write(ulong begin, ReadOnlySpan<byte> memcache)
        {
            if (begin == write_buffer_start_ + write_buffer_size_)
            {
                if (write_buffer_size_ + (ulong)memcache.Length <= write_cache_length)
                {
                    memcache.CopyTo(new Span<byte>(write_buffer_, (int)write_buffer_size_, write_buffer_.Length - (int)write_buffer_size_));
                    write_buffer_size_ += (ulong)memcache.Length;
                    return;
                }

                FlushCache();
            }

            if (write_buffer_size_ == 0 && write_cache_length >= (ulong)memcache.Length)
            {
                write_buffer_start_ = begin;
                memcache.CopyTo(new Span<byte>(write_buffer_, (int)write_buffer_size_, write_buffer_.Length - (int)write_buffer_size_));
                write_buffer_size_ = (ulong)memcache.Length;
                return;
            }

            disk.Write(begin, memcache);
        }

        private void NeedReadCache()
        {
            if (read_buffer_ != null) 
                return;
            read_buffer_ = new byte[read_ahead];
            read_buffer_start_ = ulong.MaxValue;
            read_buffer_size_ = 0;
        }
        
        public void FreeMemory()
        {
            FlushCache();

            read_buffer_ = null;
            read_buffer_size_ = 0;

            write_buffer_start_ = 0;
            write_buffer_size_ = 0;
        }

        public void FlushCache()
        {
            if (write_buffer_size_ == 0) return;

            disk.Write(write_buffer_start_, new ReadOnlySpan<byte>(write_buffer_, 0,(int)write_buffer_size_));
            write_buffer_size_ = 0;
        }

        public void Close()
        {
            disk.Close();
        }

        public void Truncate(ulong new_size)
        {
            FlushCache();
            disk.Truncate(new_size);
            filesize = new_size;
            FreeMemory();
        }

        public string GetFileName() => disk.GetFileName();

        public Span<byte> GetBuffer(ushort entrySize)
        {
            if (write_buffer_size_ + entrySize > write_cache_length)
            {
                ulong savedBytes = write_buffer_size_;
                FlushCache();
                write_buffer_start_ += savedBytes;
            }

            return new Span<byte>(write_buffer_, (int)write_buffer_size_, entrySize);
        }

        public void AdvanceTo(ushort entrySize)
        {
            write_buffer_size_ += entrySize;
        }
    }
}