// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using LiteDB;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;

namespace UbudKusCoin.DB
{
    public class PoolTransactionsDb
    {
        private readonly LiteDatabase _db;

        public PoolTransactionsDb(LiteDatabase db)
        {
            _db = db;
        }

        public void Add(Transaction transaction)
        {
            var transactions = GetAll();
            transactions.Insert(transaction);
        }

        public Transaction GetByHash(string hash)
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return null;
            }

            transactions.EnsureIndex(x => x.Hash);
            
            return transactions.FindOne(x => x.Hash == hash);
        }

        public IEnumerable<Transaction> GetRange(int pageNumber, int resultPerPage)
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return null;
            }

            transactions.EnsureIndex(x => x.TimeStamp);
            
            var query = transactions.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            
            return query;
        }


        public void DeleteAll()
        {
            var transactions = GetAll();
            if (transactions is null || transactions.Count() < 1)
            {
                return;
            }

            transactions.DeleteAll();
        }

        public ILiteCollection<Transaction> GetAll()
        {
            return _db.GetCollection<Transaction>(Constants.TBL_TRANSACTIONS_POOL);
        }
    }
}