using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Main
{

    public class Block
    {
        //public string ID {get; set;}
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
            Hash = Utils.GetHash(TimeStamp, lastHash, transactions);
            //ID = Converter.ConvertToHexString(Converter.ConvertToBytes(Hash));
        }

        public Block(Block lastBlock)
        {
            var lastHeight = lastBlock.Height;
            var lastHash = lastBlock.Hash;
            Height = lastHeight + 1;
            TimeStamp = DateTime.Now.Ticks;
            PrevHash = lastHash;
            Transactions = null;
            Hash = Utils.GetHash(TimeStamp, lastHash, null);
            //ID = Converter.ConvertToHexString(Converter.ConvertToBytes(Hash));
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
        public static Block Genesis( string transactions)
        {
 
              var ts = new DateTime(2019, 10, 24);
            var hash = Utils.GetHash(ts.Ticks, "-", transactions);
            //var block = new Block(1, ts.Ticks, Convert.ToBase64String(Encoding.ASCII.GetBytes("-")), hash, Convert.ToBase64String(Encoding.ASCII.GetBytes(genesisTrx)));
            var block = new Block(1, ts.Ticks, Convert.ToBase64String(Encoding.ASCII.GetBytes("-")), hash, transactions);
            return block;
        }



    }
}
