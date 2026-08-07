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
    public class PoolTransactionsDb
    {
        private readonly LmdbStore _store;

        public PoolTransactionsDb(LmdbStore store)
        {
            _store = store;
        }

        public void Add(Transaction transaction)
        {
            _store.Put(Key(transaction.Hash), LmdbSerializer.ToBytes(transaction));
        }

        public Transaction GetByHash(string hash)
        {
            if (_store.TryGet(Key(hash), out var bytes))
            {
                return LmdbSerializer.FromBytes<Transaction>(bytes);
            }

            return null;
        }

        public IEnumerable<Transaction> GetRange(int pageNumber, int resultPerPage)
        {
            return GetAll()
                .OrderByDescending(x => x.TimeStamp)
                .Skip((pageNumber - 1) * resultPerPage)
                .Take(resultPerPage)
                .ToList();
        }

        public void DeleteAll()
        {
            _store.Clear(Array.Empty<byte>());
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