using System;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using UbudKusCoin.Others;
using UbudKusCoin.Services;
using UbudKusCoin.Grpc;
using UbudKusCoin.DB;

namespace UbudKusCoin.Facade
{

    public class BlockFacade
    {

        public event EventHandler<Block> EventBlockCreated;

        protected virtual void OnEventBlockCreated(Block arg)
        {
            EventBlockCreated?.Invoke(this, arg);
        }
        public BlockFacade()
        {
            Initialize();
            Console.WriteLine("Block initilize ....");
        }


        private void Initialize()
        {

            var blocks = ServicePool.DbService.blockDb.GetAll();

            if (blocks.Count() < 1)
            {
                // start block time
                var startTimer = DateTime.UtcNow.Millisecond;

                // crate genesis transaction
                var genesisTxns = ServicePool.FacadeService.Transaction.CreateGenesis();


                // create genesis block
                var block = CreateGenesis(genesisTxns);

                //convert transaction to json for more easy
                var str = System.Text.Json.JsonSerializer.Serialize(block);
                block.Size = str.Length;

                // get build time    
                var endTimer = DateTime.UtcNow.Millisecond;
                block.BuildTime = (endTimer - startTimer);
                // end of    

                // update accoiunt table
                ServicePool.FacadeService.Transaction.UpdateBalanceGenesis(genesisTxns);

                // add genesis block to blockchain
                ServicePool.DbService.blockDb.Add(block);


            }

            // build report   
            ServicePool.FacadeService.Report.BuildReport();

        }


        /**
        Create genesis block
        **/
        public Block CreateGenesis(List<Transaction> transactions)
        {
            var startTimer = DateTime.UtcNow.Millisecond;

            var ts = Utils.GetTime(); //time creating block

            // for genesis bloc we set creator with first of Genesis Account
            var listGenesis = ServicePool.FacadeService.Account.GetGenesis();
            var block = new Block
            {
                Height = 1,
                TimeStamp = ts,
                PrevHash = "-",
                Transactions = System.Text.Json.JsonSerializer.Serialize(transactions),
                Validator = listGenesis[0].Address, //TODO improve logic
                Version = 1,
                NumOfTx = transactions.Count,
                TotalAmount = Utils.GetTotalAmount(transactions),
                TotalReward = Utils.GetTotalFees(transactions),
                MerkleRoot = CreateMerkleRoot(transactions),
                Difficulty = 1
            };

            var blockHash = GetBlockHash(block);
            block.Hash = blockHash;


            //block size
            var str = JsonSerializer.Serialize(block);
            block.Size = str.Length;

            // get build time    
            var endTimer = DateTime.UtcNow.Millisecond;
            block.BuildTime = (endTimer - startTimer);
            // end of    

            //triger event block created
            OnEventBlockCreated(block);

            return block;
        }

        public void CreateNew()
        {

            // start build time
            var startTimer = DateTime.UtcNow.Millisecond;
            Console.WriteLine("startTimer{0}", startTimer);
            // get transaction from pool
            var txnsInPool = ServicePool.DbService.transactionsPooldb.GetAll();
            Console.WriteLine("txnsInPool {0}", txnsInPool);
            //// get last block to get prev hash and last height
            var lastBlock = ServicePool.DbService.blockDb.GetLast();
            Console.WriteLine("lastBlock {0}", lastBlock);

            var nextHeight = lastBlock.Height + 1;
            Console.WriteLine("nextHeight {0}", nextHeight);


            var timestamp = Utils.GetTime();
            Console.WriteLine("timestamp {0}", timestamp);

            var prevHash = lastBlock.Hash;
            Console.WriteLine("prevHash {0}", prevHash);


            var validator = ServicePool.FacadeService.Stake.GetValidator();
            Console.WriteLine("validator {0}", validator);

            var transactions = ServicePool.FacadeService.Transaction.GetForMinting(nextHeight); // JsonConvert.SerializeObject(new List<Transaction>());
            Console.WriteLine("transactions {0}", transactions);

            var block = new Block
            {
                Height = nextHeight,
                TimeStamp = timestamp,
                PrevHash = prevHash,
                Transactions = System.Text.Json.JsonSerializer.Serialize(transactions),
                Difficulty = GetDifficullty(),
                Validator = validator.Address,
                Version = 1,
                NumOfTx = transactions.Count,
                TotalAmount = Utils.GetTotalAmount(transactions),
                TotalReward = Utils.GetTotalFees(transactions),
                MerkleRoot = CreateMerkleRoot(transactions),
            };
            var blockHash = GetBlockHash(block);
            block.Hash = blockHash;


            //block size
            var str = System.Text.Json.JsonSerializer.Serialize(block);
            block.Size = str.Length;

            // get build time    
            var endTimer = DateTime.UtcNow.Millisecond;
            var buildTime = endTimer - startTimer;
            block.BuildTime = buildTime;
            // end of    

            ServicePool.DbService.blockDb.Add(block);

            ServicePool.FacadeService.Transaction.UpdateBalance(transactions);

            // move pool to to transactions db
            ServicePool.FacadeService.Transaction.AddBulk(transactions);

            // clear mempool
            ServicePool.DbService.transactionsPooldb.DeleteAll();

            //triger event block created
            // OnEventBlockCreated(block);
        }

