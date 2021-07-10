using System;

namespace Chiapos.Dotnet
{
public class EntrySizes {
    public static int GetMaxEntrySize(byte k, byte table_index, bool phase_1_size)
    {
        // This represents the largest entry size that each table will have, throughout the
        // entire plotting process. This is useful because it allows us to rewrite tables
        // on top of themselves without running out of space.
        switch (table_index) {
            case 1:
                // Represents f1, x
                if (phase_1_size) {
                    return Util.ByteAlign(k + Constants.kExtraBits + k) / 8;
                } else {
                    // After computing matches, table 1 is rewritten without the f1, which
                    // is useless after phase1.
                    return Util.ByteAlign(k) / 8;
                }
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
                if (phase_1_size)
                    // If we are in phase 1, use the max size, with metadata.
                    // Represents f, pos, offset, and metadata
                    return Util.ByteAlign(
                               k + Constants.kExtraBits + (k) + (int)Constants.kOffsetSize +
                               k * Constants.kVectorLens[table_index + 1]) 
                           / 8;
                else
                    // If we are past phase 1, we can use a smaller size, the smaller between
                    // phases 2 and 3. Represents either:
                    //    a:  sort_key, pos, offset        or
                    //    b:  line_point, sort_key
                    return Util.ByteAlign(Math.Max(2 * k + (int)Constants.kOffsetSize, 3 * k - 1)) / 8;
            case 7:
            default:
                // Represents line_point, f7
                return Util.ByteAlign(3 * k - 1) / 8;
        }
    }

    // Get size of entries containing (sort_key, pos, offset). Such entries are
    // written to table 7 in phase 1 and to tables 2-7 in phase 2.
    public static int GetKeyPosOffsetSize(byte k)
    {
        return Util.Cdiv(2 * k + (int)Constants.kOffsetSize, 8);
    }

    // Calculates the size of one C3 park. This will store bits for each f7 between
    // two C1 checkpoints, depending on how many times that f7 is present. For low
    // values of k, we need extra space to account for the additional variability.
    public static int CalculateC3Size(byte k)
    {
        if (k < 20) {
            return Util.ByteAlign(8 * (int)Constants.kCheckpoint1Interval) / 8;
        } else {
            return Util.ByteAlign((int)(Constants.kC3BitsPerEntry * Constants.kCheckpoint1Interval)) / 8;
        }
    }

    public static int CalculateLinePointSize(byte k) { return Util.ByteAlign(2 * k) / 8; }

    // This is the full size of the deltas section in a park. However, it will not be fully filled
    public static int CalculateMaxDeltasSize(byte k, byte table_index)
    {
        if (table_index == 1) {
            return Util.ByteAlign((int)((Constants.kEntriesPerPark - 1) * Constants.kMaxAverageDeltaTable1)) / 8;
        }
        return Util.ByteAlign((int)((Constants.kEntriesPerPark - 1) * Constants.kMaxAverageDelta)) / 8;
    }

    public static int CalculateStubsSize(int k)
    {
        return Util.ByteAlign(((int)Constants.kEntriesPerPark - 1) * (k - Constants.kStubMinusBits)) / 8;
    }

    public static int CalculateParkSize(byte k, byte table_index)
    {
        return CalculateLinePointSize(k) + CalculateStubsSize(k) +
               CalculateMaxDeltasSize(k, table_index);
    }
};
}