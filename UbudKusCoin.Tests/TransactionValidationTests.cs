using System;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using Xunit;
using BtcKey = NBitcoin.Key;

namespace UbudKusCoin.Tests;

public class TransactionValidationTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;

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
            PubKey = fromKey.PubKey.ToBytes(),
        };
        tx.Signature = TransactionSigner.Sign(tx, fromKey.ToBytes());
        return tx;
    }

    private static Transaction MakeWellFormed()
        => MakeSignedTx(MakeKey(0x01), MakeAddress(MakeKey(0x02)), Money.FromCoins(1m), FeePolicy.MinRelayFee, 1);

    [Fact]
    public void WellFormedTx_PassesEnvelopeValidation()
    {
        var tx = MakeWellFormed();
        Assert.True(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void VerifySignature_ValidSignature_ReturnsTrue()
    {
        var tx = MakeWellFormed();
        Assert.True(tx.VerifySignature());
    }

    [Fact]
    public void VerifySignature_AfterTamperingAmount_Fails()
    {
        var tx = MakeWellFormed();
        tx.Amount = Money.FromCoins(999m); // tamper after signing
        Assert.False(tx.VerifySignature());
    }

    [Fact]
    public void VerifySignature_AfterTamperingNonce_Fails()
    {
        var tx = MakeWellFormed();
        tx.Nonce = 42; // tamper after signing
        Assert.False(tx.VerifySignature());
    }

[Fact]
    public void Envelope_AddressVersionMustMatchChain()
    {
        // Addresses are derived with the testnet version, but we build the tx
        // for the mainnet chain id. The envelope must be rejected because the
        // address layer is incompatible with the declared chain (replay protection).
        var fromKey = MakeKey(0x01);
        var to = MakeAddress(MakeKey(0x02));
        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainInfo.ChainIdMainnet,
            Nonce = 1,
            From = MakeAddress(fromKey), // testnet version
            To = to,                     // testnet version
            Amount = Money.FromCoins(1m),
            Fee = FeePolicy.MinRelayFee,
            PubKey = fromKey.PubKey.ToBytes(),
        };
        tx.Signature = TransactionSigner.Sign(tx, fromKey.ToBytes());

        Assert.False(tx.IsEnvelopeWellFormed(ChainInfo.ChainIdMainnet));
    }

    [Theory]
    [InlineData(0)]   // version 0
    [InlineData(2)]   // version 2
    public void Envelope_RejectsWrongVersion(uint version)
    {
        var tx = MakeWellFormed();
        tx.Version = version;
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void Envelope_RejectsWrongChainId()
    {
        var tx = MakeWellFormed();
        tx.ChainId = ChainInfo.ChainIdMainnet;
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void Envelope_RejectsSelfTransfer()
    {
        var key = MakeKey(0x01);
        var addr = MakeAddress(key);
        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = 1,
            From = addr,
            To = addr,
            Amount = Money.FromCoins(1m),
            Fee = FeePolicy.MinRelayFee,
            PubKey = key.PubKey.ToBytes(),
        };
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void Envelope_RejectsZeroAmount()
    {
        var tx = MakeWellFormed();
        tx.Amount = Money.Zero;
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void Envelope_RejectsFeeBelowMinimum()
    {
        var tx = MakeWellFormed();
        tx.Fee = Money.Zero;
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void Envelope_RejectsFeeAboveMaximum()
    {
        var tx = MakeWellFormed();
        tx.Fee = Money.FromCoins(100m);
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void Envelope_RejectsEmptyPubKey()
    {
        var tx = MakeWellFormed();
        tx.PubKey = Array.Empty<byte>();
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void Envelope_RejectsEmptySignature()
    {
        var tx = MakeWellFormed();
        tx.Signature = Array.Empty<byte>();
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void Envelope_RejectsInvalidTimeRange()
    {
        var tx = MakeWellFormed();
        tx.ValidFrom = 2000;
        tx.ValidUntil = 1000; // until before from
        Assert.False(tx.IsEnvelopeWellFormed(ChainId));
    }

    [Fact]
    public void TxId_IsStableAcrossSignatureMutation()
    {
        var tx = MakeWellFormed();
        var id = tx.ComputeIdHex();
        tx.Signature = new byte[70];
        Assert.Equal(id, tx.ComputeIdHex());
    }
}
