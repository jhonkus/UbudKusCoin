using LiteDB;
using System.Linq;
using System.Collections.Generic;
using UbudKusCoin;
using System;

namespace Main
{
    public class Blockchain
    {

        public const double COINT_BASE = 2.5;

        public Blockchain()
        {
            // db
            DbAccess.Initialize();

            Initialize();

            //initilize stake
            Stake.Initialize();
            Console.WriteLine(" inisitali succes ...");
        }


        private static void Initialize()
        {

            var blocks = GetBlocks();

            if (blocks.Count() < 1)
            {
                // crate initial transaction
                Transaction.CreateIcoTransction();

                // get all ico transaction from pool
                var trxPool = Transaction.GetPool();
                var transactions = trxPool.FindAll().ToList();

                //convert transaction to json for more easy
                //var strTransactions = JsonConvert.SerializeObject(transactions);

                // create genesis block
                var block = Block.GenesisBlock(transactions);

                // add genesis block to blockchain
                AddBlock(block);

                // move all record in trx pool to transactions table
                foreach (Transaction trx in transactions)
                {
                    Transaction.Add(trx);
                }

                // clear mempool
                trxPool.DeleteAll();

            }

        }

        public static List<Block> GetBlocks(int pageNumber, int resultPerPage)
        {
            var coll = DbAccess.DB.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height);
            var query = coll.Query()
                .OrderByDescending(x => x.Height)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }

        public static ILiteCollection<Block> GetBlocks()
        {
            var coll = DbAccess.DB.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height);
            return coll;
        }



        public static Block GetGenesisBlock()
        {
            var block = GetBlocks().FindAll().FirstOrDefault();
            //var block = blockchain.FindOne(Query.All(Query.Ascending));
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

        public static void BuildNewBlock()
        {


            // get transaction from pool
            var trxPool = Transaction.GetPool();

            //// get last block to get prev hash and last height
            var lastBlock = GetLastBlock();

            Console.WriteLine("\n===========\nNew Block created: ... ");

            var height = lastBlock.Height + 1;
            Console.WriteLine(" = Height: {0}", height);

            var timestamp = Utils.GetTime();
            Console.WriteLine(" = Timestamp: {0}", timestamp);

            var prevHash = lastBlock.Hash;
            Console.WriteLine(" = Prev Hash: {0}", prevHash);

            var creator = Stake.GetCreator();
            Console.WriteLine(" = Creator: {0}", creator);

            var transactions = new List<Transaction>(); // JsonConvert.SerializeObject(new List<Transaction>());

            var conbaseTrx = new Transaction
            {
                Amount = COINT_BASE,
                Recipient = creator,
                Fee = 0,
                TimeStamp = timestamp,
                Sender = "COINBASE"
            };
            conbaseTrx.Build();

            transactions.Add(conbaseTrx);

            if (trxPool.Count() > 0)
            {
                //Get all tx from pool
                transactions.AddRange(trxPool.FindAll());

                // clear mempool
                trxPool.DeleteAll();
            }


            var block = new Block
            {
                Height = height,
                TimeStamp = timestamp,
                PrevHash = prevHash,
                Transactions = transactions,
                Creator = creator
            };
            block.Build();
            AddBlock(block);

            // move all record in trx pool to transactions table
            foreach (var trx in transactions)
            {
                Transaction.Add(trx);
            }

          
        }

    }
}