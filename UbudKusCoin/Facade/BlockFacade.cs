﻿// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;
namespace UbudKusCoin.Facade
{

    public class BlockFacade
    {

        public BlockFacade()
        {
            Initialize();
            Console.WriteLine("...... Block initilized.");
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


            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Start();
            // start build time
            var startTimer = DateTime.UtcNow.Millisecond;


            // get transaction from pool
            var txnsInPool = ServicePool.DbService.transactionsPooldb.GetAll();


            var lastTimestamp = 0L;
            var wallet = ServicePool.WalletService;


            var lastBlock = ServicePool.DbService.blockDb.GetLast();
            var nextHeight = lastBlock.Height + 1;
            var prevHash = lastBlock.Hash;
            var validator = ServicePool.FacadeService.Stake.GetValidator();
            var transactions = ServicePool.FacadeService.Transaction.GetForMinting(nextHeight);
            var difficulty = GetDifficullty();
            var minterAddress = wallet.GetAddress();
            var minterAccount = ServicePool.FacadeService.Account.GetByAddress(minterAddress);
            var minterBalance = minterAccount.Balance;
            Random rnd = new Random();

            while (true)
            {
                Thread.Sleep(100); //just for make delay so not fo fast
                var timestamp = Utils.GetTime();

                if (lastTimestamp != timestamp)
                {

                    if (IsStakingMeetRule(prevHash, wallet.GetKeyPair().PublicKeyHex, timestamp, minterBalance, difficulty, nextHeight))
                    {
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
                            Nonce = rnd.Next(),
                        };

                        var blockHash = GetBlockHash(block);
                        block.Hash = blockHash;


                        //block size
                        var str = System.Text.Json.JsonSerializer.Serialize(block);
                        block.Size = str.Length;

                        // get build time    
                        stopWatch.Stop();

                        // Get the elapsed time as a TimeSpan value.
                        TimeSpan ts = stopWatch.Elapsed;
                        block.BuildTime = (ts.Milliseconds);
                        // end of    

                        ServicePool.DbService.blockDb.Add(block);
                        ServicePool.FacadeService.Transaction.UpdateBalance(transactions);

                        // move pool to to transactions db
                        ServicePool.FacadeService.Transaction.AddBulk(transactions);

                        // clear mempool
                        ServicePool.DbService.transactionsPooldb.DeleteAll();



                        //triger event block created
                        ServicePool.EventService.OnEventBlockCreated(block);

                        //exit from loop
                        break;
                    }

                    lastTimestamp = timestamp;
                }
            }



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

        public bool IsStakingMeetRule(string prevhash, string address, long timestamp, double balance, int difficulty, long height)
        {
            difficulty = difficulty + 1;
            var big1 = new BigInteger(Math.Pow(2, 256));
            var big2 = BigInteger.Multiply(big1, new BigInteger(balance));
            var balanceOverDifficulty = BigInteger.Divide(big2, difficulty);
            var stakingHash = Utils.GenHashBytes(prevhash + address + timestamp);
            var decimalStakingHash = new BigInteger(stakingHash);
            var difference = BigInteger.Min(balanceOverDifficulty, decimalStakingHash);
            return difference >= 0;
        }


        public bool isValidBlock(Block block)
        {
            var lastBlock = ServicePool.DbService.blockDb.GetLast();

            //compare block height with prev
            if (block.Height != (lastBlock.Height + 1))
            {
                return false;
            }

            //compare block hash with prev
            if (block.PrevHash != lastBlock.Hash)
            {
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
