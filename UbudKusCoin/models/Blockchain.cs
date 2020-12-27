using System.Collections.Generic;
using System;
using Utils;

namespace Models
{
    public class Blockchain
    {
        // transaction pool
        public List<Transaction> TransactionPool = new List<Transaction>();

        // list of block in blockchain
        public List<Block> Blocks { set; get; }

        public Blockchain()
        {
            Initialize();
        }

        // initialize blockchain and transaction pool
        private void Initialize()
        {
            Blocks = new List<Block>
                {
                    CreateGenesisBlock()
                };
            TransactionPool = new List<Transaction>();
        }

        /**
         * Add a transaction to pool
         */

        public void AddTransactionToPool(Transaction trx)
        {
            TransactionPool.Add(trx);
        }


        /**
         * Add a transaction to pool
         */

        public void ClearPool()
        {
            TransactionPool = new List<Transaction>();
        }

        /**
         * get last block of blockchain
         */

        public Block GetLastBlock()
        {
            return Blocks[Blocks.Count - 1];
        }

        /**
         * Create genesis block and add it to blockchain
         */

        private static Block CreateGenesisBlock()
        {
            var trx = new Transaction
            {
                Amount = 1000,
                Sender = "Founder",
                Recipient = "Genesis Account",
                Fee = 0.0001
            };
            var trxList = new List<Transaction> { trx }; ;
            return new Block(1, string.Empty.ConvertToBytes(), trxList, "Admin");
        }


        // Create new Block 
        public void CreateBlock()
        {
            var lastBlock = GetLastBlock();
            var nextHeight = lastBlock.Height + 1;
            var prevHash = lastBlock.Hash;
            var transactions = TransactionPool;
            var block = new Block(nextHeight, prevHash, transactions, "Admin");
            Blocks.Add(block);
        }


        /**
        Get transactions by name
        **/

        internal void PrintTransactionHistory(string name)
        {

            Console.WriteLine("\n\n====== Transaction History for {0} =====", name);

            foreach (Block block in Blocks)
            {
                var transactions = block.Transactions;
                foreach (var transaction in transactions)
                {
                    var sender = transaction.Sender;
                    var recipient = transaction.Recipient;

                    if (name.ToLower().Equals(sender.ToLower()) || name.ToLower().Equals(recipient.ToLower()))
                    {
                        Console.WriteLine("Timestamp :{0}", transaction.TimeStamp);
                        Console.WriteLine("Sender:   :{0}", transaction.Sender);
                        Console.WriteLine("Recipient :{0}", transaction.Recipient);
                        Console.WriteLine("Amount    :{0}", transaction.Amount);
                        Console.WriteLine("Fee       :{0}", transaction.Fee);
                        Console.WriteLine("--------------");

                    }
                }
            }
        }

        /**
        Print balance by name
        **/
        public void PrintBalance(string name)
        {

            Console.WriteLine("\n\n==== Balance for {0} ====", name);
            double balance = 0;
            double spending = 0;
            double income = 0;

            foreach (Block block in Blocks)
            {
                var transactions = block.Transactions;

                foreach (var transaction in transactions)
                {

                    var sender = transaction.Sender;
                    var recipient = transaction.Recipient;

                    if (name.ToLower().Equals(sender.ToLower()))
                    {
                        spending += transaction.Amount + transaction.Fee;
                    }


                    if (name.ToLower().Equals(recipient.ToLower()))
                    {
                        income += transaction.Amount;
                    }

                    balance = income - spending;
                }
            }
            Console.WriteLine("Balance :{0}", balance);
            Console.WriteLine("---------");
        }

        public void PrintLastBlock()
        {
            var lastBlock = GetLastBlock();
            PrintBlock(lastBlock);
        }

        public void PrintGenesisBlock()
        {
            var block = Blocks[0];
            PrintBlock(block);
        }

        /**
         * Print all block in blockchain
         */

        public void PrintBlocks()
        {

            Console.WriteLine("\n\n====== Blockchain Explorer =====");

            foreach (Block block in Blocks)
            {
                PrintBlock(block);
            }

        }

        private void PrintBlock(Block block)
        {
            Console.WriteLine("Height      :{0}", block.Height);
            Console.WriteLine("Timestamp   :{0}", block.TimeStamp.ConvertToDateTime());
            Console.WriteLine("Prev. Hash  :{0}", block.PrevHash.ConvertToHexString());
            Console.WriteLine("Hash        :{0}", block.Hash.ConvertToHexString());
            Console.WriteLine("Transactins :{0}", block.Transactions.ConvertToString());
            Console.WriteLine("Creator     :{0}", block.Creator);
            Console.WriteLine("--------------");
        }
    }
}