using System;
using System.Collections.Generic;
using System.IO;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Services;
using Xunit;
using BtcKey = NBitcoin.Key;

namespace UbudKusCoin.Tests;

public class ChainIndexerTests : IDisposable
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;
    private static readonly byte Version = ChainInfo.AddressVersion(ChainId);
    private readonly string _testDbPath;

    public ChainIndexerTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"ukc_indexer_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }

    private static BtcKey MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new BtcKey(bytes);
    }

    private static Address MakeAddress(BtcKey key)
        => Address.FromPublicKey(Version, key.PubKey.ToBytes());

    [Fact]
    public void IndexerStore_IndexesBlockAndQueriesTransactionsFast()
    {
        using var indexer = new IndexerStore(_testDbPath);

        var senderKey = MakeKey(0x01);
        var senderAddr = MakeAddress(senderKey);
        var recipientAddr = MakeAddress(MakeKey(0x02));

        var state = new State(ChainId);
        var senderAcc = state.EnsureAccount(senderAddr);
        senderAcc.Balance = Money.FromCoins(100m);
        senderAcc.Nonce = 1;

        var recipientAcc = state.EnsureAccount(recipientAddr);
        recipientAcc.Balance = Money.FromCoins(10m);

        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = 1,
            From = senderAddr,
            To = recipientAddr,
            Amount = Money.FromCoins(10m),
            Fee = FeePolicy.MinRelayFee,
            PubKey = senderKey.PubKey.ToBytes(),
        };
        tx.Signature = TransactionSigner.Sign(tx, senderKey.ToBytes());

        var block = new Block
        {
            ChainId = ChainId,
            Height = 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = senderAddr,
            Reward = Money.Zero,
            Txs = new List<Transaction> { tx },
            StateRoot = state.ComputeStateRoot()
        };

        indexer.IndexBlock(block, state);

        Assert.Equal(1, indexer.GetLastIndexedHeight());

        // Fast O(1) Address query for sender
        var senderTxs = indexer.GetTransactionsForAddress(senderAddr.Encoded);
        Assert.Single(senderTxs);
        Assert.Equal(tx.ComputeIdHex(), senderTxs[0].TxId);
        Assert.Equal(senderAddr.Encoded, senderTxs[0].From);
        Assert.Equal(recipientAddr.Encoded, senderTxs[0].To);

        // Fast O(1) Address query for recipient
        var recipientTxs = indexer.GetTransactionsForAddress(recipientAddr.Encoded);
        Assert.Single(recipientTxs);
        Assert.Equal(tx.ComputeIdHex(), recipientTxs[0].TxId);

        // Fast O(1) TxId lookup
        var txResult = indexer.GetTransactionById(tx.ComputeIdHex());
        Assert.NotNull(txResult);
        Assert.Equal(1, txResult.Height);

        // Fast O(1) Block Height lookup
        var blockResult = indexer.GetBlockByHeight(1);
        Assert.NotNull(blockResult);
        Assert.Equal(1, blockResult.TxCount);
        Assert.Equal(block.ComputeHeaderHashHex(), blockResult.BlockHash);
    }
}
