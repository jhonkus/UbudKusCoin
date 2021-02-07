using System.Collections.Generic;
using System.Linq;
using System;
using UbudKusCoin;

namespace Main
{

    public class Block
    {
        public int Height { get; set; }
        public long TimeStamp { get; set; }
        public string PrevHash { get; set; }
        public string Hash { get; set; }
        public string MerkleRoot { get; set; }
        public IList<Transaction> Transactions { get; set; }
        public string Creator { get; set; }
        public int NumOfTx { get; set; }
        public double TotalAmount { get; set; }
        public float TotalReward { get; set; }


        public void Build()
        {
            NumOfTx = Transactions.Count;
            Console.WriteLine(" = Num Of Tx: {0}", NumOfTx);
            TotalAmount = GetTotalAmount();
            TotalReward = GetTotalFees();
            MerkleRoot = GetMerkleRoot();
            Hash = GetBlockHash();
        }

        private float GetTotalFees()
        {
            var totFee = Transactions.AsEnumerable().Sum(x => x.Fee);
            Console.WriteLine(" = Total Fee: {0}", totFee);
            return totFee;
        }

        private double GetTotalAmount()
        {
           var totalAmount =  Transactions.AsEnumerable().Sum(x => x.Amount);
            Console.WriteLine(" = Total Amount: {0}", totalAmount);
            return totalAmount;
        }

        /**
        Create genesis block
        **/
        public static Block GenesisBlock(IList<Transaction> transactions)
        {
            var ts = 1498018714; //21 june 2017

            // for genesis bloc we set creatoris first of Genesis Account
            var creator = Genesis.GetAll().FirstOrDefault();
            var block = new Block
            {
                Height = 0,
                TimeStamp = ts,
                PrevHash = "-",
                Transactions = transactions,
                Creator = creator.Address
            };
            block.Build();

            return block;
        }


        public  string GetBlockHash()
        {
            var strSum = TimeStamp + PrevHash + MerkleRoot + Creator;
            var hash = Utils.GenHash(strSum);
            Console.WriteLine(" = Hash: {0}", hash);
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
            Console.WriteLine(" = Merkle Root: {0}", hashRoot);
            return hashRoot;
        }


    }
}
