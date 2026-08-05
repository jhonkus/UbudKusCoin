using System;
using System.Collections.Generic;
using System.Linq;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using Xunit;
using BtcKey = NBitcoin.Key;
using BtcNetwork = NBitcoin.Network;

namespace UbudKusCoin.Tests;

public class MempoolTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;
    private const long Now = 1_700_000_000L;

    private static BtcKey MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new BtcKey(bytes);
    }

    private static Address MakeAddress(BtcKey key)
        => Address.FromPublicKey(ChainInfo.AddressVersion(ChainId), key.PubKey.ToBytes());

    private static Transaction MakeSignedTx(BtcKey fromKey, Address to, Money amount, Money fee, ulong nonce)
    {
        var from = MakeAddress(fromKey);

        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = nonce,
            From = from,
            To = to,
            Amount = amount,
            Fee = fee,
            ValidFrom = 0,
            ValidUntil = 0,
            PubKey = fromKey.PubKey.ToBytes(),
        };

        tx.Signature = TransactionSigner.Sign(tx, fromKey.ToBytes());
        return tx;
    }

    private static State MakeState((BtcKey key, Money bal)[] funded)
    {
        var state = new State(ChainId);
        foreach (var (key, bal) in funded)
        {
            var acc = state.EnsureAccount(MakeAddress(key));
            acc.Balance = bal;
        }

        return state;
    }

    [Fact]
    public void Add_ValidSignedTx_IsAccepted()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(10m)) });
        var pool = new Mempool(ChainId);

        var tx = MakeSignedTx(fromKey, to, Money.FromCoins(1m), FeePolicy.MinRelayFee, nonce: 1);
        var result = pool.Add(tx, state, Now);

        Assert.True(result.Accepted, result.Reason);
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void Add_InvalidSignature_IsRejected()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(10m)) });
        var pool = new Mempool(ChainId);

        var tx = MakeSignedTx(fromKey, to, Money.FromCoins(1m), FeePolicy.MinRelayFee, nonce: 1);
        tx.Signature = new byte[64]; // bad

        var result = pool.Add(tx, state, Now);
        Assert.False(result.Accepted);
        Assert.Contains("signature", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_Duplicate_IsRejected()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(10m)) });
        var pool = new Mempool(ChainId);

        var tx = MakeSignedTx(fromKey, to, Money.FromCoins(1m), FeePolicy.MinRelayFee, nonce: 1);

        Assert.True(pool.Add(tx, state, Now).Accepted);
        var dup = pool.Add(tx, state, Now);
        Assert.False(dup.Accepted);
        Assert.Contains("Duplicate", dup.Reason);
        Assert.Equal(1, pool.Count);
    }

    [Fact]
    public void Add_OutOfOrderNonce_IsRejected()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(10m)) });
        var pool = new Mempool(ChainId);

        // Account nonce is 0, so next must be 1. Nonce 2 is out of order.
        var tx = MakeSignedTx(fromKey, to, Money.FromCoins(1m), FeePolicy.MinRelayFee, nonce: 2);
        var result = pool.Add(tx, state, Now);

        Assert.False(result.Accepted);
        Assert.Contains("nonce", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_InsufficientBalance_IsRejected()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(1m)) });
        var pool = new Mempool(ChainId);

        var tx = MakeSignedTx(fromKey, to, Money.FromCoins(5m), FeePolicy.MinRelayFee, nonce: 1);
        var result = pool.Add(tx, state, Now);

        Assert.False(result.Accepted);
        Assert.Contains("balance", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_AggregatePending_CountsAgainstBalance()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(3m)) });
        var pool = new Mempool(ChainId);

        // First tx spends 2.0
        var tx1 = MakeSignedTx(fromKey, to, Money.FromCoins(2m), FeePolicy.MinRelayFee, nonce: 1);
        Assert.True(pool.Add(tx1, state, Now).Accepted);

        // Second tx spends 2.0 more; total 4.0 > 3.0 => reject.
        var tx2 = MakeSignedTx(fromKey, to, Money.FromCoins(2m), FeePolicy.MinRelayFee, nonce: 2);
        var result = pool.Add(tx2, state, Now);

        Assert.False(result.Accepted);
        Assert.Contains("balance", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_TxNotYetValid_IsRejected()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(10m)) });
        var pool = new Mempool(ChainId);

        var tx = MakeSignedTx(fromKey, to, Money.FromCoins(1m), FeePolicy.MinRelayFee, nonce: 1);
        tx.ValidFrom = Now + 1000;
        tx.Signature = TransactionSigner.Sign(tx, fromKey.ToBytes()); // re-sign after change

        var result = pool.Add(tx, state, Now);
        Assert.False(result.Accepted);
        Assert.Contains("not valid yet", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_ExpiredTx_IsRejected()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(10m)) });
        var pool = new Mempool(ChainId);

        var tx = MakeSignedTx(fromKey, to, Money.FromCoins(1m), FeePolicy.MinRelayFee, nonce: 1);
        tx.ValidUntil = Now - 1000;
        tx.Signature = TransactionSigner.Sign(tx, fromKey.ToBytes());

        var result = pool.Add(tx, state, Now);
        Assert.False(result.Accepted);
        Assert.Contains("expired", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Add_MalformedEnvelope_IsRejected()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(10m)) });
        var pool = new Mempool(ChainId);

        var tx = MakeSignedTx(fromKey, to, Money.FromCoins(1m), FeePolicy.MinRelayFee, nonce: 1);
        tx.Fee = Money.Zero; // below min relay fee

        var result = pool.Add(tx, state, Now);
        Assert.False(result.Accepted);
    }

    [Fact]
    public void RemoveRange_RemovesOnlyMinedTxs()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(100m)) });
        var pool = new Mempool(ChainId);

        var tx1 = MakeSignedTx(fromKey, to, Money.FromCoins(1m), FeePolicy.MinRelayFee, nonce: 1);
        var tx2 = MakeSignedTx(fromKey, to, Money.FromCoins(2m), FeePolicy.MinRelayFee, nonce: 2);
        Assert.True(pool.Add(tx1, state, Now).Accepted);
        Assert.True(pool.Add(tx2, state, Now).Accepted);
        Assert.Equal(2, pool.Count);

        // Only tx1 is mined.
        pool.RemoveRange(new[] { tx1 });
        Assert.Equal(1, pool.Count);
        Assert.True(pool.Contains(tx2));
        Assert.False(pool.Contains(tx1));
    }

    [Fact]
    public void PerSenderCap_IsEnforced()
    {
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var state = MakeState(new[] { (fromKey, Money.FromCoins(1_000m)) });
        var pool = new Mempool(ChainId);

        ulong nonce = 1;
        for (var i = 0; i < FeePolicy.MaxPendingPerSender; i++)
        {
            var tx = MakeSignedTx(fromKey, to, Money.FromCoins(0.001m), FeePolicy.BaseFee, nonce);
            Assert.True(pool.Add(tx, state, Now).Accepted, $"tx {i}");
            nonce++;
        }

        var over = MakeSignedTx(fromKey, to, Money.FromCoins(0.001m), FeePolicy.BaseFee, nonce);
        var result = pool.Add(over, state, Now);
        Assert.False(result.Accepted);
        Assert.Contains("limit", result.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
