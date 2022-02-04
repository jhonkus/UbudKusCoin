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

        // public event EventHandler<Block> EventBlockCreated;

        // protected virtual void OnEventBlockCreated(Block arg)
        // {
        //     EventBlockCreated?.Invoke(this, arg);
        // }
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

                ServicePool.EventService.OnEventBlockCreated(block);
            }



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
                ValidatorBalance = 0,
                Difficulty = 1,
                Nonce = 1
            };

            var blockHash = GetBlockHash(block);
            block.Hash = blockHash;


            //block size
            var str = JsonSerializer.Serialize(block);
            block.Size = str.Length;

            // get build time    
            var endTimer = DateTime.UtcNow.Millisecond;
            block.BuildTime = (endTimer - startTimer);

            // Console.WriteLine("=== genesis {0}", block);
            // end of    

            return block;
        }

        public void CreateNew()
        {

            // start build time
            var startTimer = DateTime.UtcNow.Millisecond;

            // get transaction from pool
            var txnsInPool = ServicePool.DbService.transactionsPooldb.GetAll();

            //// get last block to get prev hash and last height
            var lastBlock = ServicePool.DbService.blockDb.GetLast();
            var nextHeight = lastBlock.Height + 1;
            var timestamp = Utils.GetTime();
            var prevHash = lastBlock.Hash;
            var validator = ServicePool.FacadeService.Stake.GetValidator();
            var transactions = ServicePool.FacadeService.Transaction.GetForMinting(nextHeight);

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
                ValidatorBalance = validator.Amount,
                Nonce = 1,
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
            ServicePool.EventService.OnEventBlockCreated(block);

        }

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
            var adjustment = blocks.Count() - Constants.DIFFICULTY_ADJUSTMENT_INTERVAL;
            if (adjustment <= 0)
            {
                adjustment = 1;
            }

            var prevAdjustmentBlock = ServicePool.DbService.blockDb.GetByHeight(adjustment);
            var timeExpected = Constants.BLOCK_GENERATION_INTERVAL * Constants.DIFFICULTY_ADJUSTMENT_INTERVAL;

            var timeTaken = latestBlock.TimeStamp - prevAdjustmentBlock.TimeStamp;

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

        public bool isValidBlock(Block block)
        {
            var lastBlock = ServicePool.DbService.blockDb.GetLast();

            //compare block height with prev
            if (block.Height != (lastBlock.Height +1)){
                return false;
            } 

            //compare block hash with prev
            if (block.PrevHash != lastBlock.Hash){
                return false;
            } 

            //compare hash
            if (block.Hash != GetBlockHash(block))
            {
                return false;
            }

            //compare timestamp
            if (block.TimeStamp <= lastBlock.TimeStamp)
            {
                return false;
            }

            return true;
            
        }

    }
}
