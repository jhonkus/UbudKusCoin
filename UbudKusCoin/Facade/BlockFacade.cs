// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Text.Json;

using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;
using System.Threading.Tasks;

namespace UbudKusCoin.Facade
{

    public class BlockFacade
    {

        //minter will selected by random
        private Random rnd;
        public BlockFacade()
        {
            this.rnd = new Random();

            Initialize();
            Console.WriteLine("...... Block initilized.");
        }


        private void Initialize()
        {
            var blocks = ServicePool.DbService.blockDb.GetAll();
            if (blocks.Count() < 1)
            {
                // create genesis block
                CreateGenesis();
            }

        }

        /// <summary>
        /// Create genesis block, the first block in blockchain
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public void CreateGenesis()
        {
            var startTimer = DateTime.UtcNow.Millisecond;

            //Assume Genesis will start on 2022
            var genesisTicks = new DateTime(2022, 5, 29).Ticks;
            long epochTicks = new DateTime(1970, 1, 1).Ticks;

            long timeStamp = ((genesisTicks - epochTicks) / TimeSpan.TicksPerSecond);

            // for genesis bloc we set creator with first of Genesis Account
            var listGenesis = ServicePool.FacadeService.Account.GetGenesis();
            var nodeAccountAddresss = ServicePool.WalletService.GetAddress();

            // crate genesis transaction
            var genesisTxns = ServicePool.FacadeService.Transaction.CreateGenesis();

            var block = new Block
            {
                Height = 1,
                TimeStamp = timeStamp,
                PrevHash = "-",
                Transactions = System.Text.Json.JsonSerializer.Serialize(genesisTxns),
                Validator = nodeAccountAddresss,
                Version = 1,
                NumOfTx = genesisTxns.Count,
                TotalAmount = UbudKusCoin.Others.UkcUtils.GetTotalAmount(genesisTxns),
                TotalReward = UbudKusCoin.Others.UkcUtils.GetTotalFees(genesisTxns),
                MerkleRoot = CreateMerkleRoot(genesisTxns),
                ValidatorBalance = 0,
                Difficulty = 1,
                Nonce = 1
            };

            var blockHash = GetBlockHash(block);
            block.Hash = blockHash;
            block.Signature = ServicePool.WalletService.Sign(blockHash);

            //block size
            var str = JsonSerializer.Serialize(block);
            block.Size = str.Length;

            // get build time    
            var endTimer = DateTime.UtcNow.Millisecond;
            block.BuildTime = (endTimer - startTimer);

            // Console.WriteLine("=== genesis {0}", block);
            // end of    

            // update accoiunt table
            ServicePool.FacadeService.Account.UpdateBalanceGenesis(genesisTxns);


            // add genesis block to blockchain
            ServicePool.DbService.blockDb.Add(block);

        }


        /// <summary>
        /// Create new Block
        /// </summary>
        public void New(Stake stake)
        {

          

            var startTimer = DateTime.UtcNow.Millisecond;

            // get transaction from pool
            var txnsInPool = ServicePool.DbService.transactionsPooldb.GetAll();

       

            var wallet = ServicePool.WalletService;


            // get last block before sleep
            var lastBlock = ServicePool.DbService.blockDb.GetLast();
   

            var nextHeight = lastBlock.Height + 1;
            var prevHash = lastBlock.Hash;

            // var validator = ServicePool.FacadeService.Stake.GetValidator();

            var transactions = ServicePool.FacadeService.Transaction.GetForMinting(nextHeight);
 



            var minterAddress = stake.Address;


            var minterBalance = stake.Amount;
       

            var timestamp = UkcUtils.GetTime();

            var block = new Block
            {
                Height = nextHeight,
                TimeStamp = timestamp,
                PrevHash = prevHash,
                Transactions = System.Text.Json.JsonSerializer.Serialize(transactions),
                Difficulty = 1, //GetDifficullty(),
                Validator = minterAddress,
                Version = 1,
                NumOfTx = transactions.Count,
                TotalAmount = UkcUtils.GetTotalAmount(transactions),
                TotalReward = UkcUtils.GetTotalFees(transactions),
                MerkleRoot = CreateMerkleRoot(transactions),
                ValidatorBalance = minterBalance,
                Nonce = rnd.Next(100000),

            };
            var blockHash = GetBlockHash(block);
            block.Hash = blockHash;
            block.Signature = ServicePool.WalletService.Sign(blockHash);


            UkcUtils.PrintBlock(block);

            //block size
            var str = System.Text.Json.JsonSerializer.Serialize(block);
            block.Size = str.Length;

            // get build time    
            var endTimer = DateTime.UtcNow.Millisecond;
            // Get the elapsed time as a TimeSpan value.

            block.BuildTime = (endTimer - startTimer);
            // end of    

            ServicePool.FacadeService.Account.UpdateBalance(transactions);

            // move pool to to transactions db
            ServicePool.FacadeService.Transaction.AddBulk(transactions);

            // clear mempool
            ServicePool.DbService.transactionsPooldb.DeleteAll();


            //add block to db
            ServicePool.DbService.blockDb.Add(block);

            // broadcast block          
            Task.Run(() =>  ServicePool.P2PService.BroadcastBlock(block));

        }

        public string GetBlockHash(Block block)
        {
            var strSum = block.Version + block.PrevHash + block.MerkleRoot + block.TimeStamp + block.Difficulty + block.Validator;
            var hash = UkcUtils.GenHash(strSum);
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
            var hashRoot = UkcUtils.CreateMerkleRoot(txsHash.ToArray());
            return hashRoot;
        }



        /// <summary>
        /// When receive a block from peer, validate it before insert to DB
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public bool IsValidBlock(Block block)
        {
            var lastBlock = ServicePool.DbService.blockDb.GetLast();
            //compare block height with prev
            if (block.Height != (lastBlock.Height + 1))
            {
                return false;
            }

            //compare block hash with prev block hash
            if (block.PrevHash != lastBlock.Hash)
            {
                return false;
            }

            //validate hash
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
