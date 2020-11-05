using System;
using System.Security.Cryptography;
using System.Text;

namespace Models
{
    [Serializable]
    public class Block
    {
        public int Height { get; set; }
        public Int64 TimeStamp { get; set; }
        public string PrevHash { get; set; }
        public string Hash { get; set; }
        public string Transactions { get; set; }


        public Block()
        {
        }

        /**
        To enerate genesis blok
        **/
        public static Block Genesis()
        {
            var ts = new DateTime(2019, 10, 24);
            var genesisTrx = "Genesis Block created by P.Kusuma  on 2019 10 24";
            var hash = Block.GetHash(ts.Ticks, "-", genesisTrx);
            //var block = new Block(1, ts.Ticks, Convert.ToBase64String(Encoding.ASCII.GetBytes("-")), hash, Convert.ToBase64String(Encoding.ASCII.GetBytes(genesisTrx)));
            var block = new Block(1, ts.Ticks, Convert.ToBase64String(Encoding.ASCII.GetBytes("-")), hash, genesisTrx);
            return block;
        }


        // constructure
        public Block(int height, Int64 timestamp, string lastHash, string hash, string transactions)
        {
            this.Height = height;
            this.TimeStamp = timestamp;
            this.PrevHash = lastHash;
            this.Hash = hash;
            this.Transactions = transactions;
        }


        public static string GetHash(Int64 timestamp, string lastHash, string transactions)
        {
            SHA256 sha256 = SHA256.Create();
            var strSum = timestamp + lastHash + transactions;
            byte[] sumBytes = Encoding.ASCII.GetBytes(strSum);
            byte[] hashBytes = sha256.ComputeHash(sumBytes);
            return Convert.ToBase64String(hashBytes);
        }

        public  Block(Block lastBlock, string transactions)
        {
            var lastHeight = lastBlock.Height;
            var lastHash = lastBlock.Hash;
            this.Height = lastHeight + 1;
            this.TimeStamp = DateTime.Now.Ticks;
            this.PrevHash = lastHash;
            this.Transactions = transactions;
            this.Hash = Block.GetHash(this.TimeStamp, lastHash, transactions);
        }

    }
}
