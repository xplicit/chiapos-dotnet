namespace Chiapos.Dotnet
{
    public class Constants
    {
        // Unique plot id which will be used as a ChaCha8 key, and determines the PoSpace.
        public const uint kIdLen = 32;

        // Distance between matching entries is stored in the offset
        public const uint kOffsetSize = 10;

        // Max matches a single entry can have, used for hardcoded memory allocation
        public const uint kMaxMatchesSingleEntry = 30;
        public const uint kMinBuckets = 16;
        public const uint kMaxBuckets = 256;

        // During backprop and compress, the write pointer is ahead of the read pointer
        // Note that the large the offset, the higher these values must be
        public const uint kReadMinusWrite = 1U << (int)kOffsetSize;
        public const uint kCachedPositionsSize = kReadMinusWrite * 4;

        // Must be set high enough to prevent attacks of fast plotting
        public const uint kMinPlotSize = 18;

        // Set to 50 since k + kExtraBits + k*4 must not exceed 256 (BLAKE3 output size)
        public const uint kMaxPlotSize = 50;

        // The amount of spare space used for sort on disk (multiplied time memory buffer size)
        public const uint kSpareMultiplier = 5;

        // The proportion of memory to allocate to the Sort Manager for reading in buckets and sorting them
        // The lower this number, the more memory must be provided by the caller. However, lowering the
        // number also allows a higher proportion for writing, which reduces seeks for HDD.
        public const double kMemSortProportion = 0.75;
        public const double kMemSortProportionLinePoint = 0.85;

        // How many f7s per C1 entry, and how many C1 entries per C2 entry
        public const uint kCheckpoint1Interval = 10000;
        public const uint kCheckpoint2Interval = 10000;

        // F1 evaluations are done in batches of 2^kBatchSizes
        public const uint kBatchSizes = 8;

        // EPP for the final file, the higher this is, the less variability, and lower delta
        // Note: if this is increased, ParkVector size must increase
        public const uint kEntriesPerPark = 2048;

        // To store deltas for EPP entries, the average delta must be less than this number of bits
        public const double kMaxAverageDeltaTable1 = 5.6;
        public const double kMaxAverageDelta = 3.5;

        // C3 entries contain deltas for f7 values, the max average size is the following
        public const double kC3BitsPerEntry = 2.4;

        // The number of bits in the stub is k minus this value
        public const byte kStubMinusBits = 3;

        // The ANS encoding R values for the 7 final plot tables
        // Tweaking the R values might allow lowering of the max average deltas, and reducing final
        // plot size
        public static readonly double[] kRValues = new []{4.7, 2.75, 2.75, 2.7, 2.6, 2.45};

        // The ANS encoding R value for the C3 checkpoint table
        public const double kC3R = 1.0;

        // Plot format (no compatibility guarantees with other formats). If any of the
        // above contants are changed, or file format is changed, the version should
        // be incremented.
        public const string kFormatDescription = "v1.0";
        
        // ChaCha8 block size
        public const ushort kF1BlockSizeBits = 512;

        // Extra bits of output from the f functions. Instead of being a function from k -> k bits,
        // it's a function from k -> k + kExtraBits bits. This allows less collisions in matches.
        // Refer to the paper for mathematical motivations.
        public const byte kExtraBits = 6;

        // Convenience variable
        public const byte kExtraBitsPow = 1 << kExtraBits;

        // B and C groups which constitute a bucket, or BC group. These groups determine how
        // elements match with each other. Two elements must be in adjacent buckets to match.
        public const ushort kB = 119;
        public const ushort kC = 127;
        public const ushort kBC = kB * kC;

        // This (times k) is the length of the metadata that must be kept for each entry. For example,
        // for a table 4 entry, we must keep 4k additional bits for each entry, which is used to
        // compute f5.
        public static readonly byte[] kVectorLens = {0, 0, 1, 2, 4, 4, 3, 2};
    }
}