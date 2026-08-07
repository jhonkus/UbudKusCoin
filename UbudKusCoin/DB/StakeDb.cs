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
    /// Stake database. Stakes are keyed by address for direct upsert lookups.
    /// </summary>
    public class StakeDb
    {
        private readonly LmdbStore _store;

        public StakeDb(LmdbStore store)
        {
            _store = store;
        }

        /// <summary>
        /// add or update stake
        /// </summary>
        public void AddOrUpdate(Stake stake)
        {
            _store.Put(Key(stake.Address), LmdbSerializer.ToBytes(stake));
        }

        /// <summary>
        /// Delete all stake
        /// </summary>
        public void DeleteAll()
        {
            _store.Clear(Array.Empty<byte>());
        }

        /// <summary>
        /// Get maximum stake, base on amount
        /// </summary>
        public Stake GetMax()
        {
            return GetAll()
                .OrderByDescending(x => x.Amount)
                .FirstOrDefault();
        }

        /// <summary>
        /// Get stake by address
        /// </summary>
        public Stake GetByAddress(string address)
        {
            if (_store.TryGet(Key(address), out var bytes))
            {
                return LmdbSerializer.FromBytes<Stake>(bytes);
            }

            return null;
        }

        /// <summary>
        /// Get all stake
        /// </summary>
        public List<Stake> GetAll()
        {
            return _store.Scan(Array.Empty<byte>())
                .Select(pair => LmdbSerializer.FromBytes<Stake>(pair.Value))
                .ToList();
        }

        private static byte[] Key(string address)
        {
            return Encoding.UTF8.GetBytes(address ?? string.Empty);
        }
    }
}