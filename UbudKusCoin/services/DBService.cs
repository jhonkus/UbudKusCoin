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
        private readonly LiteDatabase DB_REPORT;
        private readonly LiteDatabase DB_STAKE;

        public BlockDb blockDb;
        public TransactionDb transactionDb;
        public ReportDb reportDb;

        public AccountDb accountDb;
        public TransactionPoolDb transactionsPooldb;
        public StakeDb stakeDb;



        public DbService(string name)
        {
            this.DB_BLOCK = new LiteDatabase(@"DbFiles//" + name + "_block.db");
            this.DB_ACCOUNT = new LiteDatabase(@"DbFiles//" + name + "_account.db");
            this.DB_TRANSACTION = new LiteDatabase(@"DbFiles//" + name + "_transaction.db");
            this.DB_TRANSACTION_POOL = new LiteDatabase(@"DbFiles//" + name + "_transaction_pool.db");
            this.DB_STAKE = new LiteDatabase(@"DbFiles//" + name + "_stake.db");
            this.DB_REPORT = new LiteDatabase(@"DbFiles//" + name + "_report.db");

            this.blockDb = new BlockDb(this.DB_BLOCK);
            this.accountDb = new AccountDb(this.DB_ACCOUNT);
            this.transactionDb = new TransactionDb(this.DB_ACCOUNT);
            this.transactionsPooldb = new TransactionPoolDb(this.DB_TRANSACTION_POOL);
            this.stakeDb = new StakeDb(this.DB_STAKE);
            this.reportDb = new ReportDb(this.DB_REPORT);
        }
        /**
        it will create db with name node.db
        **/

        public void Start()
        {

            Console.WriteLine("Db started");
        }
        /**
        * Close database when app closed
        **/
        public void Stop()
        {
            DB_BLOCK.Dispose();
            DB_STAKE.Dispose();
            DB_TRANSACTION.Dispose();
            DB_TRANSACTION_POOL.Dispose();
            DB_REPORT.Dispose();
            DB_ACCOUNT.Dispose();
            Console.WriteLine("Closing db");
        }

    }
}