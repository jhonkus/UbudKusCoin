    using LiteDB;
    using Newtonsoft.Json;
    using System;
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
                   var trxList =  Transaction.CreateIcoTransction();
                   string genesisTrx = JsonConvert.SerializeObject(trxList);
                    var gnsBlock = Block.Genesis(genesisTrx);
                    blocks.Insert(gnsBlock);
                }

            }

            public static List<Block> GetBlocks(int pageNumber, int resultPerPage)
            {
                var coll = DbAccess.DB.GetCollection<Block>(DbAccess.TBL_BLOCKS);
                coll.EnsureIndex(x => x.Height);
                var query = coll.Query()
                    .OrderByDescending(x => x.Height)
                    .Offset((pageNumber-1) * resultPerPage)
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

            public static void CreateBlock()
            {

                var trxPool = Transaction.GetPool();
                var lastBlock = GetLastBlock();

                if (trxPool.Count() <= 0)
                {
                    //Create transaction
                    var newTrx = new Transaction()
                    {
                        TimeStamp = DateTime.Now.Ticks,
                        Sender = "-",
                        Recipient = "-",
                        Amount = 0,
                        Fee = 0.001f
                    };

                    var lstTrx = new List<Transaction>
                    {
                        newTrx
                    };

                    // create empty block
                    string tempTransactions = JsonConvert.SerializeObject(lstTrx);
                    var block = new Block(lastBlock, tempTransactions);
                    Console.WriteLine("Empty Block created height: {0}, timestamp: {1}", block.Height, block.TimeStamp);
                    AddBlock(block);

                }
                else
                {
                    var transactions = trxPool.FindAll();

                    // create block from transaction pool
                    string tempTransactions = JsonConvert.SerializeObject(transactions);

                    var block = new Block(lastBlock, tempTransactions);
                    Console.WriteLine("Block created height: {0}, timestamp: {1}", block.Height, block.TimeStamp);

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

        }
    }