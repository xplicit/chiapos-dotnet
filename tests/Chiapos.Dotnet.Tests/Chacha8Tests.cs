using System;
using Dirichlet.Numerics;
using NUnit.Framework;

namespace Chiapos.Dotnet.Tests
{
    [TestFixture]
    public class Chacha8Tests
    {
        [Test]
        public void Chacha8_CanGenerateKeyStream()
        {
            byte test_k = 35;
            var test_key = new byte[] {0, 2, 3, 4,  5, 5, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                1, 2, 3, 41, 5, 6, 7, 8, 9, 10, 11, 12, 13, 11, 15, 16};

            // First byte is 1, the index of this table
            var chacha_key = new byte[32];
            chacha_key[0] = 1;
            Array.Copy(test_key, 0, chacha_key, 1, 31);

            Chacha8 chacha8 = new();
            chacha8.KeySetup(chacha_key, 256, default);

            Bits L = new Bits(525, test_k);
            
            UInt128 counter_bit = L.GetValue() * (UInt128) test_k;
            ulong counter = (ulong) (counter_bit / Constants.kF1BlockSizeBits);
            Assert.That(counter_bit, Is.EqualTo((UInt128)18375));
            Assert.That(counter, Is.EqualTo(35));
            
            var cipher_buffer = new byte[Constants.kF1BlockSizeBits / 8 ];
            var ciphertext_bytes = new Span<byte>(cipher_buffer, 0, Constants.kF1BlockSizeBits / 8);
            chacha8.GetKeystream(counter, 1, ciphertext_bytes);
            
            var expectedChacha8KeyStream = Convert.FromHexString(
                "4f96a056ad12e0ebc4bc0a9fa68b9da0b28c4f70978623b0c2d4f3e90ac8c20fbe6f0c55200fa714efa926a5a91f20a825744f60e1713c80bedcecf7f13d2263");
            
            Assert.That(cipher_buffer, Is.EquivalentTo(expectedChacha8KeyStream));
        }
    }
}