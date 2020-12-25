using DB;
using Models;
using LiteDB;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Main
{
    public class Blockchain
    {

   
        public Blockchain()
        {
            Initialize();
        }


        private static void Initialize()
        {

            var blocks = GetBlocks();
            // blocks.EnsureIndex(x => x.Height);

            if (blocks.Count() < 1)
            {
                // Create genesis block
                var gnsBlock = Block.Genesis();
                blocks.Insert(gnsBlock);
            }

        }

        public static ILiteCollection<Models.Block> GetBlocks()
        {
            var coll = DbAccess.DB.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height);
            return coll;
        }



        public static Block GetGenesisBlock()
        {
            var blockchain = GetBlocks();
            var block = blockchain.FindOne(Query.All(Query.Ascending));
            return block;
        }

        public static Block GetLastBlock()
        {
            var blockchain = GetBlocks();
            var block = blockchain.FindOne(Query.All(Query.Descending));
            return block;
        }

        public static int GetHeight()
        {
            var lastBlock = GetLastBlock();
            return lastBlock.Height;
        }

        public static void AddBlock(Block block)
        {
            var blocks = GetBlocks();
            blocks.Insert(block);
        }

        public static double GetBalance(string name)
        {
            double balance = 0;
            double spending = 0;
            double income = 0;
            var blockchain = GetBlocks();
            var blocks = blockchain.FindAll();
            int i = 0;
            foreach (Block block in blocks)
            {
                if (i != 0)
                {
                    var transactions = JsonConvert.DeserializeObject<List<Transaction>>(block.Transactions);
                    foreach (Transaction trx in transactions)
                    {

                        var sender = trx.Sender;
                        var recipient = trx.Recipient;

                        if (name.ToLower().Equals(sender.ToLower()))
                        {
                            spending += trx.Amount + trx.Fee;
                        }


                        if (name.ToLower().Equals(recipient.ToLower()))
                        {
                            income += trx.Amount;
                        }

                        balance = income - spending;
                    }
                }
                i++;

            }
            return balance;
        }

        public static List<Transaction> GetTransactionHistory(string name)
        {
     
            var blocks = GetBlocks().FindAll();
            int i = 0;

            var transactionsHistory = new List<Transaction>();
            foreach (Block block in blocks)
            {
                if (i != 0)
                {
                    var transactions = JsonConvert.DeserializeObject<List<Models.Transaction>>(block.Transactions);
                    foreach (Transaction trx in transactions)
                    {

                        var sender = trx.Sender;
                        var recipient = trx.Recipient;

                        if (name.ToLower().Equals(sender.ToLower()))
                        {
                            transactionsHistory.Add(trx);
                        }


                        if (name.ToLower().Equals(recipient.ToLower()))
                        {
                            transactionsHistory.Add(trx);
                        }
                    }
                }
                i++;

            }
            return transactionsHistory;
        }


    }
}