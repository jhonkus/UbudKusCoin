using System.Collections.Generic;
using System.Text;
using System;
using Utils; 

namespace Models
{
    public class Blockchain
    {

 
        public List<Block> Blocks { set; get; }

        // transaction pool to hold all transaction before make block
        private List<Transaction> TrxPool { set; get; }

        public Blockchain()
        {
            Initialize();
            
        }


        private void Initialize()
        {
            //initilize transaction pool
            TrxPool = new List<Transaction>();

            Blocks = new List<Block>
            {
                CreateGenesisBlock()
            };

        }

        public Block GetLastBlock()
        {
            return Blocks[Blocks.Count - 1];
        }

        public int GetHeight()
        {
            var lastBlock  = Blocks[Blocks.Count - 1];
            return lastBlock.Height;
        }

        private Block CreateGenesisBlock()
        {
            var lst = new List<Transaction>();
            var trx = new Transaction
            {
                Amount = 2000000,
                Sender = "Founder",
                Recipient = "Genesis Account",
                Fee = 0.0001
            };
            lst.Add(trx);

            var trxByte = lst.ToArray().ConvertToByte();
            return new Block(1, String.Empty.ConvertToBytes(), lst.ToArray(), "Admin");
        }

        /** 
         * Get genesis block, first block in Blockchain
         **/ 
        internal Block GetGenesisBlock()
        {
            return Blocks[0];
        }


        // Add new Block to blockchain 
        public void AddBlock(Transaction[] transactions)
        {

            var lastBlock = GetLastBlock();
            var nextHeight = lastBlock.Height + 1;
            var prevHash = lastBlock.Hash;
            var timestamp = DateTime.Now.Ticks;
            var block = new Block(nextHeight, prevHash, transactions, "Admin");
            Blocks.Add(block);

        }

       
        public double GetBalance(string name) {

            double balance = 0;
            double spending = 0;
            double income = 0;

            foreach (Block block in Blocks)
            {
                var transactions = block.Transactions;
              
                foreach (Transaction transaction in transactions)
                {

                    var sender = transaction.Sender;
                    var recipient = transaction.Recipient;

                    if (name.ToLower().Equals(sender.ToLower())) {
                        spending += transaction.Amount + transaction.Fee;                    
                    }


                    if (name.ToLower().Equals(recipient.ToLower())) {
                        income += transaction.Amount;
                    }

                    balance = income - spending;
                }
            }
            return balance;
        }

        internal List<Transaction> GetPool()
        {
            return TrxPool;
        }


        //Get all block in Blockchain
        public List<Block> GetBlocks()
        {
            return Blocks;
        }



    }
}