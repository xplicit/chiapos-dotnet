namespace Chiapos.Dotnet.Tests
{
    public class PlottingTests
    {
        /*
         * void HexToBytes(const string& hex, uint8_t* result)
{
    for (unsigned int i = 0; i < hex.length(); i += 2) {
        string byteString = hex.substr(i, 2);
        uint8_t byte = (uint8_t)strtol(byteString.c_str(), NULL, 16);
        result[i / 2] = byte;
    }
}

void TestProofOfSpace(
    std::string filename,
    uint32_t iterations,
    uint8_t k,
    uint8_t* plot_id,
    uint32_t num_proofs)
{
    DiskProver prover(filename);
    uint8_t* proof_data = new uint8_t[8 * k];
    uint32_t success = 0;
    // Tries an edge case challenge with many 1s in the front, and ensures there is no segfault
    vector<unsigned char> hash(picosha2::k_digest_size);
    HexToBytes("fffffa2b647d4651c500076d7df4c6f352936cf293bd79c591a7b08e43d6adfb", hash.data());
    prover.GetQualitiesForChallenge(hash.data());

    for (uint32_t i = 0; i < iterations; i++) {
        vector<unsigned char> hash_input = intToBytes(i, 4);
        vector<unsigned char> hash(picosha2::k_digest_size);
        picosha2::hash256(hash_input.begin(), hash_input.end(), hash.begin(), hash.end());
        vector<LargeBits> qualities = prover.GetQualitiesForChallenge(hash.data());
        Verifier verifier = Verifier();
        for (uint32_t index = 0; index < qualities.size(); index++) {
            LargeBits proof = prover.GetFullProof(hash.data(), index);
            proof.ToBytes(proof_data);

            LargeBits quality = verifier.ValidateProof(plot_id, k, hash.data(), proof_data, k * 8);
            REQUIRE(quality.GetSize() == 256);
            REQUIRE(quality == qualities[index]);
            success += 1;

            // Tests invalid proof
            proof_data[0] = (proof_data[0] + 1) % 256;
            LargeBits quality_2 =
                verifier.ValidateProof(plot_id, k, hash.data(), proof_data, k * 8);
            REQUIRE(quality_2.GetSize() == 0);
        }
    }
    std::cout << "Success: " << success << "/" << iterations << " "
              << (100 * ((double)success / (double)iterations)) << "%" << std::endl;
    REQUIRE(success == num_proofs);
    REQUIRE(success > 0.5 * iterations);
    REQUIRE(success < 1.5 * iterations);
    delete[] proof_data;
}

void PlotAndTestProofOfSpace(
    std::string filename,
    uint32_t iterations,
    uint8_t k,
    uint8_t* plot_id,
    uint32_t buffer,
    uint32_t num_proofs,
    uint32_t stripe_size,
    uint8_t num_threads)
{
    DiskPlotter plotter = DiskPlotter();
    uint8_t memo[5] = {1, 2, 3, 4, 5};
    plotter.CreatePlotDisk(
        ".", ".", ".", filename, k, memo, 5, plot_id, 32, buffer, 0, stripe_size, num_threads);
    TestProofOfSpace(filename, iterations, k, plot_id, num_proofs);
    REQUIRE(remove(filename.c_str()) == 0);
}

TEST_CASE("Plotting")
{
    SECTION("Disk plot k18")
    {
        PlotAndTestProofOfSpace("cpp-test-plot.dat", 100, 18, plot_id_1, 11, 95, 4000, 2);
    }
    SECTION("Disk plot k19")
    {
        PlotAndTestProofOfSpace("cpp-test-plot.dat", 100, 19, plot_id_1, 100, 71, 8192, 2);
    }
    SECTION("Disk plot k19 single-thread")
    {
        PlotAndTestProofOfSpace("cpp-test-plot.dat", 100, 19, plot_id_1, 100, 71, 8192, 1);
    }
    SECTION("Disk plot k20")
    {
        PlotAndTestProofOfSpace("cpp-test-plot.dat", 500, 20, plot_id_3, 100, 469, 16000, 2);
    }
    SECTION("Disk plot k21")
    {
        PlotAndTestProofOfSpace("cpp-test-plot.dat", 5000, 21, plot_id_3, 100, 4945, 8192, 4);
    }
    // SECTION("Disk plot k24") { PlotAndTestProofOfSpace("cpp-test-plot.dat", 100, 24, plot_id_3,
    // 100, 107); }
}

TEST_CASE("Invalid plot")
{
    SECTION("File gets deleted")
    {
        string filename = "invalid-plot.dat";
        {
            DiskPlotter plotter = DiskPlotter();
            uint8_t memo[5] = {1, 2, 3, 4, 5};
            uint8_t k = 20;
            plotter.CreatePlotDisk(".", ".", ".", filename, k, memo, 5, plot_id_1, 32, 200, 32, 8192, 2);
            DiskProver prover(filename);
            uint8_t* proof_data = new uint8_t[8 * k];
            uint8_t challenge[32];
            size_t i;
            memset(challenge, 155, 32);
            vector<LargeBits> qualities;
            for (i = 0; i < 50; i++) {
                qualities = prover.GetQualitiesForChallenge(challenge);
                if (qualities.size())
                    break;
                challenge[0]++;
            }
            Verifier verifier = Verifier();
            REQUIRE(qualities.size() > 0);
            for (uint32_t index = 0; index < qualities.size(); index++) {
                LargeBits proof = prover.GetFullProof(challenge, index);
                proof.ToBytes(proof_data);
                LargeBits quality =
                    verifier.ValidateProof(plot_id_1, k, challenge, proof_data, k * 8);
                REQUIRE(quality == qualities[index]);
            }
            delete[] proof_data;
        }
        REQUIRE(remove(filename.c_str()) == 0);
        REQUIRE_THROWS_WITH([&]() { DiskProver p(filename); }(), "Invalid file " + filename);
    }
}

         */
    }
}