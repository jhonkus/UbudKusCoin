using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Main
{

    public class Block
    {
        public int Height { get; set; }
        public long TimeStamp { get; set; }
        public string PrevHash { get; set; }
        public string Hash { get; set; }
        public string Transactions { get; set; }

        public Block(Block lastBlock, string transactions)
        {
            var lastHeight = lastBlock.Height;
            var lastHash = lastBlock.Hash;
            Height = lastHeight + 1;
            TimeStamp = DateTime.Now.Ticks;
            PrevHash = lastHash;
            Transactions = transactions;
            Hash = GetHash(TimeStamp, lastHash, transactions);
        }

        public Block(Block lastBlock)
        {
            var lastHeight = lastBlock.Height;
            var lastHash = lastBlock.Hash;
            Height = lastHeight + 1;
            TimeStamp = DateTime.Now.Ticks;
            PrevHash = lastHash;
            Transactions = null;
            Hash = GetHash(TimeStamp, lastHash, null);
        }

        public Block(int height, long timestamp, string lastHash, string hash, string transactions)
        {
            Height = height;
            TimeStamp = timestamp;
            PrevHash = lastHash;
            Hash = hash;
            Transactions = transactions;
        }

        /**
        Create genesis block
        **/
        public static Block Genesis(string transactions)
        {
            var ts = new DateTime(2020, 10, 24);
            var hash = GetHash(ts.Ticks, "-", transactions);
            var block = new Block(0, ts.Ticks, Convert.ToBase64String(Encoding.ASCII.GetBytes("-")), hash, transactions);
            return block;
        }


        public static string GetHash(long timestamp, string lastHash, string transactions)
        {
            SHA256 sha256 = SHA256.Create();
            var strSum = timestamp + lastHash + transactions;
            byte[] sumBytes = Encoding.ASCII.GetBytes(strSum);
            byte[] hashBytes = sha256.ComputeHash(sumBytes);
            return Convert.ToBase64String(hashBytes);
        }


    }
}
