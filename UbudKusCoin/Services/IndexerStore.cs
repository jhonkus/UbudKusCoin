#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using UbudKusCoin.Core.Types;
using CoreBlock = UbudKusCoin.Core.Types.Block;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;

namespace UbudKusCoin.Services;

public sealed record IndexedTransactionDto(
    string TxId,
    long Height,
    long TimeStamp,
    string From,
    string To,
    long AmountBaseUnits,
    long FeeBaseUnits,
    ulong Nonce,
    uint Kind);

public sealed record IndexedBlockDto(
    long Height,
    string BlockHash,
    long TimeStamp,
    int TxCount,
    string Proposer,
    string StateRoot);

public sealed class IndexerStore : IDisposable
{
    private readonly string _connectionString;
    private readonly object _dbLock = new();

    public IndexerStore(string dbPath)
    {
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ConnectionString;

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        lock (_dbLock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                PRAGMA journal_mode = WAL;
                PRAGMA synchronous = NORMAL;

                CREATE TABLE IF NOT EXISTS blocks (
                    height INTEGER PRIMARY KEY,
                    block_hash TEXT NOT NULL,
                    time_stamp INTEGER NOT NULL,
                    tx_count INTEGER NOT NULL,
                    proposer TEXT NOT NULL,
                    state_root TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS transactions (
                    tx_id TEXT PRIMARY KEY,
                    height INTEGER NOT NULL,
                    time_stamp INTEGER NOT NULL,
                    from_address TEXT NOT NULL,
                    to_address TEXT NOT NULL,
                    amount INTEGER NOT NULL,
                    fee INTEGER NOT NULL,
                    nonce INTEGER NOT NULL,
                    kind INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS accounts (
                    address TEXT PRIMARY KEY,
                    balance INTEGER NOT NULL,
                    nonce INTEGER NOT NULL,
                    updated_height INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS staking (
                    address TEXT PRIMARY KEY,
                    amount INTEGER NOT NULL,
                    jailed INTEGER NOT NULL,
                    unlock_height INTEGER NOT NULL,
                    updated_height INTEGER NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_tx_from ON transactions(from_address);
                CREATE INDEX IF NOT EXISTS idx_tx_to ON transactions(to_address);
                CREATE INDEX IF NOT EXISTS idx_tx_height ON transactions(height);
                CREATE INDEX IF NOT EXISTS idx_tx_from_to ON transactions(from_address, to_address);
            ";
            command.ExecuteNonQuery();
        }
    }

    public long GetLastIndexedHeight()
    {
        lock (_dbLock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COALESCE(MAX(height), 0) FROM blocks;";
            var result = command.ExecuteScalar();
            return Convert.ToInt64(result);
        }
    }

    public void IndexBlock(CoreBlock block, State state)
    {
        lock (_dbLock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            // Insert or replace block
            using (var cmdBlock = connection.CreateCommand())
            {
                cmdBlock.Transaction = transaction;
                cmdBlock.CommandText = @"
                    INSERT OR REPLACE INTO blocks (height, block_hash, time_stamp, tx_count, proposer, state_root)
                    VALUES (@height, @hash, @time, @txCount, @proposer, @stateRoot);";
                cmdBlock.Parameters.AddWithValue("@height", block.Height);
                cmdBlock.Parameters.AddWithValue("@hash", block.ComputeHeaderHashHex());
                cmdBlock.Parameters.AddWithValue("@time", block.TimeStamp);
                cmdBlock.Parameters.AddWithValue("@txCount", block.Txs.Count);
                cmdBlock.Parameters.AddWithValue("@proposer", block.Validator.Encoded ?? string.Empty);
                cmdBlock.Parameters.AddWithValue("@stateRoot", Convert.ToHexStringLower(block.StateRoot));
                cmdBlock.ExecuteNonQuery();
            }

            // Insert transactions
            foreach (var tx in block.Txs)
            {
                using var cmdTx = connection.CreateCommand();
                cmdTx.Transaction = transaction;
                cmdTx.CommandText = @"
                    INSERT OR REPLACE INTO transactions (tx_id, height, time_stamp, from_address, to_address, amount, fee, nonce, kind)
                    VALUES (@txId, @height, @time, @from, @to, @amount, @fee, @nonce, @kind);";
                cmdTx.Parameters.AddWithValue("@txId", tx.ComputeIdHex());
                cmdTx.Parameters.AddWithValue("@height", block.Height);
                cmdTx.Parameters.AddWithValue("@time", block.TimeStamp);
                cmdTx.Parameters.AddWithValue("@from", tx.From.Encoded ?? string.Empty);
                cmdTx.Parameters.AddWithValue("@to", tx.To.Encoded ?? string.Empty);
                cmdTx.Parameters.AddWithValue("@amount", tx.Amount.BaseUnits);
                cmdTx.Parameters.AddWithValue("@fee", tx.Fee.BaseUnits);
                cmdTx.Parameters.AddWithValue("@nonce", (long)tx.Nonce);
                cmdTx.Parameters.AddWithValue("@kind", (uint)tx.Kind);
                cmdTx.ExecuteNonQuery();
            }

            // Update state accounts affected
            foreach (var account in state.Accounts)
            {
                using var cmdAcc = connection.CreateCommand();
                cmdAcc.Transaction = transaction;
                cmdAcc.CommandText = @"
                    INSERT OR REPLACE INTO accounts (address, balance, nonce, updated_height)
                    VALUES (@address, @balance, @nonce, @height);";
                cmdAcc.Parameters.AddWithValue("@address", account.Address.Encoded);
                cmdAcc.Parameters.AddWithValue("@balance", account.Balance.BaseUnits);
                cmdAcc.Parameters.AddWithValue("@nonce", (long)account.Nonce);
                cmdAcc.Parameters.AddWithValue("@height", block.Height);
                cmdAcc.ExecuteNonQuery();
            }

            // Update state stakes affected
            foreach (var stake in state.Stakes)
            {
                using var cmdStake = connection.CreateCommand();
                cmdStake.Transaction = transaction;
                cmdStake.CommandText = @"
                    INSERT OR REPLACE INTO staking (address, amount, jailed, unlock_height, updated_height)
                    VALUES (@address, @amount, @jailed, @unlockHeight, @height);";
                cmdStake.Parameters.AddWithValue("@address", stake.Address.Encoded);
                cmdStake.Parameters.AddWithValue("@amount", stake.Amount.BaseUnits);
                cmdStake.Parameters.AddWithValue("@jailed", stake.Jailed ? 1 : 0);
                cmdStake.Parameters.AddWithValue("@unlockHeight", stake.UnlockHeight);
                cmdStake.Parameters.AddWithValue("@height", block.Height);
                cmdStake.ExecuteNonQuery();
            }

            transaction.Commit();
        }
    }

    public IReadOnlyList<IndexedTransactionDto> GetTransactionsForAddress(string addressEncoded, int limit = 50)
    {
        lock (_dbLock)
        {
            var results = new List<IndexedTransactionDto>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT tx_id, height, time_stamp, from_address, to_address, amount, fee, nonce, kind
                FROM transactions
                WHERE from_address = @address OR to_address = @address
                ORDER BY height DESC, nonce DESC
                LIMIT @limit;";
            command.Parameters.AddWithValue("@address", addressEncoded);
            command.Parameters.AddWithValue("@limit", Math.Clamp(limit, 1, 100));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new IndexedTransactionDto(
                    reader.GetString(0),
                    reader.GetInt64(1),
                    reader.GetInt64(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetInt64(5),
                    reader.GetInt64(6),
                    (ulong)reader.GetInt64(7),
                    (uint)reader.GetInt32(8)));
            }

            return results;
        }
    }

    public IndexedTransactionDto? GetTransactionById(string txIdHex)
    {
        lock (_dbLock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT tx_id, height, time_stamp, from_address, to_address, amount, fee, nonce, kind
                FROM transactions
                WHERE tx_id = @txId LIMIT 1;";
            command.Parameters.AddWithValue("@txId", txIdHex.ToLowerInvariant());

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new IndexedTransactionDto(
                    reader.GetString(0),
                    reader.GetInt64(1),
                    reader.GetInt64(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetInt64(5),
                    reader.GetInt64(6),
                    (ulong)reader.GetInt64(7),
                    (uint)reader.GetInt32(8));
            }

            return null;
        }
    }

    public IndexedBlockDto? GetBlockByHeight(long height)
    {
        lock (_dbLock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT height, block_hash, time_stamp, tx_count, proposer, state_root
                FROM blocks
                WHERE height = @height LIMIT 1;";
            command.Parameters.AddWithValue("@height", height);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new IndexedBlockDto(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetInt64(2),
                    reader.GetInt32(3),
                    reader.GetString(4),
                    reader.GetString(5));
            }

            return null;
        }
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
    }
}
