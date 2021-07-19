using System;
using System.Collections.Generic;
using System.IO;
using Chiapos.Dotnet.Collections;
using Chiapos.Dotnet.Disks;

namespace Chiapos.Dotnet
{
    public class SortManager : IDisk
    {
        // The buffer we use to sort buckets in-memory
        byte[] memory_start_;

        // Size of the whole memory array
        ulong memory_size_;

        // Size of each entry
        ushort entry_size_;

        // Bucket determined by the first "log_num_buckets" bits starting at "begin_bits"
        uint begin_bits_;

        // Log of the number of buckets; num bits to use to determine bucket
        uint log_num_buckets_;

        List<bucket_t> buckets_;

        ulong prev_bucket_buf_size;
        byte[] prev_bucket_buf_;
        ulong prev_bucket_position_start = 0;

        bool done = false;

        ulong final_position_start = 0;
        ulong final_position_end = 0;
        ulong next_bucket_to_sort = 0;
        byte[] entry_buf_;
        SortStrategy strategy_;

        public SortManager(
            ulong memory_size,
            uint num_buckets,
            uint log_num_buckets,
            ushort entry_size,
            string tmp_dirname,
            string filename,
            uint begin_bits,
            ulong stripe_size,
            SortStrategy sort_strategy = SortStrategy.Uniform)
        {
            this.memory_size_ = memory_size;
            this.entry_size_ = entry_size;
            this.begin_bits_ = begin_bits;
            this.log_num_buckets_ = log_num_buckets;
            this.prev_bucket_buf_size = 2 * (stripe_size + 10 * (Constants.kBC / (ulong)Math.Pow(2, Constants.kExtraBits))) * entry_size;
            // 7 bytes head-room for SliceInt64FromBytes()
            this.entry_buf_ = new byte[entry_size + 7];
            this.strategy_ = sort_strategy;


            // Cross platform way to concatenate paths, gulrak library.
            var bucket_filenames = new List<string>();

            buckets_ = new List<bucket_t>((int) num_buckets);
            for (int bucket_i = 0; bucket_i < num_buckets; bucket_i++)
            {
                string bucket_filename = Path.Combine(tmp_dirname, $"{filename} .sort_bucket_{bucket_i:000}.tmp");
                File.Delete(bucket_filename);
                buckets_.Add(new bucket_t(new FileDisk(bucket_filename)));
            }
        }

        public void AddToCache(BitArray entry)
        {
            entry.CopyTo(entry_buf_, 0);
            AddToCache(entry_buf_);
        }

        public void AddToCache(ReadOnlySpan<byte> entry)
        {
            if (this.done) {
                throw new ArgumentException("Already finished.");
            }
            ulong bucket_index = Util.ExtractNum(entry, entry_size_, begin_bits_, log_num_buckets_);
            bucket_t b = buckets_[(int)bucket_index];
            b.file.Write(b.write_pointer, entry.Slice(0, entry_size_));  //Can entry be other length that entry size?
            b.write_pointer += entry_size_;
        }


        internal struct bucket_t
        {
            internal bucket_t(FileDisk f)
            {
                this.underlying_file = f;
                this.file = new BufferedDisk(this.underlying_file, 0);
                this.write_pointer = 0;
            }

            // The amount of data written to the disk bucket
            public ulong write_pointer;

            // The file for the bucket
            public FileDisk underlying_file;
            public BufferedDisk file;
        }

        public ReadOnlySpan<byte> Read(ulong begin, ulong length)
        {
            //assert(length <= entry_size_);
            return ReadEntry(begin);
        }
        
        public void Write(ulong begin, ReadOnlySpan<byte> buffer)
        {
            //assert(false);
            throw new NotImplementedException("Invalid Write() called on SortManager");
        }

        public ReadOnlySpan<byte> ReadEntry(ulong position)
        {
            if (position < this.final_position_start) {
                if (!(position >= this.prev_bucket_position_start)) {
                    throw new InvalidDataException("Invalid prev bucket start");
                }
                // this is allocated lazily, make sure it's here
                //assert(prev_bucket_buf_);
                return new ReadOnlySpan<byte>(prev_bucket_buf_,  (int)(position - prev_bucket_position_start), 
                    prev_bucket_buf_.Length - (int)(position - prev_bucket_position_start));
            }

            while (position >= final_position_end) {
                SortBucket();
            }
            if (!(final_position_end > position)) {
                throw new InvalidDataException("Position too large");
            }
            if (!(final_position_start <= position)) {
                throw new InvalidDataException("Position too small");
            }
            //assert(memory_start_);
            
            return new ReadOnlySpan<byte>(memory_start_ , (int)(position - final_position_start),
                memory_start_.Length - (int)(position - final_position_start));
        }
        
        public bool CloseToNewBucket(ulong position)
        {
            if (!(position <= final_position_end)) {
                return next_bucket_to_sort < (ulong)buckets_.Count;
            };
            return (
                position + prev_bucket_buf_size / 2 >= final_position_end &&
                next_bucket_to_sort < (ulong)buckets_.Count);
        }
        
