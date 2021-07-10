using System;
using System.Collections.Generic;

namespace Chiapos.Dotnet
{
    public class BitfieldIndex
    {
        // Cache the number of set bits every kIndexBucket bits.
        // For a bitfield of size 2^32, this means a 32 MiB index
        public const ulong kIndexBucket = 1024;

        private Bitfield bitfield;
        private List<ulong> index;

        public BitfieldIndex(Bitfield bitfield)
        {
            this.bitfield = bitfield;
            ulong counter = 0;
            index = new List<ulong>((int) (bitfield.Length / kIndexBucket));
            
            for (ulong idx = 0; idx < bitfield.Length; idx += kIndexBucket) {
                index.Add(counter);
                ulong left = Math.Min(bitfield.Length - idx, kIndexBucket);
                counter += bitfield.Count(idx, idx + left);
            }
        }
        
        public ValueTuple<ulong,ulong> Lookup(ulong pos, ulong offset)
        {
            ulong bucket = pos / kIndexBucket;

            //assert(bucket < index_.size());
            //assert(pos < uint64_t(bitfield_.size()));
            //assert(pos + offset < uint64_t(bitfield_.size()));
            //assert(bitfield_.get(pos) && bitfield_.get(pos + offset));

            ulong baseIndex = index[(int)bucket];

            ulong aligned_pos = pos & ~63U;

            ulong aligned_pos_count = bitfield.Count(bucket * kIndexBucket, aligned_pos);
            ulong offset_count = aligned_pos_count + bitfield.Count(aligned_pos, pos + offset);
            ulong pos_count = aligned_pos_count + bitfield.Count(aligned_pos, pos);

            //assert(offset_count >= pos_count);

            return (baseIndex + pos_count, offset_count - pos_count );
        }

    }
}
