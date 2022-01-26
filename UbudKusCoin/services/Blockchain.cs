using System;
using System.Linq;
using System.Collections.Generic;
using LiteDB;
using UbudKusCoin.Models;
using UbudKusCoin.Others;

namespace UbudKusCoin.Services
{
    public class Blockchain
    {

        public const float COINT_REWARD = 0.01f;

        public Blockchain()
        {
            // db
            DbAccess.Initialize();

            Initialize();

            //initilize stake
            Stake.Initialize();
            Console.WriteLine(" initilize success ...");
        }

        public static float GetCoinReward()
        {
            return COINT_REWARD;
        }

        private static void Initialize()
        {

            var blocks = GetBlocks();

            if (blocks.Count() < 1)
            {
                // start block time
                var startTimer = DateTime.UtcNow;

                // crate genesis transaction
                Transaction.CreateGenesisTransction();

                // crate ICO transaction
                Transaction.CreateIcoTransction();

                // get all ico transaction from pool
                // var trxPool = Transaction.GetPool();
                // var transactions = trxPool.FindAll().ToList();

                var transactions = Transaction.GetPool();

                //convert transaction to json for more easy
                //var strTransactions = JsonConvert.SerializeObject(transactions);

                // create genesis block
                var block = Block.GenesisBlock(transactions);

                var str = System.Text.Json.JsonSerializer.Serialize(block);
                block.Size = str.Length;

                // get build time    
                var endTimer = DateTime.UtcNow;
                var buildTime = endTimer - startTimer;
                block.BuildTime = buildTime.Milliseconds;
                // end of    

                // update accoiunt table
                var txns = block.Transactions;
                AccountServiceImpl.UpdateAccountBalance(txns);

                // add genesis block to blockchain
                AddBlock(block);

                // move all record in trx pool to transactions table
                foreach (Transaction trx in transactions)
                {
                    Transaction.Add(trx);
                }

                // clear mempool
                // trxPool.DeleteAll();
                Transaction.DeletePool();

            }

        }

        public static List<Block> GetBlocks(int pageNumber, int resultPerPage)
        {
            var coll = DbAccess.DB_BLOCKS.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height);
            var query = coll.Query()
                .OrderByDescending(x => x.Height)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }

