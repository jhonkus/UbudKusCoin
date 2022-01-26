using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using UbudKusCoin.Others;

namespace UbudKusCoin.Models
{

    public class Block
    {
        public int Version { get; set; }
        public long Height { get; set; }
        public long TimeStamp { get; set; }
        public string PrevHash { get; set; }
        public string Hash { get; set; }
        public string MerkleRoot { get; set; }
        public IList<Transaction> Transactions { get; set; }
        public string Validator { get; set; }
        public long NumOfTx { get; set; }
        public double TotalAmount { get; set; }
        public float TotalReward { get; set; }
        public int Difficulty { get; set; }

        public long Size { get; set; }
        public int BuildTime { get; set; }

        public void Build()
        {
            Version = 1;
            NumOfTx = Transactions.Count;
            TotalAmount = GetTotalAmount();
            TotalReward = GetTotalFees();
            MerkleRoot = GetMerkleRoot();
            Hash = GetBlockHash();
            Difficulty = 1;
        }

        private float GetTotalFees()
        {
            var totFee = Transactions.AsEnumerable().Sum(x => x.Fee);
            return totFee;
        }

        private double GetTotalAmount()
        {
            var totalAmount = Transactions.AsEnumerable().Sum(x => x.Amount);
            return totalAmount;
        }

        /**
        Create genesis block
        **/
        public static Block GenesisBlock(IList<Transaction> transactions)
        {
            var startTimer = DateTime.UtcNow;

            var ts = Utils.GetTime(); //21 june 2017

            // for genesis bloc we set creatoris first of Genesis Account
            var validator = Genesis.GetAll().FirstOrDefault();
            var block = new Block
            {
                Height = 1,
                TimeStamp = ts,
                PrevHash = "-",
                Transactions = transactions,
                Validator = validator.Address
            };
            block.Build();

            //block size
            var str = JsonSerializer.Serialize(block);
            block.Size = str.Length;

            // get build time    
            var endTimer = DateTime.UtcNow;
            var buildTime = endTimer - startTimer;
            block.BuildTime = buildTime.Milliseconds;
            // end of    

            return block;
        }


        public string GetBlockHash()
        {
            var strSum = Version + PrevHash + MerkleRoot + TimeStamp + Difficulty + Validator;
            var hash = Utils.GenHash(strSum);
            return hash;
        }

        private string GetMerkleRoot()
        {
            // List<Transaction> txList = JsonConvert.DeserializeObject<List<Transaction>>(jsonTxs);
            var txsHash = new List<string>();
            foreach (var tx in Transactions)
            {
                txsHash.Add(tx.Hash);
            }

            var hashRoot = Utils.CreateMerkleRoot(txsHash.ToArray());
            return hashRoot;
        }


    }
}
