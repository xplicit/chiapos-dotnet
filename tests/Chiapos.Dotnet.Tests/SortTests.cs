namespace Chiapos.Dotnet.Tests
{
    public class SortTests
    {
        /*
         TEST_CASE("Sort on disk")
{
    SECTION("ExtractNum")
    {
        for (int i = 0; i < 15 * 8 - 5; i++) {
            uint8_t buf[15 + 7];
            Bits((uint128_t)27 << i, 15 * 8).ToBytes(buf);

            REQUIRE(Util::ExtractNum(buf, 15, 15 * 8 - 4 - i, 3) == 5);
        }
        uint8_t buf[16 + 7];
        Bits((uint128_t)27 << 5, 128).ToBytes(buf);
        REQUIRE(Util::ExtractNum(buf, 16, 100, 200) == 864);
    }

    SECTION("MemCmpBits")
    {
        uint8_t left[3];
        left[0] = 12;
        left[1] = 10;
        left[2] = 100;

        uint8_t right[3];
        right[0] = 12;
        right[1] = 10;
        right[2] = 100;

        REQUIRE(Util::MemCmpBits(left, right, 3, 0) == 0);
        REQUIRE(Util::MemCmpBits(left, right, 3, 10) == 0);

        right[1] = 11;
        REQUIRE(Util::MemCmpBits(left, right, 3, 0) < 0);
        REQUIRE(Util::MemCmpBits(left, right, 3, 16) == 0);

        right[1] = 9;
        REQUIRE(Util::MemCmpBits(left, right, 3, 0) > 0);

        right[1] = 10;

        // Last bit differs
        right[2] = 101;
        REQUIRE(Util::MemCmpBits(left, right, 3, 0) < 0);
    }

    SECTION("Quicksort")
    {
        uint32_t const iters = 100;
        vector<string> hashes;
        uint8_t* hashes_bytes = new uint8_t[iters * 16];
        memset(hashes_bytes, 0, iters * 16);

        srand(0);
        for (uint32_t i = 0; i < iters; i++) {
            // reverting to rand()
            string to_insert = std::to_string(rand());
            while (to_insert.length() < 16) {
                to_insert += "0";
            }
            hashes.push_back(to_insert);
            memcpy(hashes_bytes + i * 16, to_insert.data(), to_insert.length());
        }
        sort(hashes.begin(), hashes.end());
        QuickSort::Sort(hashes_bytes, 16, iters, 0);

        for (uint32_t i = 0; i < iters; i++) {
            std::string str(reinterpret_cast<char*>(hashes_bytes) + i * 16, 16);
            REQUIRE(str.compare(hashes[i]) == 0);
        }
        delete[] hashes_bytes;
    }

    SECTION("File disk")
    {
        FileDisk d = FileDisk("test_file.bin");
        uint8_t buf[5] = {1, 2, 3, 5, 7};
        d.Write(250, buf, 5);

        uint8_t read_buf[5];
        d.Read(250, read_buf, 5);

        REQUIRE(memcmp(buf, read_buf, 5) == 0);
        remove("test_file.bin");
    }

    SECTION("Lazy Sort Manager QS")
    {
        uint32_t iters = 250000;
        uint32_t const size = 32;
        vector<Bits> input;
        const uint32_t memory_len = 1000000;
        SortManager manager(memory_len, 16, 4, size, ".", "test-files", 0, 1);
        int total_written_1 = 0;
        for (uint32_t i = 0; i < iters; i++) {
            vector<unsigned char> hash_input = intToBytes(i, 4);
            vector<unsigned char> hash(picosha2::k_digest_size);
            picosha2::hash256(hash_input.begin(), hash_input.end(), hash.begin(), hash.end());
            total_written_1 += size;
            Bits to_write = Bits(hash.data(), size, size * 8);
            input.emplace_back(to_write);
            manager.AddToCache(to_write);
        }
        manager.FlushCache();
        uint8_t buf[size];
        sort(input.begin(), input.end());
        uint8_t* buf3;
        for (uint32_t i = 0; i < iters; i++) {
            buf3 = manager.ReadEntry(i * size);
            input[i].ToBytes(buf);
            REQUIRE(memcmp(buf, buf3, size) == 0);
        }
    }

    SECTION("Lazy Sort Manager uniform sort")
    {
        uint32_t iters = 120000;
        uint32_t const size = 32;
        vector<Bits> input;
        const uint32_t memory_len = 1000000;
        SortManager manager(memory_len, 16, 4, size, ".", "test-files", 0, 1);
        int total_written_1 = 0;
        for (uint32_t i = 0; i < iters; i++) {
            vector<unsigned char> hash_input = intToBytes(i, 4);
            vector<unsigned char> hash(picosha2::k_digest_size);
            picosha2::hash256(hash_input.begin(), hash_input.end(), hash.begin(), hash.end());
            total_written_1 += size;
            Bits to_write = Bits(hash.data(), size, size * 8);
            input.emplace_back(to_write);
            manager.AddToCache(to_write);
        }
        manager.FlushCache();
        uint8_t buf[size];
        sort(input.begin(), input.end());
        uint8_t* buf3;
        for (uint32_t i = 0; i < iters; i++) {
            buf3 = manager.ReadEntry(i * size);
            input[i].ToBytes(buf);
            REQUIRE(memcmp(buf, buf3, size) == 0);
        }
    }

    SECTION("Sort in Memory")
    {
        uint32_t iters = 100000;
        uint32_t const size = 32;
        vector<Bits> input;
        uint32_t begin = 1000;
        FileDisk disk("test_file.bin");

        for (uint32_t i = 0; i < iters; i++) {
            vector<unsigned char> hash_input = intToBytes(i, 4);
            vector<unsigned char> hash(picosha2::k_digest_size);
            picosha2::hash256(hash_input.begin(), hash_input.end(), hash.begin(), hash.end());
            hash[0] = hash[1] = 0;
            disk.Write(begin + i * size, hash.data(), size);
            input.emplace_back(Bits(hash.data(), size, size * 8));
        }

        const uint32_t memory_len = Util::RoundSize(iters) * size;
        auto memory = std::make_unique<uint8_t[]>(memory_len);
        UniformSort::SortToMemory(disk, begin, memory.get(), size, iters, 16);

        sort(input.begin(), input.end());
        uint8_t buf[size];
        for (uint32_t i = 0; i < iters; i++) {
            input[i].ToBytes(buf);
            REQUIRE(memcmp(buf, memory.get() + i * size, size) == 0);
        }
    }
}

         */
    }
}