using System;

namespace Chiapos.Dotnet.Disks
{
    public class FilteredDisk : IDisk
    {
        // only entries whose bit is set should be read
        private readonly Bitfield filter;
        private readonly BufferedDisk underlying;
        private readonly ulong entrySize;

        // the "physical" disk offset of the last read
        ulong last_physical_ = 0;
        // the "logical" disk offset of the last read. i.e. the offset as if the
        // file would have been compacted based on filter_
        ulong last_logical_ = 0;

        // the index of the last read. This is also the index into the bitfield. It
        // could be computed as last_physical_ / entry_size_, but we want to avoid
        // the division.
        ulong last_idx_ = 0;

        public FilteredDisk(BufferedDisk underlying, Bitfield filter, ulong entrySize)
        {
            this.filter = filter;
            this.underlying = underlying;
            this.entrySize = entrySize;
            
           //assert(entry_size_ > 0);
            while (!filter.Get(last_idx_)) {
                last_physical_ += entrySize;
                ++last_idx_;
            }
            //assert(filter_.get(last_idx_));
            //assert(last_physical_ == last_idx_ * entry_size_);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public ReadOnlySpan<byte> Read(ulong begin, ulong length)
        {
            // we only support a single read-pass with no going backwards
            //assert(begin >= last_logical_);
            //assert((begin % entry_size_) == 0);
            //assert(filter_.get(last_idx_));
            //assert(last_physical_ == last_idx_ * entry_size_);

            if (begin > last_logical_) {
                // last_idx_ et.al. always points to an entry we have (i.e. the bit
                // is set). So when we advance from there, we always take at least
                // one step on all counters.
                last_logical_ += entrySize;
                last_physical_ += entrySize;
                ++last_idx_;

                while (begin > last_logical_)
                {
                    if (filter.Get(last_idx_)) {
                        last_logical_ += entrySize;
                    }
                    last_physical_ += entrySize;
                    ++last_idx_;
                }

                while (!filter.Get(last_idx_)) {
                    last_physical_ += entrySize;
                    ++last_idx_;
                }
            }

            //assert(filter_.get(last_idx_));
            //assert(last_physical_ == last_idx_ * entry_size_);
            //assert(begin == last_logical_);
            return underlying.Read(last_physical_, length);
        }

        public void Write(ulong begin, ReadOnlySpan<byte> buffer)
        {
            //assert(false);
            throw new NotImplementedException("Write() called on read-only disk abstraction");
        }

        public void Truncate(ulong new_size)
        {
            underlying.Truncate(new_size);
            if (new_size == 0) filter.FreeMemory();
        }

        public void FreeMemory()
        {
            filter.FreeMemory();
            underlying.FreeMemory();
        }

        public string GetFileName() => underlying.GetFileName();
    }
}