using System.Collections.Generic;
using System.Text;
using System;
using Utils; 

namespace Models
{
    public class Blockchain
    {

 
        public IList<Block> Blocks { set; get; }
        public List<Transaction> TrxPool {set;  get;}

        public Blockchain()
        {
            Initialize();
        }


        private void Initialize()
        {
            Blocks = new List<Block>
            {
                CreateGenesisBlock()
            };

            TrxPool = new List<Transaction>();
        }

        public Block GetGenesisBlock()
        {
            return Blocks[0];
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
                Amount = 1000,
                Sender = "Founder",
                Recipient = "Genesis Account",
                Fee = 0.0001
            };
            lst.Add(trx);

            var trxByte = lst.ToArray().ConvertToByte();
            return new Block(1, String.Empty.ConvertToBytes(), lst.ToArray(), "Admin");
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

        public void PrintBlocks() {

            var sbf = new StringBuilder();

            foreach (Block block in Blocks)
            {
                Console.WriteLine("Height:      {0}", block.Height);
                Console.WriteLine("Timestamp:   {0}", block.TimeStamp.ConvertToDateTime());
                Console.WriteLine("Prev. Hash:  {0}", block.PrevHash.ConvertToHexString());
                Console.WriteLine("Hash:        {0}", block.Hash.ConvertToHexString());
                Console.WriteLine("Transactins: {0}", block.Transactions.ConvertToString());
                Console.WriteLine("Creator:     {0}", block.Creator);
                Console.WriteLine("--------------\n");

            }

            Console.WriteLine(sbf);


        }
    }
}