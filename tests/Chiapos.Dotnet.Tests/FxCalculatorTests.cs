namespace Chiapos.Dotnet.Tests
{
    public class FxCalculatorTests
    {
        /*
TEST_CASE("F functions")
{
    SECTION("F1")
    {
        uint8_t test_k = 35;
        uint8_t test_key[] = {0, 2, 3, 4,  5, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                              1, 2, 3, 41, 5, 6, 7, 8, 9, 10, 11, 12, 13, 11, 15, 16};
        F1Calculator f1(test_k, test_key);

        Bits L = Bits(525, test_k);
        pair<Bits, Bits> result1 = f1.CalculateBucket(L);
        Bits L2 = Bits(526, test_k);
        pair<Bits, Bits> result2 = f1.CalculateBucket(L2);
        Bits L3 = Bits(625, test_k);
        pair<Bits, Bits> result3 = f1.CalculateBucket(L3);

        uint64_t results[256];
        f1.CalculateBuckets(L.GetValue(), 101, results);
        REQUIRE(result1.first.GetValue() == results[0]);
        REQUIRE(result2.first.GetValue() == results[1]);
        REQUIRE(result3.first.GetValue() == results[100]);

        uint32_t max_batch = 1 << kBatchSizes;
        test_k = 32;
        F1Calculator f1_2(test_k, test_key);
        L = Bits(192837491, test_k);
        result1 = f1_2.CalculateBucket(L);
        L2 = Bits(192837491 + 1, test_k);
        result2 = f1_2.CalculateBucket(L2);
        L3 = Bits(192837491 + 2, test_k);
        result3 = f1_2.CalculateBucket(L3);
        Bits L4 = Bits(192837491 + max_batch - 1, test_k);
        pair<Bits, Bits> result4 = f1_2.CalculateBucket(L4);

        f1_2.CalculateBuckets(L.GetValue(), max_batch, results);
        REQUIRE(result1.first.GetValue() == results[0]);
        REQUIRE(result2.first.GetValue() == results[1]);
        REQUIRE(result3.first.GetValue() == results[2]);
        REQUIRE(result4.first.GetValue() == results[max_batch - 1]);
    }

    SECTION("F2")
    {
        uint8_t test_key_2[] = {20,  2,  5,  4,   51, 52,  23,  84,  91, 10, 111,
                                12,  13, 24, 151, 16, 228, 211, 254, 45, 92, 198,
                                204, 10, 9,  10,  11, 129, 139, 171, 15, 18};
        map<uint64_t, vector<pair<Bits, Bits>>> buckets;

        uint8_t const k = 12;
        uint64_t num_buckets = (1ULL << (k + kExtraBits)) / kBC + 1;
        uint64_t x = 0;

        F1Calculator f1(k, test_key_2);
        for (uint32_t j = 0; j < (1ULL << (k - 4)) + 1; j++) {
            uint64_t y[1 << 4];

            f1.CalculateBuckets(x, 1U << 4, y);
            for (int i = 0; i < 1 << 4; i++) {
                uint64_t bucket = y[i] / kBC;
                if (buckets.find(bucket) == buckets.end()) {
                    buckets[bucket] = vector<std::pair<Bits, Bits>>();
                }
                buckets[bucket].push_back(std::make_pair(Bits(y[i], k + kExtraBits), Bits(x, k)));
                if (x + 1 > (1ULL << k) - 1) {
                    break;
                }
                ++x;
            }
            if (x + 1 > (1ULL << k) - 1) {
                break;
            }
        }

        FxCalculator f2(k, 2);
        int total_matches = 0;

        for (auto kv : buckets) {
            if (kv.first == num_buckets - 1) {
                continue;
            }
            auto bucket_elements_2 = buckets[kv.first + 1];
            vector<PlotEntry> left_bucket;
            vector<PlotEntry> right_bucket;
            for (auto yx1 : kv.second) {
                PlotEntry e;
                e.y = get<0>(yx1).GetValue();
                left_bucket.push_back(e);
            }
            for (auto yx2 : buckets[kv.first + 1]) {
                PlotEntry e;
                e.y = get<0>(yx2).GetValue();
                right_bucket.push_back(e);
            }
            sort(
                left_bucket.begin(),
                left_bucket.end(),
                [](const PlotEntry& a, const PlotEntry& b) -> bool { return a.y > b.y; });
            sort(
                right_bucket.begin(),
                right_bucket.end(),
                [](const PlotEntry& a, const PlotEntry& b) -> bool { return a.y > b.y; });

            uint16_t idx_L[10000];
            uint16_t idx_R[10000];

            int32_t idx_count = f2.FindMatches(left_bucket, right_bucket, idx_L, idx_R);
            for(int32_t i=0; i < idx_count; i++) {
                REQUIRE(CheckMatch(left_bucket[idx_L[i]].y, right_bucket[idx_R[i]].y));
            }
            total_matches += idx_count;
        }
        REQUIRE(total_matches > (1 << k) / 2);
        REQUIRE(total_matches < (1 << k) * 2);
    }

    SECTION("Fx")
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
}
         
         */
    }
}