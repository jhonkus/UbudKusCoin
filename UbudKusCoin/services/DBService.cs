// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

using LiteDB;

using UbudKusCoin.DB;

namespace UbudKusCoin.Services
{
    public class DbService
    {
        private readonly LiteDatabase DB_BLOCK;
        private readonly LiteDatabase DB_ACCOUNT;
        private readonly LiteDatabase DB_TRANSACTION;
        private readonly LiteDatabase DB_TRANSACTION_POOL;
        private readonly LiteDatabase DB_PEER;
        private readonly LiteDatabase DB_STAKE;

        public BlockDb blockDb;
        public TransactionDb transactionDb;
        public PeerDb peerDb;

        public AccountDb accountDb;
        public TransactionPoolDb transactionsPooldb;
        public StakeDb stakeDb;


        // I use multiple database, to minimize database size for transaction, block
        // size will smaller for each database

        public DbService()
        {
            //create db folder
            try
            {
                System.IO.Directory.CreateDirectory(@"DbFiles");
            }
            catch { }

            this.DB_BLOCK = new LiteDatabase(@"DbFiles//block.db");
            this.DB_ACCOUNT = new LiteDatabase(@"DbFiles//account.db");
            this.DB_TRANSACTION = new LiteDatabase(@"DbFiles//transaction.db");
            this.DB_TRANSACTION_POOL = new LiteDatabase(@"DbFiles//transaction_pool.db");
            this.DB_STAKE = new LiteDatabase(@"DbFiles//stake.db");
            this.DB_PEER = new LiteDatabase(@"DbFiles//peer.db");

        }

        public void Start()
        {
            Console.WriteLine("... DB Servie is starting");
            this.blockDb = new BlockDb(this.DB_BLOCK);
            this.accountDb = new AccountDb(this.DB_ACCOUNT);
            this.transactionDb = new TransactionDb(this.DB_ACCOUNT);
            this.transactionsPooldb = new TransactionPoolDb(this.DB_TRANSACTION_POOL);
            this.stakeDb = new StakeDb(this.DB_STAKE);
            this.peerDb = new PeerDb(this.DB_PEER);
            Console.WriteLine("...... DB Servie is ready");
        }


        public void Stop()
        {
            DB_BLOCK.Dispose();
            DB_STAKE.Dispose();
            DB_TRANSACTION.Dispose();
            DB_TRANSACTION_POOL.Dispose();
            DB_PEER.Dispose();
            DB_ACCOUNT.Dispose();
            Console.WriteLine("db Disposed");
        }

    }
}