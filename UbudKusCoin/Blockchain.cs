using DB;
using Models;
using LiteDB;
using Newtonsoft.Json;
using System;

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

                // create genesis transaction and block
                CreateGenesisTransction();
            }

        }

        /**
         create transaction for genesis account
         asume have two genesis account with name ga1 and ga2
         each have opening balance 1.000.000 and 2.000.000
        **/

        public static void CreateGenesisTransction()
        {
            var newTrx = new Transaction()
            {
                TimeStamp = DateTime.Now.Ticks,
                Sender = "system",
                Recipient = "ga1",
                Amount = 1000000,
                Fee = 0
            };
            Transaction.AddToPool(newTrx);

            newTrx = new Transaction()
            {
                TimeStamp = DateTime.Now.Ticks,
                Sender = "system",
                Recipient = "ga2",
                Amount = 2000000,
                Fee = 0
            };
            Transaction.AddToPool(newTrx);

            var trxPool = Transaction.GetPool();
            var transactions = trxPool.FindAll();
            string tempTransactions = JsonConvert.SerializeObject(transactions);
            var lastBlock = GetLastBlock();
            var block = new Block(lastBlock, tempTransactions);

            // add block to blockchain
            AddBlock(block);

            // move all record from trx pool to transactions table
            foreach (Transaction trx in transactions)
            {
                Transaction.Add(trx);
            }

            // clear mempool
            trxPool.DeleteAll();

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


    }
}