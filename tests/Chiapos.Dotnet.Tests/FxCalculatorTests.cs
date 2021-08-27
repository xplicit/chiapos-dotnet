using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    [TestFixture]
    public class FxCalculatorTests
    {
        [Test]
        public void F1Calculator_GeneratesBits()
        {
            byte test_k = 35;
            var test_key = new byte[]
            {
                0, 2, 3, 4, 5, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                1, 2, 3, 41, 5, 6, 7, 8, 9, 10, 11, 12, 13, 11, 15, 16
            };
            F1Calculator f1 = new F1Calculator(test_k, test_key);

            Bits L = new Bits(525, test_k);
            (Bits, Bits) result1 = f1.CalculateBucket(L);
            Assert.That(result1.Item1.GetValue(), Is.EqualTo(0xdcecf7f100));

            Bits L2 = new Bits(526, test_k);
            (Bits, Bits) result2 = f1.CalculateBucket(L2);
            Assert.That(result2.Item1.GetValue(), Is.EqualTo(0x1e9131a8340));

            Bits L3 = new Bits(625, test_k);
            (Bits, Bits) result3 = f1.CalculateBucket(L3);
            Assert.That(result3.Item1.GetValue(), Is.EqualTo(0x152d2a7cc40));

            var results = new ulong[256];
            f1.CalculateBuckets(L.GetValue(), 101, results);
            Assert.That(result1.Item1.GetValue(), Is.EqualTo(results[0]));
            Assert.That(result2.Item1.GetValue(), Is.EqualTo(results[1]));
            Assert.That(result3.Item1.GetValue(), Is.EqualTo(results[100]));

            uint max_batch = 1 << (int) Constants.kBatchSizes;
            test_k = 32;
            F1Calculator f1_2 = new F1Calculator(test_k, test_key);
            L = new Bits(192837491, test_k);
            result1 = f1_2.CalculateBucket(L);
            L2 = new Bits(192837491 + 1, test_k);
            result2 = f1_2.CalculateBucket(L2);
            L3 = new Bits(192837491 + 2, test_k);
            result3 = f1_2.CalculateBucket(L3);
            Bits L4 = new Bits(192837491 + max_batch - 1, test_k);
            (Bits, Bits) result4 = f1_2.CalculateBucket(L4);

            f1_2.CalculateBuckets(L.GetValue(), max_batch, results);
            Assert.That(result1.Item1.GetValue(), Is.EqualTo(results[0]));
            Assert.That(result2.Item1.GetValue(), Is.EqualTo(results[1]));
            Assert.That(result3.Item1.GetValue(), Is.EqualTo(results[2]));
            Assert.That(result4.Item1.GetValue(), Is.EqualTo(results[max_batch - 1]));

        }


        [Test]
        public void FxCulculator_Calculates()
        {
            byte[] test_key_2 =
            {
                20, 2, 5, 4, 51, 52, 23, 84, 91, 10, 111,
                12, 13, 24, 151, 16, 228, 211, 254, 45, 92, 198,
                204, 10, 9, 10, 11, 129, 139, 171, 15, 18
            };
            SortedDictionary<ulong, List<ValueTuple<Bits, Bits>>> buckets = new();

            byte k = 12;
            ulong num_buckets = (1UL << (k + Constants.kExtraBits)) / Constants.kBC + 1;
            ulong x = 0;

            var f1 = new F1Calculator(k, test_key_2);
            for (uint j = 0; j < (1UL << (k - 4)) + 1; j++)
            {
                ulong[] y = new ulong[1 << 4];

                f1.CalculateBuckets(x, 1U << 4, y);
                for (int i = 0; i < 1 << 4; i++)
                {
                    ulong bucket = y[i] / Constants.kBC;
                    if (!buckets.ContainsKey(bucket))
                    {
                        buckets.Add(bucket, new List<(Bits, Bits)>());
                    }

                    buckets[bucket].Add((new Bits(y[i], k + Constants.kExtraBits), new Bits(x, k)));
                    if (x + 1 > (1UL << k) - 1)
                    {
                        break;
                    }

                    ++x;
                }

                if (x + 1 > (1UL << k) - 1)
                {
                    break;
                }
            }

            var f2 = new FxCalculator(k, 2);
            int total_matches = 0;

            foreach (var kv in buckets)
            {
                if (kv.Key == num_buckets - 1)
                {
                    continue;
                }

                var bucket_elements_2 = buckets[kv.Key + 1];
                List<PlotEntry> left_bucket = new();
                List<PlotEntry> right_bucket = new();
                foreach (var yx1 in kv.Value)
                {
                    PlotEntry e = new();
                    e.y = yx1.Item1.GetValue();
                    left_bucket.Add(e);
                }

                foreach (var yx2 in buckets[kv.Key + 1])
                {
                    PlotEntry e = new();
                    e.y = yx2.Item1.GetValue();
                    right_bucket.Add(e);
                }

                left_bucket.Sort((a, b) => b.y.CompareTo(a.y));
                right_bucket.Sort((a, b) => b.y.CompareTo(a.y));

                var idx_L = new ushort[10000];
                var idx_R = new ushort[10000];

                int idx_count = f2.FindMatches(left_bucket, right_bucket, idx_L, idx_R);
                for (int i = 0; i < idx_count; i++)
                {
                    Assert.That(CheckMatch((long)left_bucket[idx_L[i]].y, (long)right_bucket[idx_R[i]].y), Is.True);
                }

                total_matches += idx_count;
            }

            Assert.That(total_matches, Is.GreaterThan((1 << k) / 2));
            Assert.That(total_matches, Is.LessThan((1 << k) * 2));
        }

        [Test]
        public void FxCalculator_VerifyFC()
        {
            VerifyFC(2, 16, 0x44cb, 0x204f, 0x20a61a, 0x2af546, 0x44cb204f);
            VerifyFC(2, 16, 0x3c5f, 0xfda9, 0x3988ec, 0x15293b, 0x3c5ffda9);
            VerifyFC(3, 16, 0x35bf992d, 0x7ce42c82, 0x31e541, 0xf73b3, 0x35bf992d7ce42c82);
            VerifyFC(3, 16, 0x7204e52d, 0xf1fd42a2, 0x28a188, 0x3fb0b5, 0x7204e52df1fd42a2);
            VerifyFC(
                4, 16, 0x5b6e6e307d4bedc, 0x8a9a021ea648a7dd, 0x30cb4c, 0x11ad5, 0xd4bd0b144fc26138);
            VerifyFC(
                4, 16, 0xb9d179e06c0fd4f5, 0xf06d3fef701966a0, 0x1dd5b6, 0xe69a2, 0xd02115f512009d4d);
            VerifyFC(5, 16, 0xc2cd789a380208a9, 0x19999e3fa46d6753, 0x25f01e, 0x1f22bd, 0xabe423040a33);
            VerifyFC(5, 16, 0xbe3edc0a1ef2a4f0, 0x4da98f1d3099fdf5, 0x3feb18, 0x31501e, 0x7300a3a03ac5);
            VerifyFC(6, 16, 0xc965815a47c5, 0xf5e008d6af57, 0x1f121a, 0x1cabbe, 0xc8cc6947);
            VerifyFC(6, 16, 0xd420677f6cbd, 0x5894aa2ca1af, 0x2efde9, 0xc2121, 0x421bb8ec);
            VerifyFC(7, 16, 0x5fec898f, 0x82283d15, 0x14f410, 0x24c3c2, 0x0);
            VerifyFC(7, 16, 0x64ac5db9, 0x7923986, 0x590fd, 0x1c74a2, 0x0);
        }

        void VerifyFC(byte t, byte k, ulong L, ulong R, ulong y1, ulong y, ulong c)
        {
            byte[] sizes = {1, 2, 4, 4, 3, 2};
            byte size = sizes[t - 2];
            FxCalculator fcalc = new FxCalculator(k, t);

            var res = fcalc.CalculateBucket(
                new Bits(y1, k + Constants.kExtraBits), new Bits(L, k * size), new Bits(R, k * size));
            Assert.That(res.Item1.GetValue() == y);
            if (c != 0)
            {
                Assert.That(res.Item2.GetValue() == c);
            }
        }


        bool CheckMatch(long yl, long yr)
        {
            long kBC = Constants.kBC;
            long kB = Constants.kB;
            long kC = Constants.kC;
            
            long bl = yl / kBC;
            long br = yr / kBC;
            if (bl + 1 != br)
                return false;  // Buckets don't match
            for (long m = 0; m < Constants.kExtraBitsPow; m++) {
                if ((((yr % kBC) / kC - ((yl % kBC) / kC)) - m) % kB == 0) {
                    long c_diff = 2 * m + bl % 2;
                    c_diff *= c_diff;

                    if ((((yr % kBC) % kC - ((yl % kBC) % kC)) - c_diff) % kC == 0) {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}