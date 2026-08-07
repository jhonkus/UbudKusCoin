using System;
using System.Collections.Generic;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using Xunit;
using BtcKey = NBitcoin.Key;

namespace UbudKusCoin.Tests;

public class MultiSigTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;
    private static readonly byte Version = ChainInfo.AddressVersion(ChainId);

    private static BtcKey MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new BtcKey(bytes);
    }

    [Fact]
    public void FromMultiSig_GeneratesDeterministicAddress()
    {
        var key1 = MakeKey(0x01);
        var key2 = MakeKey(0x02);
        var key3 = MakeKey(0x03);

        var pubKeys = new[] { key1.PubKey.ToBytes(), key2.PubKey.ToBytes(), key3.PubKey.ToBytes() };

        var addr1 = Address.FromMultiSig(Version, threshold: 2, pubKeys);
        var addr2 = Address.FromMultiSig(Version, threshold: 2, pubKeys);

        Assert.Equal(Version, addr1.Version);
        Assert.Equal(addr1.Encoded, addr2.Encoded);
        Assert.NotEmpty(addr1.Encoded);
    }

    [Fact]
    public void MultiSigTransaction_WithValidThresholdSignatures_IsAccepted()
    {
        var key1 = MakeKey(0x01);
        var key2 = MakeKey(0x02);
        var key3 = MakeKey(0x03);

        var pubKeys = new[] { key1.PubKey.ToBytes(), key2.PubKey.ToBytes(), key3.PubKey.ToBytes() };
        var multiSigAddr = Address.FromMultiSig(Version, threshold: 2, pubKeys);
        var recipient = Address.FromPublicKey(Version, MakeKey(0x99).PubKey.ToBytes());

        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = 1,
            From = multiSigAddr,
            To = recipient,
            Amount = Money.FromCoins(10m),
            Fee = FeePolicy.MinRelayFee,
            PubKey = key1.PubKey.ToBytes(), // Representative pubkey
        };

        // Sign with key1 and key2 (2 of 3 threshold)
        var sig1 = TransactionSigner.Sign(tx, key1.ToBytes());
        var sig2 = TransactionSigner.Sign(tx, key2.ToBytes());

        tx.Signature = MultiSigUtils.EncodeMultiSigPayload(
            threshold: 2,
            publicKeys: pubKeys,
            signatures: new[] { sig1, sig2 }
        );

        Assert.True(tx.VerifySignature());
    }

    [Fact]
    public void MultiSigTransaction_BelowThreshold_IsRejected()
    {
        var key1 = MakeKey(0x01);
        var key2 = MakeKey(0x02);
        var key3 = MakeKey(0x03);

        var pubKeys = new[] { key1.PubKey.ToBytes(), key2.PubKey.ToBytes(), key3.PubKey.ToBytes() };
        var multiSigAddr = Address.FromMultiSig(Version, threshold: 2, pubKeys);
        var recipient = Address.FromPublicKey(Version, MakeKey(0x99).PubKey.ToBytes());

        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = 1,
            From = multiSigAddr,
            To = recipient,
            Amount = Money.FromCoins(10m),
            Fee = FeePolicy.MinRelayFee,
            PubKey = key1.PubKey.ToBytes(),
        };

        // Sign with ONLY 1 key when threshold is 2
        var sig1 = TransactionSigner.Sign(tx, key1.ToBytes());

        tx.Signature = MultiSigUtils.EncodeMultiSigPayload(
            threshold: 2,
            publicKeys: pubKeys,
            signatures: new[] { sig1 }
        );

        Assert.False(tx.VerifySignature());
    }

    [Fact]
    public void MultiSigTransaction_TamperedAmount_IsRejected()
    {
        var key1 = MakeKey(0x01);
        var key2 = MakeKey(0x02);
        var key3 = MakeKey(0x03);

        var pubKeys = new[] { key1.PubKey.ToBytes(), key2.PubKey.ToBytes(), key3.PubKey.ToBytes() };
        var multiSigAddr = Address.FromMultiSig(Version, threshold: 2, pubKeys);
        var recipient = Address.FromPublicKey(Version, MakeKey(0x99).PubKey.ToBytes());

        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = 1,
            From = multiSigAddr,
            To = recipient,
            Amount = Money.FromCoins(10m),
            Fee = FeePolicy.MinRelayFee,
            PubKey = key1.PubKey.ToBytes(),
        };

        var sig1 = TransactionSigner.Sign(tx, key1.ToBytes());
        var sig2 = TransactionSigner.Sign(tx, key2.ToBytes());

        tx.Signature = MultiSigUtils.EncodeMultiSigPayload(
            threshold: 2,
            publicKeys: pubKeys,
            signatures: new[] { sig1, sig2 }
        );

        // Tamper transaction amount after signing
        tx.Amount = Money.FromCoins(100m);

        Assert.False(tx.VerifySignature());
    }
}
