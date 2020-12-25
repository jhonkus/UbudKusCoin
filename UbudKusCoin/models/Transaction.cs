using System;
using LiteDB;
using DB;

namespace Models
{

    [Serializable]
    public class Transaction
    {
        public long TimeStamp { get; set; }
        public string Sender { set; get; }
        public string Recipient { set; get; }
        public double Amount { set; get; }
        public double Fee { set; get; }


        public static void AddToPool(Transaction transaction)
        {
            var trxPool = GetPool();
            trxPool.Insert(transaction);
        }

        public static void Add(Transaction transaction)
        {
            var trxPool = GetAll();
            trxPool.Insert(transaction);
        }

        public static ILiteCollection<Models.Transaction> GetPool()
        {
            var coll = DbAccess.DB.GetCollection<Models.Transaction>(DbAccess.TBL_TRANSACTION_POOL);
            return coll;
        }

        public static ILiteCollection<Transaction> GetAll()
        {
            var coll = DbAccess.DB.GetCollection<Models.Transaction>(DbAccess.TBL_TRANSACTIONS);
            return coll;
        }

    }

}