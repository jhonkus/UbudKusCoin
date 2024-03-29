﻿// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly Random rnd;

        public BlockFacade()
        {
            rnd = new Random();

            Initialize();
            Console.WriteLine("...... Block innitialized.");
        }

        private void Initialize()
        {
            var blocks = ServicePool.DbService.BlockDb.GetAll();
            if (blocks.Count() < 1)
            {
                // create genesis block
                CreateGenesis();
            }
        }

        /// <summary>
        /// Create genesis block, the first block in blockchain
        /// </summary>
        public void CreateGenesis()
        {
            var startTimer = DateTime.UtcNow.Millisecond;

            //Assume Genesis will start on 2022
            var genesisTicks = new DateTime(2022, 5, 29).Ticks;
            long epochTicks = new DateTime(1970, 1, 1).Ticks;
            long timeStamp = (genesisTicks - epochTicks) / TimeSpan.TicksPerSecond;

            // for genesis bloc we set creator with first of Genesis Account
            var genesisAccounts = ServicePool.FacadeService.Account.GetGenesis();
            var nodeAccountAddresss = ServicePool.WalletService.GetAddress();

            // crate genesis transaction
            var genesisTransactions = ServicePool.FacadeService.Transaction.CreateGenesis();
            var block = new Block
            {
                Height = 1,
                TimeStamp = timeStamp,
                PrevHash = "-",
                Transactions = JsonSerializer.Serialize(genesisTransactions),
                Validator = nodeAccountAddresss,
                Version = 1,
                NumOfTx = genesisTransactions.Count,
                TotalAmount = UkcUtils.GetTotalAmount(genesisTransactions),
                TotalReward = UkcUtils.GetTotalFees(genesisTransactions),
                MerkleRoot = CreateMerkleRoot(genesisTransactions),
                ValidatorBalance = 0,
                Difficulty = 1,
                Nonce = 1
            };

            var blockHash = GetBlockHash(block);
            block.Hash = blockHash;
            block.Signature = ServicePool.WalletService.Sign(blockHash);

            //block size
            block.Size = JsonSerializer.Serialize(block).Length;

            // get build time    
            var endTimer = DateTime.UtcNow.Millisecond;
            block.BuildTime = endTimer - startTimer;

            // update accoiunt table
            ServicePool.FacadeService.Account.UpdateBalanceGenesis(genesisTransactions);

            // add genesis block to blockchain
            ServicePool.DbService.BlockDb.Add(block);
        }

        /// <summary>
        /// Create new Block
        /// </summary>
        public void New(Stake stake)
        {
            var startTimer = DateTime.UtcNow.Millisecond;

            // get transaction from pool
            var poolTransactions = ServicePool.DbService.PoolTransactionsDb.GetAll();
            var wallet = ServicePool.WalletService;

            // get last block before sleep
            var lastBlock = ServicePool.DbService.BlockDb.GetLast();
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
                Transactions = JsonSerializer.Serialize(transactions),
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
            block.Size = JsonSerializer.Serialize(block).Length;

            // get build time    
            var endTimer = DateTime.UtcNow.Millisecond;
            block.BuildTime = (endTimer - startTimer);

            ServicePool.FacadeService.Account.UpdateBalance(transactions);

            // move pool to to transactions db
            ServicePool.FacadeService.Transaction.AddBulk(transactions);

            // clear mempool
            ServicePool.DbService.PoolTransactionsDb.DeleteAll();

            //add block to db
            ServicePool.DbService.BlockDb.Add(block);

            // broadcast block          
            Task.Run(() => ServicePool.P2PService.BroadcastBlock(block));
        }

        public string GetBlockHash(Block block)
        {
            var strSum = block.Version + block.PrevHash + block.MerkleRoot + block.TimeStamp + block.Difficulty + block.Validator;
            return UkcUtils.GenHash(strSum);
        }
        
        private string CreateMerkleRoot(List<Transaction> txns)
        {
            return UkcUtils.CreateMerkleRoot(txns.Select(tx => tx.Hash).ToArray());
        }

        /// <summary>
        /// When receive a block from peer, validate it before insert to DB
        /// </summary>
        public bool IsValidBlock(Block block)
        {
            var lastBlock = ServicePool.DbService.BlockDb.GetLast();
            
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