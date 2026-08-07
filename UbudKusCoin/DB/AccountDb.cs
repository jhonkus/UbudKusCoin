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
    /// Account Database, for Add. Update and retrieve account. Accounts are
    /// keyed by address for direct lookups; other queries iterate the store.
    /// </summary>
    public class AccountDb
    {
        private readonly LmdbStore _store;

        public AccountDb(LmdbStore store)
        {
            _store = store;
        }

        public void Add(Account acc)
        {
            _store.Put(Key(acc.Address), LmdbSerializer.ToBytes(acc));
        }

        public void Update(Account acc)
        {
            _store.Put(Key(acc.Address), LmdbSerializer.ToBytes(acc));
        }

        public bool RemoveByAddress(string address)
        {
            return _store.Delete(Key(address));
        }

        public IEnumerable<Account> GetRange(int pageNumber, int resultPerPage)
        {
            return GetAll()
                .OrderByDescending(x => x.Balance)
                .Skip((pageNumber - 1) * resultPerPage)
                .Take(resultPerPage)
                .ToList();
        }

        public Account GetByAddress(string address)
        {
            if (_store.TryGet(Key(address), out var bytes))
            {
                return LmdbSerializer.FromBytes<Account>(bytes);
            }

            return null;
        }

        public Account GetByPubKey(string pubkey)
        {
            return GetAll().FirstOrDefault(x => x.PubKey == pubkey);
        }

        public List<Account> GetAll()
        {
            return _store.Scan(Array.Empty<byte>())
                .Select(pair => LmdbSerializer.FromBytes<Account>(pair.Value))
                .ToList();
        }

        private static byte[] Key(string address)
        {
            return Encoding.UTF8.GetBytes(address ?? string.Empty);
        }
    }
}