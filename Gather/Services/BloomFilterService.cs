using System;
using System.Security.Cryptography;
using System.Text;

namespace Gather.Services
{
    public interface IBloomFilterService
    {
        bool CheckExists(string source);
    }

    public class BloomFilterService : IBloomFilterService
    {
        private readonly BitMap bitMap;
        private readonly int capcity;
        private readonly HashAlgorithm[] hashAlgorithms;
        public BloomFilterService(int count)
        {
            // TODO if has file 'bloom_filter.bin', load it to memory
            capcity = count * 5;
            bitMap = new BitMap(capcity);
            hashAlgorithms = new HashAlgorithm[] { MD5.Create(), SHA1.Create(), SHA256.Create() };
        }

        public bool CheckExists(string source)
        {
            var exists = true;
            foreach (var hashAlgorithm in hashAlgorithms)
            {
                var idx = GetBitMapIndex(GetHashInt(hashAlgorithm, source), capcity);
                if (!bitMap.Get(idx))
                {
                    bitMap.Set(idx);
                    exists = false;
                }
            }
            return exists;
        }

        // TODO Persist to file(bloom_filter.bin) each half hour
        public void PersistToFile()
        {

        }

        private int GetHashInt(HashAlgorithm hashAlgorithm, string source)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(source));
            return BitConverter.ToInt32(data, 0);
        }

        private int GetBitMapIndex(int hashInt, int capcity)
        {
            hashInt = hashInt < 0 ? -hashInt : hashInt;
            return hashInt % (capcity - 1);
        }

    }

    class BitMap
    {
        private readonly char[] bytes;
        private readonly int nbits;

        public BitMap(int nbits)
        {
            this.nbits = nbits;
            this.bytes = new char[nbits / 16 + 1];
        }

        public void Set(int k)
        {
            if (k > nbits)
                return;
            var byteIndex = k / 16;
            var bitIndex = k % 16;
            var result = bytes[byteIndex] | (1 << bitIndex);
            bytes[byteIndex] = Convert.ToChar(result);
        }

        public bool Get(int k)
        {
            if (k > nbits)
                return false;
            var byteIndex = k / 16;
            var bitIndex = k % 16;
            return (bytes[byteIndex] & (1 << bitIndex)) != 0;
        }
    }
}