        public static ILiteCollection<Block> GetBlocks()
        {
            var coll = DbAccess.DB_BLOCKS.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height);
            return coll;
        }

        public static IEnumerable<Block> GetBlocksByValidator(string address)
        {

            var coll = DbAccess.DB_BLOCKS.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Validator);
            var query = coll.Query()
                .OrderByDescending(x => x.Height)
                .Where(x => x.Validator == address)
                .Limit(20).ToList();
            return query;


            // var coll = DbAccess.DB.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            // coll.EnsureIndex(x => x.Validator);
            // var blocks = coll.Find(x => x.Validator == address);
            // return blocks;
        }


        public static Block GetGenesisBlock()
        {
            var block = GetBlocks().FindAll().FirstOrDefault();
            return block;
        }


        /**
        Get block by height
        */
        public static Block GetBlockByHeight(int height)
        {
            var coll = DbAccess.DB_BLOCKS.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height); ;
            var block = coll.FindOne(x => x.Height == height);
            return block;
        }

        public static Block GetBlockByHash(string hash)
        {
            var coll = DbAccess.DB_BLOCKS.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height); ;
            var block = coll.FindOne(x => x.Hash == hash);
            return block;
        }
        public static Block GetLastBlock()
        {
            var blockchain = GetBlocks();
            var block = blockchain.FindOne(Query.All(Query.Descending));
            return block;
        }

        public static long GetHeight()
        {
            var lastBlock = GetLastBlock();
            return lastBlock.Height;
        }

        public static void AddBlock(Block block)
        {
            var blocks = GetBlocks();
            blocks.Insert(block);
        }


        public static List<Transaction> GiveOtherInfos(List<Transaction> trxs, long height)
        {
            foreach (var trx in trxs)
            {
                trx.Height = height;
            }
            return trxs;
        }

        public static void BuildNewBlock()
        {

            // start build time
            var startTimer = DateTime.UtcNow;

            // get transaction from pool
            var trxPool = Transaction.GetPool();

            //// get last block to get prev hash and last height
            var lastBlock = GetLastBlock();
            var height = lastBlock.Height + 1;
            var timestamp = Utils.GetTime();
            var prevHash = lastBlock.Hash;
            var validator = Stake.GetValidator();


            var transactions = new List<Transaction>(); // JsonConvert.SerializeObject(new List<Transaction>());


            // validator will get coin reward from genesis account
            // to keep total coin in Blockchain not changed
            var conbaseTrx = new Transaction
            {
                Amount = 0,
                // Recipient = "UKC_QPQY9wHP0jxi/0c/YRlch2Uk5ur/T8lcOaawqyoe66o=",
                Recipient = "UkcDrMshfqdHrfckb2SLoSCoG8Lp6MBdrkZ2T83FivTpWC8",
                Fee = COINT_REWARD,
                TimeStamp = timestamp,
                // Sender = "UKC_rcyChuW7cQcIVoKi1LfSXKfCxZBHysTwyPm88ZsN0BM="
                Sender = "UkcDEfU9gGnm9tGjmFtXRjirf2LuohU5CzjWunEkPNbUcFW",
            };

            if (trxPool.Count() > 0)
            {
                //Get all tx from pool
                conbaseTrx.Recipient = validator;
                //sum all fees and give block creator as reward
                conbaseTrx.Amount = GetTotalFees(trxPool);
                conbaseTrx.Build();

                // add coinbase trx to list    
                transactions.Add(conbaseTrx);
                transactions.AddRange(trxPool);

                // clear mempool
                Transaction.DeletePool();
            }
            else
            {
                conbaseTrx.Build();
                transactions.Add(conbaseTrx);
            }

            var block = new Block
            {
                Height = height,
                TimeStamp = timestamp,
                PrevHash = prevHash,
                Transactions = GiveOtherInfos(transactions, height),
                Validator = validator
            };
            block.Build();


            //block size
            var str = System.Text.Json.JsonSerializer.Serialize(block);
            block.Size = str.Length;

            // get build time    
            var endTimer = DateTime.UtcNow;
            var buildTime = endTimer - startTimer;
            block.BuildTime = buildTime.Milliseconds;
            // end of    


            // update  balance

            var txns = block.Transactions;

            AccountServiceImpl.UpdateAccountBalance(txns);


            AddBlock(block);
            PrintBlock(block);

            // move all record in trx pool to transactions table
            foreach (var trx in transactions)
            {
                Transaction.Add(trx);
            }


        }

        private static float GetTotalFees(IList<Transaction> txs)
        {
            var totFee = txs.AsEnumerable().Sum(x => x.Fee);
            return totFee;
        }

        private static void PrintBlock(Block block)
        {
            Console.WriteLine("\n===========\nNew Block created");
            Console.WriteLine(" = Height      : {0}", block.Height);
            Console.WriteLine(" = Version     : {0}", block.Version);
            Console.WriteLine(" = Prev Hash   : {0}", block.PrevHash);
            Console.WriteLine(" = Hash        : {0}", block.Hash);
            Console.WriteLine(" = Merkle Hash : {0}", block.MerkleRoot);
            Console.WriteLine(" = Timestamp   : {0}", Utils.ToDateTime(block.TimeStamp));
            Console.WriteLine(" = Difficulty  : {0}", block.Difficulty);
            Console.WriteLine(" = Validator   : {0}", block.Validator);

            Console.WriteLine(" = Number Of Tx: {0}", block.NumOfTx);
            Console.WriteLine(" = Amout       : {0}", block.TotalAmount);
            Console.WriteLine(" = Reward      : {0}", block.TotalReward);
            Console.WriteLine(" = Size        : {0}", block.Size);
            Console.WriteLine(" = Build Time  : {0}", block.BuildTime);


        }
    }
}