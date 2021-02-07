    using LiteDB;
    using Newtonsoft.Json;
    using System;
    using System.Linq;
    using System.Collections.Generic;

    namespace Main
    {
        public class Blockchain
        {


            public Blockchain()
            {
                // db
                DbAccess.Initialize();

                Initialize();
            }


            private static void Initialize()
            {

                var blocks = GetBlocks();

                if (blocks.Count() < 1)
                {
                    Transaction.CreateIcoTransction();
                    var trxPool = Transaction.GetPool();
                    var transactions = trxPool.FindAll();
                    var strTransactions = JsonConvert.SerializeObject(transactions);


                    var block = Block.Genesis(strTransactions);
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

            public static void BuildNewBlock()
            {

                var trxPool = Transaction.GetPool();

                // build merkle tree

                // var transactionStrList = trxPool.FindAll().Select(tran => CalculateHash(CalculateHash(tran.From + tran.To + tran.Amount))).ToList();

                var lastBlock = GetLastBlock();

                if (trxPool.Count() <= 0)
                {

                    var lstTrx = new List<Transaction>();
                    string tempTransactions = JsonConvert.SerializeObject(lstTrx);
                    var block = new Block(lastBlock, tempTransactions);
                    Console.WriteLine("Block w/o trx created height: {0}, timestamp: {1}", block.Height, block.TimeStamp);
                    AddBlock(block);

                }
                else
                {
                    var transactions = trxPool.FindAll();
                    // var transactionStrList = transactions.Select(trx => Utils.DoReverse(Utils.DoSwap(trx.ID)).ToList());
                    var transactionHashes = new List<string>();
                    foreach (dynamic trx in transactions)
                    {
                            transactionHashes.Add(Utils.ReverseString(Utils.SwapString(trx.ID)));
                    }

                    var markleRoot =   Utils.CreateMerkleRoot(transactionHashes);

                    var markleRootFinal = Utils.ReverseString(Utils.SwapString(markleRoot));

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