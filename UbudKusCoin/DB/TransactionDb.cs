// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.


using System.Collections.Generic;
using LiteDB;
using UbudKusCoin.Others;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;

namespace UbudKusCoin.DB
{

    /// <summary>
    /// Transaction DB, for add, update transaction
    /// </summary>
    public class TransactionDb
    {

        private readonly LiteDatabase _db;
        public TransactionDb(LiteDatabase db)
        {
            this._db = db;
        }

        /// <summary>
        /// Add some transaction in smae time
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public string AddBulk(List<Transaction> transactions)
        {
            try
            {
                var coll = GetAll();
                coll.InsertBulk(transactions);
                return "success";
            }
            catch
            {
                return "fail";
            }
        }

        /// <summary>
        /// Add a transaction
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public string Add(Transaction transaction)
        {
            try
            {
                var txns = GetAll();
                txns.Insert(transaction);
                return "success";
            }
            catch
            {
                return "fail";
            }
        }


        /// <summary>
        /// Get A;l Transactions by Address and with paging
        /// </summary>
        /// <param name="address"></param>
        /// <param name="pageNumber"></param>
        /// <param name="resultPerPage"></param>
        /// <returns></returns>
        public IEnumerable<Transaction> GetRangeByAddress(string address, int pageNumber, int resultPerPage)
        {
            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return null;
            }
            txns.EnsureIndex(x => x.Sender);
            txns.EnsureIndex(x => x.Recipient);
            var query = txns.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Where(x => x.Sender == address || x.Recipient == address)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }

        /// <summary>
        /// Get Transaction by Hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Transaction GetByHash(string hash)
        {
            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return null;
            }
            txns.EnsureIndex(x => x.Hash);
            var transaction = txns.FindOne(x => x.Hash == hash);
            return transaction;
        }


        /**
        * get transactions  
        */
        public IEnumerable<Transaction> GetRange(int pageNumber, int resultPerPage)
        {
            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return null;
            }
            txns.EnsureIndex(x => x.TimeStamp);
            var query = txns.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }

        public IEnumerable<Transaction> GetLasts(int num)
        {
            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return null;
            }
            txns.EnsureIndex(x => x.TimeStamp);
            var query = txns.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Limit(num).ToList();
            return query;
        }
        /**
       get one transaction by address
       */
        public Transaction GetByAddress(string address)
        {
            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return null;
            }
            txns.EnsureIndex(x => x.TimeStamp);
            var transaction = txns.FindOne(x => x.Sender == address || x.Recipient == address);
            return transaction;
        }

        private ILiteCollection<Transaction> GetAll()
        {
            var coll = this._db.GetCollection<Transaction>(Constants.TBL_TRANSACTIONS);
            return coll;
        }

    }
}