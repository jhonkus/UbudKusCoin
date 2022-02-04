using System.Collections.Generic;
using LiteDB;
using UbudKusCoin.Others;
using UbudKusCoin.Grpc;

namespace UbudKusCoin.DB
{


    public class TransactionDb
    {

        private readonly LiteDatabase _db;
        public TransactionDb(LiteDatabase db)
        {
            this._db = db;
        }

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


        /**
          * Get balance by address
          */
        public double GetBalanceByScan(string address)
        {
            double balance = 0;
            double spending = 0;
            double income = 0;

            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return 0;
            }
            var transactions = txns.Find(x => x.Sender == address || x.Recipient == address);

            foreach (Transaction trx in transactions)
            {
                var sender = trx.Sender;
                var recipient = trx.Recipient;

                if (address.ToLower().Equals(sender.ToLower()))
                {
                    spending += trx.Amount + trx.Fee;
                }

                if (address.ToLower().Equals(recipient.ToLower()))
                {
                    income += trx.Amount;
                }

                balance = income - spending;
            }

            return balance;
        }

        /**
        * get all transaction by address
        */
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

        /**
         * get a transaction by hash
         */
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

        public Transaction GetFirst()
        {

            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return null;
            }
            txns.EnsureIndex(x => x.TimeStamp);
            var trx = txns.FindOne(Query.All(Query.Ascending));
            return trx;
        }

        /**
           * get transactions by block height
           */
        public IEnumerable<Transaction> GetRangeByHeight(long height, int pageNumber, int resultPerPage)
        {
            var txns = GetAll();
            if (txns is null || txns.Count() < 1)
            {
                return null;
            }
            txns.EnsureIndex(x => x.TimeStamp);
            var query = txns.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Where(x => x.Height == height)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;

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