        public void TriggerNewBucket(ulong position)
        {
            if (!(position <= final_position_end)) {
                throw new InvalidDataException("Triggering bucket too late");
            }
            if (!(position >= final_position_start)) {
                throw new InvalidDataException("Triggering bucket too early");
            }

            if (memory_start_ != null) {
                // save some of the current bucket, to allow some reverse-tracking
                // in the reading pattern,
                // position is the first position that we need in the new array
                ulong cache_size = final_position_end - position;
                prev_bucket_buf_ = new byte[prev_bucket_buf_size];
                Array.Clear(prev_bucket_buf_, (int)cache_size, (int)(prev_bucket_buf_size - cache_size));
                Buffer.BlockCopy(memory_start_, (int)(position - final_position_start), 
                    prev_bucket_buf_,0, (int)cache_size);
            }

            SortBucket();
            prev_bucket_position_start = position;
        }

        void SortBucket()
        {
            if (memory_start_ != null)
            {
                // we allocate the memory to sort the bucket in lazily. It'se freed
                // in FreeMemory() or the destructor
                memory_start_ = new byte[memory_size_];
            }

            done = true;
            if (next_bucket_to_sort >= (ulong) buckets_.Count)
            {
                throw new InvalidDataException("Trying to sort bucket which does not exist.");
            }

            int bucket_i = (int) next_bucket_to_sort;
            bucket_t b = buckets_[bucket_i];
            ulong bucket_entries = b.write_pointer / entry_size_;
            ulong entries_fit_in_memory = memory_size_ / entry_size_;

            double have_ram = entry_size_ * entries_fit_in_memory / (1024.0 * 1024.0 * 1024.0);
            double qs_ram = entry_size_ * bucket_entries / (1024.0 * 1024.0 * 1024.0);
            double u_ram = Util.RoundSize(bucket_entries) * entry_size_ / (1024.0 * 1024.0 * 1024.0);

            if (bucket_entries > entries_fit_in_memory)
            {
                throw new InsufficientMemoryException(
                    "Not enough memory for sort in memory. Need to sort " +
                    $"{b.write_pointer / (1024.0 * 1024.0 * 1024.0)} GiB");
            }

            bool last_bucket = (bucket_i == buckets_.Count - 1) || buckets_[bucket_i + 1].write_pointer == 0;

            bool force_quicksort = (strategy_ == SortStrategy.Quicksort)
                                   || (strategy_ == SortStrategy.QuicksortLast && last_bucket);

            // Do SortInMemory algorithm if it fits in the memory
            // (number of entries required * entry_size_) <= total memory available
            if (!force_quicksort &&
                Util.RoundSize(bucket_entries) * entry_size_ <= memory_size_)
            {
                Console.WriteLine($"\tBucket {bucket_i} uniform sort. Ram: {have_ram:F3} GiB, qs_min: {qs_ram:F3}GiB.");
                UniformSort.SortToMemory(
                    b.underlying_file,
                    0,
                    memory_start_,
                    entry_size_,
                    bucket_entries,
                    (int)(begin_bits_ + log_num_buckets_));
            }
            else
            {
                // Are we in Compress phrase 1 (quicksort=1) or is it the last bucket (quicksort=2)?
                // Perform quicksort if so (SortInMemory algorithm won't always perform well), or if we
                // don't have enough memory for uniform sort
                Console.WriteLine($"\tBucket {bucket_i} QS. Ram: {have_ram:F3} GiB, u_sort min: {u_ram:F3}GiB, " +
                                  $"qs min: {qs_ram:F3} GiB. force_qs: {force_quicksort}");

                b.underlying_file.Read(0, memory_start_, 0, bucket_entries * entry_size_);
                QuickSort.Sort(memory_start_, entry_size_, bucket_entries, begin_bits_ + log_num_buckets_);
            }

            // Deletes the bucket file
            string filename = b.file.GetFileName();
            b.underlying_file.Close();
            File.Delete(filename);

            final_position_start = final_position_end;
            final_position_end += b.write_pointer;
            next_bucket_to_sort += 1;
        }


        public void FlushCache()
        {
            foreach (var bucket in buckets_)
            {
                bucket.file.FlushCache();
            }

            final_position_end = 0;
            memory_start_ = null;
        }
        
        public void Dispose()
        {
            // Close and delete files in case we exit without doing the sort
            foreach (var b in buckets_) {
                var filename = b.file.GetFileName();
                b.underlying_file.Close();
                File.Delete(filename);
            }
        }


        public void Truncate(ulong new_size)
        {
            if (new_size != 0) {
                //assert(false);
                throw new ArgumentException($"{nameof(new_size)} must be 0");
            }

            FlushCache();
            FreeMemory();
        }
        
        public void FreeMemory()
        {
            foreach (var b in buckets_) {
                b.file.FreeMemory();
                // the underlying file will be re-opened again on-demand
                b.underlying_file.Close();
            }
            prev_bucket_buf_ = null;
            memory_start_ = null;
            final_position_end = 0;
            // TODO: Ideally, bucket files should be deleted as we read them (in the
            // last reading pass over them)
        }


        public string GetFileName() => "<SortManager>";

        public void Reset()
        {
        }
    }
}