using System;
using System.Collections.Generic;
using LiteDB;
using UbudKusCoin.Others;
using UbudKusCoin.Grpc;

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
            var pool = GetAll();
            pool.Insert(transaction);
        }

 
        public IEnumerable<Transaction> GetRange(int pageNumber, int resultPerPage)
        {
            var txns = GetAll();
            txns.EnsureIndex(x => x.TimeStamp);
            var query = txns.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }

 
        public void DeleteAll()
        {
            var pool = GetAll();
            pool.DeleteAll();
        }

        public ILiteCollection<Transaction> GetAll()
        {
            var col  = _db.GetCollection<Transaction>(Constants.TBL_TRANSACTIONS_POOL);
            Console.WriteLine(" transactin pool {0}", col.Count());

            return col; 
        }
    }
}