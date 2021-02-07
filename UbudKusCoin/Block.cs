using System;
using System.Text;

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
            TimeStamp = Utils.GetTime();
            PrevHash = lastHash;
            Transactions = transactions;
            Hash = GetBlockHash(TimeStamp, lastHash, transactions);
        }

        public Block(Block lastBlock)
        {
            var lastHeight = lastBlock.Height;
            var lastHash = lastBlock.Hash;
            Height = lastHeight + 1;
            TimeStamp = DateTime.Now.Ticks;
            PrevHash = lastHash;
            Transactions = null;
            Hash = GetBlockHash(TimeStamp, lastHash, null);
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
            var hash = GetBlockHash(ts.Ticks, "-", transactions);
            var block = new Block(0, ts.Ticks, Convert.ToBase64String(Encoding.ASCII.GetBytes("-")), hash, transactions);
            return block;
        }


        public static string GetBlockHash(long timestamp, string lastHash, string transactions)
        {
            var strSum = timestamp + lastHash + transactions;
            return Utils.GenHash(strSum);
        }


    }
}
