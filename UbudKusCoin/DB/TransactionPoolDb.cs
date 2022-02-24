// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

using LiteDB;

using UbudKusCoin.Grpc;
using UbudKusCoin.Others;

namespace UbudKusCoin.DB
{


    public class TransactionPoolDb
    {

        private readonly LiteDatabase _db;
        public TransactionPoolDb(LiteDatabase db)
        {
            _db = db;
        }

        // Add to pool
        public void Add(Transaction transaction)
        {
            var txns = GetAll();
            txns.Insert(transaction);
        }

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


        public void DeleteAll()
        {
            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return;
            }
            txns.DeleteAll();
        }

        public ILiteCollection<Transaction> GetAll()
        {
            var col = _db.GetCollection<Transaction>(Constants.TBL_TRANSACTIONS_POOL);
            return col;
        }
    }
}