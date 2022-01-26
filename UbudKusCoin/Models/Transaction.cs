using System;
using LiteDB;
using EllipticCurve;
using System.Collections.Generic;
using UbudKusCoin.Services;
using UbudKusCoin.Others;

namespace UbudKusCoin.Models
{

    public class Transaction
    {
        public string Hash { get; set; }
        public long TimeStamp { get; set; }
        public string Sender { set; get; }
        public string Recipient { set; get; }
        public double Amount { set; get; }
        public float Fee { set; get; }
        public long Height { get; set; }

        private static List<Transaction> mempool = new List<Transaction>();

        public static void AddToPool(Transaction transaction)
        {
            if (GetPool() is null)
            {
                mempool = new List<Transaction>();
            }
            // var trxPool = GetPool();
            // trxPool.Insert(transaction);
            mempool.Add(transaction);
        }

        public static void Add(Transaction transaction)
        {
            var transactions = GetAll();
            transactions.Insert(transaction);
        }


        public static List<Transaction> GetPool()
        {
            if (mempool is null)
            {
                return new List<Transaction>();
            }
            // var coll = DbAccess.DB_POOL.GetCollection<Transaction>(DbAccess.TBL_TRANSACTION_POOL);
            // return coll;
            return mempool;
        }

        public static void DeletePool()
        {
            // var coll = DbAccess.DB_POOL.GetCollection<Transaction>(DbAccess.TBL_TRANSACTION_POOL);
            // return coll;
            mempool = new List<Transaction>();
        }

        public static ILiteCollection<Transaction> GetAll()
        {
            var coll = DbAccess.DB_TXNS.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            return coll;
        }

        /**

        get one transaction for speed, to check if address have tnx
        */
        public static Transaction GetOneTxnByAddress(string address)
        {
            var coll = DbAccess.DB_TXNS.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            coll.EnsureIndex(x => x.TimeStamp);
            var transaction = coll.FindOne(x => x.Sender == address || x.Recipient == address);
            return transaction;
        }

        /**
        * get transaction list by address
        */
        public static IEnumerable<Transaction> GetAccountTransactions(string address)
        {
            var coll = DbAccess.DB_TXNS.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            coll.EnsureIndex(x => x.Sender);
            coll.EnsureIndex(x => x.Recipient);
            var query = coll.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Where(x => x.Sender == address || x.Recipient == address)
                .Limit(50).ToList();
            return query;
        }

        /**
         * get a transaction by hash
         */
        public static Transaction GetTxnByHash(string hash)
        {
            var coll = DbAccess.DB_TXNS.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            coll.EnsureIndex(x => x.TimeStamp);
            var transaction = coll.FindOne(x => x.Hash == hash);
            return transaction;
        }


        /**
  * get a transaction by hash
  */
        public static Transaction GetTxnsByHeight(string hash)
        {
            var coll = DbAccess.DB_TXNS.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            coll.EnsureIndex(x => x.TimeStamp);
            var transaction = coll.FindOne(x => x.Hash == hash);
            return transaction;
        }

        /**
           * get transaction list by block height
           */
        public static IEnumerable<Transaction> GetTxnsByHeight(long height)
        {
            // var coll = DbAccess.DB.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            // coll.EnsureIndex(x => x.Height);
            // var transactions = coll.Find(x => x.Height == height);
            // return transactions;

            var coll = DbAccess.DB_TXNS.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            coll.EnsureIndex(x => x.TimeStamp);
            var query = coll.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Where(x => x.Height == height)
                .Limit(50).ToList();
            return query;

        }

        /**
        * get transaction list 
        */
        // public static IEnumerable<Transaction> GetPendingTransactions2(int pageNumber, int resultPerPage)
        // {
        //     var coll = DbAccess.DB_POOL.GetCollection<Transaction>(DbAccess.TBL_TRANSACTION_POOL);
        //     coll.EnsureIndex(x => x.TimeStamp);
        //     var query = coll.Query()
        //         .OrderByDescending(x => x.TimeStamp)
        //         .Offset((pageNumber - 1) * resultPerPage)
        //         .Limit(resultPerPage).ToList();
        //     return query;
        // }

