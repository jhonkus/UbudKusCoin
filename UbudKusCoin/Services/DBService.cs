// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using UbudKusCoin.DB;

namespace UbudKusCoin.Services
{
    public class DbService
    {
        private readonly LmdbStore DB_BLOCK;
        private readonly LmdbStore DB_ACCOUNT;
        private readonly LmdbStore DB_TRANSACTION;
        private readonly LmdbStore DB_TRANSACTION_POOL;
        private readonly LmdbStore DB_PEER;
        private readonly LmdbStore DB_STAKE;

        public BlockDb BlockDb { get; set; }
        public TransactionDb TransactionDb { get; set; }
        public PeerDb PeerDb { get; set; }

        public AccountDb AccountDb { get; set; }
        public PoolTransactionsDb PoolTransactionsDb { get; set; }
        public StakeDb StakeDb { get; set; }

        // I use multiple databases, to minimize database size for transaction, block
        // size will smaller for each database
        public DbService()
        {
            var dataDirectory = DotNetEnv.Env.GetString("DB_FILES_PATH", "DbFiles");
            //create db folder
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            DB_BLOCK = InitializeDatabase(Path.Combine(dataDirectory, "block.mdb"));
            DB_ACCOUNT = InitializeDatabase(Path.Combine(dataDirectory, "account.mdb"));
            DB_TRANSACTION = InitializeDatabase(Path.Combine(dataDirectory, "transaction.mdb"));
            DB_TRANSACTION_POOL = InitializeDatabase(Path.Combine(dataDirectory, "transaction_pool.mdb"));
            DB_STAKE = InitializeDatabase(Path.Combine(dataDirectory, "stake.mdb"));
            DB_PEER = InitializeDatabase(Path.Combine(dataDirectory, "peer.mdb"));
        }

        private LmdbStore InitializeDatabase(string path)
        {
            return new LmdbStore(path);
        }

        public void Start()
        {
            Console.WriteLine("... DB Service is starting");
            BlockDb = new BlockDb(DB_BLOCK);
            AccountDb = new AccountDb(DB_ACCOUNT);
            TransactionDb = new TransactionDb(DB_TRANSACTION);
            PoolTransactionsDb = new PoolTransactionsDb(DB_TRANSACTION_POOL);
            StakeDb = new StakeDb(DB_STAKE);
            PeerDb = new PeerDb(DB_PEER);
            Console.WriteLine("...... DB Service is ready");
        }

        public void Stop()
        {
            Console.WriteLine("... DB Service is stopping...");
            DB_BLOCK.Dispose();
            DB_STAKE.Dispose();
            DB_TRANSACTION.Dispose();
            DB_TRANSACTION_POOL.Dispose();
            DB_PEER.Dispose();
            DB_ACCOUNT.Dispose();
            Console.WriteLine("... DB Service has been disposed");
        }
    }
}