// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using LightningDB;

namespace UbudKusCoin.DB
{
    /// <summary>
    /// Minimal key-value store backed by a single LMDB environment. Each store
    /// owns one physical database file; records are protobuf-encoded values keyed
    /// by a caller-managed byte or string key. All mutations are serialized, so
    /// callers never need to coordinate concurrent writes.
    /// </summary>
    public sealed class LmdbStore : IDisposable
    {
        private readonly LightningEnvironment environment;
        private readonly object writeLock = new();

        public LmdbStore(string path)
        {
            environment = new LightningEnvironment(path)
            {
                MaxDatabases = 1
            };
            environment.Open();

            // Ensure the unnamed main database exists before any read transaction.
            using (var tx = environment.BeginTransaction())
            using (tx.OpenDatabase(new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            {
                tx.Commit();
            }
        }

        public void Put(byte[] key, byte[] value)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);

            lock (writeLock)
            {
                using var tx = environment.BeginTransaction();
                using var db = tx.OpenDatabase();
                tx.Put(db, key, value);
                tx.Commit();
            }
        }

        public bool TryGet(byte[] key, out byte[] value)
        {
            ArgumentNullException.ThrowIfNull(key);

            using var tx = environment.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();
            var (resultCode, _, bytes) = tx.Get(db, key);
            if (resultCode == MDBResultCode.Success)
            {
                value = bytes.CopyToNewArray();
                return true;
            }

            value = Array.Empty<byte>();
            return false;
        }

        public bool Contains(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);

            using var tx = environment.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();
            var (resultCode, _, _) = tx.Get(db, key);
            return resultCode == MDBResultCode.Success;
        }

        public bool Delete(byte[] key)
        {
            ArgumentNullException.ThrowIfNull(key);

            lock (writeLock)
            {
                using var tx = environment.BeginTransaction();
                using var db = tx.OpenDatabase();
                var deleted = tx.Delete(db, key) == MDBResultCode.Success;
                tx.Commit();
                return deleted;
            }
        }

        /// <summary>
        /// Returns every key/value pair whose key starts with <paramref name="prefix"/>
        /// in ascending key order.
        /// </summary>
        public List<KeyValuePair<byte[], byte[]>> Scan(byte[] prefix)
        {
            ArgumentNullException.ThrowIfNull(prefix);

            var results = new List<KeyValuePair<byte[], byte[]>>();
            using var tx = environment.BeginTransaction(TransactionBeginFlags.ReadOnly);
            using var db = tx.OpenDatabase();
            using var cursor = tx.CreateCursor(db);

            var resultCode = cursor.SetRange(prefix);
            while (resultCode == MDBResultCode.Success)
            {
                var (code, key, value) = cursor.GetCurrent();
                if (code != MDBResultCode.Success)
                {
                    break;
                }

                var keyBytes = key.CopyToNewArray();
                if (!StartsWith(keyBytes, prefix))
                {
                    break;
                }

                results.Add(new KeyValuePair<byte[], byte[]>(keyBytes, value.CopyToNewArray()));
                resultCode = cursor.Next().Item1;
            }

            return results;
        }

        /// <summary>
        /// Deletes every key that starts with <paramref name="prefix"/>.
        /// </summary>
        public long Clear(byte[] prefix)
        {
            ArgumentNullException.ThrowIfNull(prefix);

            lock (writeLock)
            {
                using var tx = environment.BeginTransaction();
                using var db = tx.OpenDatabase();
                using var cursor = tx.CreateCursor(db);

                long removed = 0;
                var resultCode = cursor.SetRange(prefix);
                while (resultCode == MDBResultCode.Success)
                {
                    var (code, key, _) = cursor.GetCurrent();
                    if (code != MDBResultCode.Success)
                    {
                        break;
                    }

                    var keyBytes = key.CopyToNewArray();
                    if (!StartsWith(keyBytes, prefix))
                    {
                        break;
                    }

                    cursor.Delete();
                    removed++;
                    resultCode = cursor.Next().Item1;
                }

                tx.Commit();
                return removed;
            }
        }

        /// <summary>
        /// Returns the number of keys that start with <paramref name="prefix"/>.
        /// </summary>
        public long Count(byte[] prefix)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            return Scan(prefix).Count;
        }

        public void Dispose()
        {
            environment.Dispose();
        }

        private static bool StartsWith(byte[] value, byte[] prefix)
        {
            if (value.Length < prefix.Length)
            {
                return false;
            }

            for (var i = 0; i < prefix.Length; i++)
            {
                if (value[i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}