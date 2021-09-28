using System;
using System.Collections.Generic;
using System.Linq;

namespace Chiapos.Dotnet
{
    // Class to evaluate F2 .. F7.
    public class FxCalculator
    {
        private static object syncRoot = new();
        private static bool isInitialized;
        //instead of using 3-dimensional array we use one-dimensional
        //private static readonly ushort[,,] L_targets = new ushort[2, Constants.kBC, Constants.kExtraBitsPow];
        private static readonly ushort[] L_targets = new ushort[2 * Constants.kBC * Constants.kExtraBitsPow];
        const int shiftArray1 = Constants.kExtraBits;
        const int shiftArray2 = Constants.kExtraBits + 1; // 1 is power of 2 for parity dimension


        byte k_;
        byte table_index_;
        private List<rmap_item> rmap = Enumerable.Range(0, Constants.kBC).Select(_ => new rmap_item()).ToList();
        List<ushort> rmap_clean = new(10000);

        private class rmap_item
        {
            public ushort count; //: 4;
            public ushort pos; // : 12;
        }

        public FxCalculator()
        {
        }

        public FxCalculator(byte k, byte table_index)
        {
            this.k_ = k;
            this.table_index_ = table_index;

            load_tables();
        }

        void load_tables()
        {
            if (isInitialized) return;

            lock (syncRoot)
            {
                if (isInitialized) return;
                var span = L_targets.AsSpan(0, L_targets.Length);

                for (byte parity = 0; parity < 2; parity++)
                {
                    for (ushort i = 0; i < Constants.kBC; i++)
                    {
                        ushort indJ = (ushort) (i / Constants.kC);
                        for (ushort m = 0; m < Constants.kExtraBitsPow; m++)
                        {
                            ushort yr =
                                (ushort) (((indJ + m) % Constants.kB) * Constants.kC +
                                          (((2 * m + parity) * (2 * m + parity) + i) % Constants.kC));
                            span[(parity << shiftArray1) + (i << shiftArray2) + m] = yr;
                        }
                    }
                }

                isInitialized = true;
            }
        }

        public void ReloadKey()
        {
        }

        byte[] input_bytes = new byte[64];
        byte[] hash_bytes = new byte[32];
        // Performs one evaluation of the f function.
        public ValueTuple<Bits2, Bits2> CalculateBucket(ulong y1, int y1_bits, Bits2 L, Bits2 R)
        {
            using var hasher = Blake3.Hasher.New();
            ulong f;
            Bits2 c = null;

            if (table_index_ < 4)
            {
                c = L + R;
            }
            else
            {
                //just write data to input_bytes
            }
            
            //input = y1 + L + R
            int input_length = Bits2.WriteBytesToBuffer(input_bytes, 0, y1, y1_bits);
            input_length = Bits2.WriteBytesToBuffer(input_bytes, input_length, L.GetBuffer(), L.Length);
            input_length = Bits2.WriteBytesToBuffer(input_bytes, input_length, R.GetBuffer(), R.Length);

            //blake3_hasher_init(hasher);
            //blake3_hasher_update(hasher, input_bytes, Util.Cdiv(input.Length, 8));
            hasher.Update(new ReadOnlySpan<byte>(input_bytes, 0, Util.Cdiv(input_length, 8)));
            //blake3_hasher_finalize(hasher, hash_bytes, hash_bytes.Length * sizeof(byte));
            hasher.Finalize(hash_bytes);

            if (table_index_ < 4)
            {
                // c is already computed
            }
            else if (table_index_ < 7)
            {
                byte len = Constants.kVectorLens[table_index_ + 1];
                byte start_byte = (byte) ((k_ + Constants.kExtraBits) / 8);
                byte end_bit = (byte) (k_ + Constants.kExtraBits + k_ * len);
                byte end_byte = (byte) Util.Cdiv(end_bit, 8);

                c = new Bits2(new ReadOnlySpan<byte>(hash_bytes, start_byte, end_byte - start_byte),
                    (k_ + Constants.kExtraBits) % 8,
                    end_bit - start_byte * 8 - ((k_ + Constants.kExtraBits) % 8)
                    );
                    
            }

            return (new Bits2(hash_bytes, 0, k_ + Constants.kExtraBits), c);
        }

        // Given two buckets with entries (y values), computes which y values match, and returns a list
        // of the pairs of indices into bucket_L and bucket_R. Indices l and r match iff:
        //   let  yl = bucket_L[l].y,  yr = bucket_R[r].y
        //
        //   For any 0 <= m < kExtraBitsPow:
        //   yl / kBC + 1 = yR / kBC   AND
        //   (yr % kBC) / kC - (yl % kBC) / kC = m   (mod kB)  AND
        //   (yr % kBC) % kC - (yl % kBC) % kC = (2m + (yl/kBC) % 2)^2   (mod kC)
        //
        // Instead of doing the naive algorithm, which is an O(kExtraBitsPow * N^2) comparisons on
        // bucket length, we can store all the R values and lookup each of our 32 candidates to see if
        // any R value matches. This function can be further optimized by removing the inner loop, and
        // being more careful with memory allocation.
        public int FindMatches(
            List<PlotEntry> bucket_L,
            List<PlotEntry> bucket_R,
            ushort[] idx_L,
            ushort[] idx_R)
        {
            int idx_count = 0;
            ushort parity = (ushort) ((bucket_L[0].y / Constants.kBC) % 2);

            foreach (var yl in rmap_clean)
            {
                this.rmap[yl].count = 0;
            }

            rmap_clean.Clear();

            ulong remove = (bucket_R[0].y / Constants.kBC) * Constants.kBC;
            for (int pos_R = 0; pos_R < bucket_R.Count; pos_R++)
            {
                int r_y = (int) (bucket_R[pos_R].y - remove);

                var rmap_ry = rmap[r_y];
                
                if (rmap_ry.count == 0)
                {
                    rmap_ry.pos = (ushort) pos_R;
                }

                rmap_ry.count++;
                rmap_clean.Add((ushort) r_y);
            }

            var L_targets_span = L_targets.AsSpan(0, L_targets.Length);
            ulong remove_y = remove - Constants.kBC;
            for (ushort pos_L = 0; pos_L < bucket_L.Count; pos_L++)
            {
                ushort r = (ushort)(bucket_L[pos_L].y - remove_y);
                for (byte i = 0; i < Constants.kExtraBitsPow; i++)
                {
                    ushort r_target = L_targets_span[(parity << shiftArray1) + (r << shiftArray2) + i];
                    var rmap_target = rmap[r_target];
                    for (ushort j = 0; j < rmap_target.count; j++)
                    {
                        idx_L[idx_count] = pos_L;
                        idx_R[idx_count] = (ushort)(rmap_target.pos + j);

                        idx_count++;
                    }
                }
            }

            return idx_count;
        }
    }
}