        // public void BuildNewBlockdddd()
        // {
        //     // get transaction from pool
        //     var trxPool = ServicePool.DbService.PoolDb.GetAll();

        //     //// get last block to get prev hash and last height
        //     var lastBlock = db.GetLast();
        //     var height = lastBlock.Height + 1;
        //     var timestamp = Utils.GetTime();
        //     var prevHash = lastBlock.Hash;
        //     var validator = ServicePool.DbService.StakeDb.GetValidator();


        //     var transactions = new List<Transaction>(); // JsonConvert.SerializeObject(new List<Transaction>());


        //     // validator will get coin reward from genesis account
        //     // to keep total coin in Blockchain not changed
        //     var conbaseTrx = new Transaction
        //     {
        //         Amount = 0,
        //         Recipient = "UKC_QPQY9wHP0jxi/0c/YRlch2Uk5ur/T8lcOaawqyoe66o=",
        //         Fee = Constants.COINT_REWARD,
        //         TimeStamp = timestamp,
        //         Sender = "UKC_rcyChuW7cQcIVoKi1LfSXKfCxZBHysTwyPm88ZsN0BM="
        //     };

        //     if (trxPool.Count() > 0)
        //     {
        //         //Get all tx from pool
        //         conbaseTrx.Recipient = validator.Address;
        //         conbaseTrx.Amount = GetTotalFees(trxPool.FindAll().ToList());
        //         conbaseTrx.Build();

        //         transactions.Add(conbaseTrx);
        //         transactions.AddRange(trxPool.FindAll());

        //         // clear mempool
        //         trxPool.DeleteAll();
        //     }
        //     else
        //     {
        //         conbaseTrx.Build();
        //         transactions.Add(conbaseTrx);
        //     }


        //     var block = new Block
        //     {
        //         Height = height,
        //         TimeStamp = timestamp,
        //         PrevHash = prevHash,
        //         Transactions = System.Text.Json.JsonSerializer.Serialize(transactions),
        //         Difficulty = GetDifficullty(),
        //         Validator = validator.Address,
        //         Version = 1,
        //         NumOfTx = transactions.Count,
        //         TotalAmount = Utils.GetTotalAmount(transactions),
        //         TotalReward = Utils.GetTotalFees(transactions),
        //         MerkleRoot = GetMerkleRoot(transactions),
        //     };
        //     var blockHash = GetBlockHash(block);
        //     block.Hash = blockHash;

        //     //triger event block created
        //     OnEventBlockCreated(block);

        //     ServiceManager.DbService.Blocks.AddBlock(block);

        //     // PrintBlock(block);

        //     // move all record in trx pool to transactions table
        //     foreach (var trx in transactions)
        //     {
        //         Transaction.Add(trx);
        //     }
        // }



        public string GetBlockHash(Block block)
        {
            var strSum = block.Version + block.PrevHash + block.MerkleRoot + block.TimeStamp + block.Difficulty + block.Validator;
            var hash = Utils.GenHash(strSum);
            return hash;
        }

        private string CreateMerkleRoot(List<Transaction> txns)
        {
            // List<Transaction> txList = JsonConvert.DeserializeObject<List<Transaction>>(jsonTxs);
            var txsHash = new List<string>();
            foreach (var tx in txns)
            {
                txsHash.Add(tx.Hash);
            }
            var hashRoot = Utils.CreateMerkleRoot(txsHash.ToArray());
            return hashRoot;
        }
        private int GetAdjustedDifficulty(Block latestBlock)
        {

            var blocks = ServicePool.DbService.blockDb.GetAll();
            Console.WriteLine("==== GetAdjustedDifficulty");
            var prevAdjustmentBlock = ServicePool.DbService.blockDb.GetByHeight(blocks.Count() - Constants.DIFFICULTY_ADJUSTMENT_INTERVAL);

            Console.WriteLine("prevAdjustmentBlock: " + prevAdjustmentBlock.TimeStamp);
            Console.WriteLine("latestBlock: " + latestBlock.TimeStamp);

            var timeExpected = Constants.BLOCK_GENERATION_INTERVAL * Constants.DIFFICULTY_ADJUSTMENT_INTERVAL;
            Console.WriteLine("timeExpected:" + timeExpected);

            var timeTaken = latestBlock.TimeStamp - prevAdjustmentBlock.TimeStamp;
            Console.WriteLine("timeTaken:" + timeTaken);

            if (timeTaken < (timeExpected / 2))
            {
                return prevAdjustmentBlock.Difficulty + 1;
            }
            else if (timeTaken > timeExpected * 2)
            {
                return prevAdjustmentBlock.Difficulty - 1;
            }
            else
            {
                return prevAdjustmentBlock.Difficulty;
            }

        }

        private int GetDifficullty()
        {
            var latestBlock = ServicePool.DbService.blockDb.GetLast();
            Console.WriteLine("latestBlock.Height:" + latestBlock.Height);
            // Console.WriteLine("Constants.DIFFICULTY_ADJUSTMENT_INTERVAL:" + Constants.DIFFICULTY_ADJUSTMENT_INTERVAL);

            if (latestBlock.Height % Constants.DIFFICULTY_ADJUSTMENT_INTERVAL == 0 && latestBlock.Height != 0)
            {
                return GetAdjustedDifficulty(latestBlock);
            }
            else
            {
                return latestBlock.Difficulty;
            }
        }



    }
}
