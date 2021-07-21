using System;
using Dirichlet.Numerics;

namespace Chiapos.Dotnet
{
    // Class to evaluate F1
    public class F1Calculator
    {
        byte k_;

        // ChaCha8 context
        Chacha8 chacha8 = new();

        byte[] buf_;


        public F1Calculator(byte k, byte[] orig_key)
        {
            var enc_key = new byte[32];
            int buf_blocks = Util.Cdiv(k << (int) Constants.kBatchSizes, Constants.kF1BlockSizeBits) + 1;
            this.k_ = k;
            this.buf_ = new byte[buf_blocks * Constants.kF1BlockSizeBits / 8 + 7];

            // First byte is 1, the index of this table
            enc_key[0] = 1;
            Array.Copy(orig_key, 0, enc_key, 1, 31);

            // Setup ChaCha8 context with zero-filled IV
            chacha8.KeySetup(enc_key, 256, default);
        }

        // Reloading the encryption key is a no-op since encryption state is local.
        public void ReloadKey()
        {
        }

        // Performs one evaluation of the F function on input L of k bits.
        public Bits CalculateF(Bits L)
        {
            ushort num_output_bits = k_;
            ushort block_size_bits = Constants.kF1BlockSizeBits;

            // Calculates the counter that will be used to get ChaCha8 keystream.
            // Since k < block_size_bits, we can fit several k bit blocks into one
            // ChaCha8 block.
            UInt128 counter_bit = L.GetValue() * (UInt128) num_output_bits;
            ulong counter = (ulong) (counter_bit / block_size_bits);

            // How many bits are before L, in the current block
            uint bits_before_L = (uint) (counter_bit % block_size_bits);

            // How many bits of L are in the current block (the rest are in the next block)
            ushort bits_of_L = Math.Min((ushort) (block_size_bits - bits_before_L), num_output_bits);

            // True if L is divided into two blocks, and therefore 2 ChaCha8
            // keystream blocks will be generated.
            bool spans_two_blocks = bits_of_L < num_output_bits;

            var cipher_buffer = new byte[Constants.kF1BlockSizeBits / 8 * 2];
            var ciphertext_bytes = new Span<byte>(cipher_buffer, 0, Constants.kF1BlockSizeBits / 8);
            var ciphertext_bytes1 = new Span<byte>(cipher_buffer, Constants.kF1BlockSizeBits / 8, Constants.kF1BlockSizeBits / 8);
            Bits output_bits;

            // This counter is used to initialize words 12 and 13 of ChaCha8
            // initial state (4x4 matrix of 32-bit words). This is similar to
            // encrypting plaintext at a given offset, but we have no
            // plaintext, so no XORing at the end.
            chacha8.GetKeystream(counter, 1, ciphertext_bytes);
            Bits ciphertext0 = new Bits(ciphertext_bytes, block_size_bits);

            if (spans_two_blocks)
            {
                // Performs another encryption if necessary
                ++counter;
                chacha8.GetKeystream(counter, 1, ciphertext_bytes1);
                var ciphertext1 = new Bits(ciphertext_bytes1, block_size_bits);

                output_bits = new Bits(ciphertext_bytes, block_size_bits * 2);
                var output_length = ciphertext0.Length - bits_before_L + num_output_bits - bits_of_L;
                output_bits = (Bits)output_bits.Slice((int)bits_before_L, (int)(bits_before_L + output_length));
                //ciphertext0.Slice(bits_before_L) +
                //          ciphertext1.Slice(0, num_output_bits - bits_of_L);
            }
            else
            {
                output_bits = (Bits)ciphertext0.Slice((int)bits_before_L, (int)(bits_before_L + num_output_bits));
            }

            // Adds the first few bits of L to the end of the output, production k + kExtraBits of
            // output
            ulong extra = L.GetValue() & ((1 << Constants.kExtraBits) - 1);
            output_bits.AppendValue(extra, Constants.kExtraBits);
            
            /*Bits extra_data = L.Slice(0, Constants.kExtraBits);
            if (extra_data.Length < Constants.kExtraBits)
            {
                extra_data.AppendValue(0, Constants.kExtraBits - extra_data.Length);
            }*/

            return output_bits;
        }

        // Returns an evaluation of F1(L), and the metadata (L) that must be stored to evaluate F2.
        public ValueTuple<Bits, Bits> CalculateBucket(Bits L)
        {
            return (CalculateF(L), L);
        }

        // F1(x) values for x in range [first_x, first_x + n) are placed in res[].
        // n must not be more than 1 << kBatchSizes.
        public void CalculateBuckets(ulong first_x, ulong n, ulong[] res)
        {
            ulong start = first_x * k_ / Constants.kF1BlockSizeBits;
            // 'end' is one past the last keystream block number to be generated
            ulong end = Util.Cdiv((first_x + n) * k_, Constants.kF1BlockSizeBits);
            uint num_blocks = (uint) (end - start);
            uint start_bit = (uint) (first_x * k_ % Constants.kF1BlockSizeBits);
            byte x_shift = (byte) (k_ - Constants.kExtraBits);

            //assert(n <= (1U << kBatchSizes));

            chacha8.GetKeystream(start, num_blocks, buf_);
            for (ulong x = first_x; x < first_x + n; x++)
            {
                ulong y = Util.SliceInt64FromBytes(buf_, start_bit, k_);

                res[x - first_x] = (y << Constants.kExtraBits) | (x >> x_shift);

                start_bit += k_;
            }
        }

    }
}