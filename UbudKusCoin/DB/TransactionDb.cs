// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UbudKusCoin.Grpc;

namespace UbudKusCoin.DB
{
    /// <summary>
    /// Transaction DB, for add, update transaction. Transactions are keyed by
    /// their hash so lookups are direct and scans provide chronological views.
    /// </summary>
    public class TransactionDb
    {
        private readonly LmdbStore _store;

        public TransactionDb(LmdbStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Add some transaction in same time
        /// </summary>
        public bool AddBulk(List<Transaction> transactions)
        {
            try
            {
                foreach (var transaction in transactions)
                {
                    Add(transaction);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Add a transaction
        /// </summary>
        public bool Add(Transaction transaction)
        {
            try
            {
                _store.Put(Key(transaction.Hash), LmdbSerializer.ToBytes(transaction));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool RemoveByHash(string hash)
        {
            return _store.Delete(Key(hash));
        }

        /// <summary>
        /// Get All Transactions by Address and with paging
        /// </summary>
        public IEnumerable<Transaction> GetRangeByAddress(string address, int pageNumber, int resultsPerPage)
        {
            return GetAll()
                .Where(x => x.Sender == address || x.Recipient == address)
                .OrderByDescending(x => x.TimeStamp)
                .Skip((pageNumber - 1) * resultsPerPage)
                .Take(resultsPerPage)
                .ToList();
        }

        /// <summary>
        /// Get Transaction by Hash
        /// </summary>
        public Transaction GetByHash(string hash)
        {
            if (_store.TryGet(Key(hash), out var bytes))
            {
                return LmdbSerializer.FromBytes<Transaction>(bytes);
            }

            return null;
        }

        /// <summary>
        /// Get transactions
        /// </summary>
        public IEnumerable<Transaction> GetRange(int pageNumber, int resultPerPage)
        {
            return GetAll()
                .OrderByDescending(x => x.TimeStamp)
                .Skip((pageNumber - 1) * resultPerPage)
                .Take(resultPerPage)
                .ToList();
        }

        public IEnumerable<Transaction> GetLast(int num)
        {
            return GetAll()
                .OrderByDescending(x => x.TimeStamp)
                .Take(num)
                .ToList();
        }

        /// <summary>
        /// get one transaction by address
        /// </summary>
        public Transaction GetByAddress(string address)
        {
            return GetAll()
                .OrderByDescending(x => x.TimeStamp)
                .FirstOrDefault(x => x.Sender == address || x.Recipient == address);
        }

        public List<Transaction> GetAll()
        {
            return _store.Scan(Array.Empty<byte>())
                .Select(pair => LmdbSerializer.FromBytes<Transaction>(pair.Value))
                .ToList();
        }

        private static byte[] Key(string hash)
        {
            return Encoding.UTF8.GetBytes(hash ?? string.Empty);
        }
    }
}