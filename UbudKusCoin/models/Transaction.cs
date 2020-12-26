using System;
using LiteDB;
using DB;
using System.Collections.Generic;

namespace Models
{

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
            var transactions = GetAll();
            transactions.Insert(transaction);
        }

        public static ILiteCollection<Transaction> GetPool()
        {
            var coll = DbAccess.DB.GetCollection<Transaction>(DbAccess.TBL_TRANSACTION_POOL);
            return coll;
        }

        public static ILiteCollection<Transaction> GetAll()
        {
            var coll = DbAccess.DB.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            return coll;
        }

        /**
        * get transaction list by name
        */
        public static IEnumerable<Transaction> GetHistory(string name)
        {
            var coll = DbAccess.DB.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            var transactions = coll.Find(x => x.Sender == name || x.Recipient == name);
            return transactions;
        }


        /**
        * Get balance by name
        */
        public static double GetBalance(string name)
        {
            double balance = 0;
            double spending = 0;
            double income = 0;

            var collection = GetAll();
            var transactions = collection.Find(x => x.Sender == name || x.Recipient == name);

            foreach (Transaction trx in transactions)
            {
                var sender = trx.Sender;
                var recipient = trx.Recipient;

                if (name.ToLower().Equals(sender.ToLower()))
                {
                    spending += trx.Amount + trx.Fee;
                }

                if (name.ToLower().Equals(recipient.ToLower()))
                {
                    income += trx.Amount;
                }

                balance = income - spending;
            }

            return balance;
        }

    }

}