        public static List<Transaction> GetPendingTransactions(int pageNumber, int resultPerPage)
        {
            // var coll = DbAccess.DB_POOL.GetCollection<Transaction>(DbAccess.TBL_TRANSACTION_POOL);
            // coll.EnsureIndex(x => x.TimeStamp);
            // var query = coll.Query()
            //     .OrderByDescending(x => x.TimeStamp)
            //     .Offset((pageNumber - 1) * resultPerPage)
            //     .Limit(resultPerPage).ToList();
            // var list = new List<Transaction>();

            var start = (pageNumber - 1) * resultPerPage;

            var pool = GetPool();

            var end = (start + resultPerPage - 1);
            if (pool.Count < end)
            {
                end = pool.Count;
            }

            Console.WriteLine("== start {0}", start);
            Console.WriteLine("== end {0}", end);


            // var startGet = mempool.Count;
            // var temp = GetPool();
            pool.Reverse();

            var rtns = pool.GetRange(start, end);

            // if (startGet > offset)
            // {
            //     startGet = startGet - offset;
            // }

            // for (int i = mempool.Count; i > mempool.Count - resultPerPage; i--)
            // {
            //     list.Add(mempool[i]);
            // }
            // var rtn = GetPool();
            // rtn.Reverse();
            return rtns;
        }

        /**
        * get transaction list 
        */
        public static IEnumerable<Transaction> GetTransactions(int pageNumber, int resultPerPage)
        {
            var coll = DbAccess.DB_TXNS.GetCollection<Transaction>(DbAccess.TBL_TRANSACTIONS);
            coll.EnsureIndex(x => x.TimeStamp);
            var query = coll.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }

        /**
        create transaction for each ico account
        **/
        public static void CreateIcoTransction()
        {
            var timeStamp = Utils.GetTime();
            foreach (var acc in IcoBalance.GetIcoAccounts())
            {

                var newTrx = new Transaction()
                {
                    TimeStamp = timeStamp,
                    Sender = "UkcU6SQGuPqrDWgD8AY5oRD7PRxVQV5LWrbf6vkrTtuDtBc",
                    Recipient = acc.Address,
                    Amount = acc.Balance,
                    Height = 1,
                    Fee = 0.0f
                };
                newTrx.Build();

                AddToPool(newTrx);
            }
        }

        /**
        create transaction for each ico account
        **/
        public static void CreateGenesisTransction()
        {
            var timeStamp = Utils.GetTime();
            foreach (var acc in Genesis.GetAll())
            {

                var newTrx = new Transaction()
                {
                    TimeStamp = timeStamp,
                    Sender = "Genesis",
                    Recipient = acc.Address,
                    Amount = acc.Balance,
                    Fee = 0.0f,
                    Height = 1
                };
                newTrx.Build();

                AddToPool(newTrx);
            }
        }


        /**
        * Get balance by name
        */
        public static double GetBalance(string address)
        {
            double balance = 0;
            double spending = 0;
            double income = 0;

            var collection = GetAll();
            var transactions = collection.Find(x => x.Sender == address || x.Recipient == address);

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

        public static bool VerifySignature(string publicKeyHex, string message, string signature)
        {
            var byt = Utils.HexToBytes(publicKeyHex);
            var publicKey = PublicKey.fromString(byt);
            return Ecdsa.verify(message, Signature.fromBase64(signature), publicKey);
        }

        public void Build()
        {
            Hash = GetHash();
        }

        public string GetHash()
        {
            var data = TimeStamp + Sender + Amount + Fee + Recipient;
            return Utils.GenHash(Utils.GenHash(data));
        }


    }

}