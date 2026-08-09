using System;
using System.Collections.Generic;
using System.Linq;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Sdk;
using Xunit;
using Key = NBitcoin.Key;

namespace UbudKusCoin.Tests;

public class SdkTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;

    private static Key MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new Key(bytes);
    }

    [Fact]
    public void WalletUtils_DerivesCorrectSingleSigAddress()
    {
        var key = MakeKey(0xaa);
        var expected = Address.FromPublicKey(Address.TestnetVersion, key.PubKey.ToBytes()).Encoded;
        var actual = WalletUtils.DeriveAddress(key.PubKey.ToBytes(), ChainId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WalletUtils_DerivesCorrectMultiSigAddress()
    {
        var key1 = MakeKey(0x01);
        var key2 = MakeKey(0x02);
        var key3 = MakeKey(0x03);

        var pubKeys = new[] { key1.PubKey.ToBytes(), key2.PubKey.ToBytes(), key3.PubKey.ToBytes() };
        var expected = Address.FromMultiSig(Address.TestnetVersion, threshold: 2, pubKeys).Encoded;
        var actual = WalletUtils.DeriveMultiSigAddress(2, pubKeys, ChainId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TransactionBuilder_Transfer_BuildsAndSignsCorrectly()
    {
        var senderKey = MakeKey(0x05);
        var senderAddr = WalletUtils.DeriveAddress(senderKey.PubKey.ToBytes(), ChainId);
        var recipientAddr = WalletUtils.DeriveAddress(MakeKey(0x06).PubKey.ToBytes(), ChainId);

        var amount = Money.FromCoins(15.5m);
        var fee = Money.FromCoins(0.001m);

        var builder = new TransactionBuilder(ChainId)
            .SetTransfer(senderAddr, recipientAddr, amount)
            .SetFee(fee)
            .SetNonce(10)
            .SetValidity(1_700_000_000, 1_700_010_000);

        var tx = builder.BuildAndSign(senderKey.ToBytes());

        Assert.Equal(ChainId, tx.ChainId);
        Assert.Equal(TransactionKind.Transfer, tx.Kind);
        Assert.Equal(senderAddr, tx.From.Encoded);
        Assert.Equal(recipientAddr, tx.To.Encoded);
        Assert.Equal(amount.BaseUnits, tx.Amount.BaseUnits);
        Assert.Equal(fee.BaseUnits, tx.Fee.BaseUnits);
        Assert.Equal(10ul, tx.Nonce);
        Assert.Equal(1_700_000_000, tx.ValidFrom);
        Assert.Equal(1_700_010_000, tx.ValidUntil);
        Assert.True(tx.PubKey.SequenceEqual(senderKey.PubKey.ToBytes()));
        Assert.NotEmpty(tx.Signature);

        // Verify signatures offline
        Assert.True(tx.VerifySignature());

        // Test roundtrip encoding/decoding
        var encoded = TransactionCodec.Encode(tx);
        bool decodedSuccess = TransactionCodec.TryDecode(encoded, out var decodedTx, out var error);
        Assert.True(decodedSuccess, error);
        Assert.NotNull(decodedTx);
        Assert.Equal(tx.ComputeIdHex(), decodedTx.ComputeIdHex());
        Assert.True(decodedTx.VerifySignature());
    }

    [Fact]
    public void TransactionBuilder_MultiSig_BuildsAndSignsCorrectly()
    {
        var key1 = MakeKey(0x01);
        var key2 = MakeKey(0x02);
        var key3 = MakeKey(0x03);
        var pubKeys = new[] { key1.PubKey.ToBytes(), key2.PubKey.ToBytes(), key3.PubKey.ToBytes() };

        var multiSigAddr = WalletUtils.DeriveMultiSigAddress(2, pubKeys, ChainId);
        var recipientAddr = WalletUtils.DeriveAddress(MakeKey(0x07).PubKey.ToBytes(), ChainId);

        var builder = new TransactionBuilder(ChainId)
            .SetTransfer(multiSigAddr, recipientAddr, Money.FromCoins(5m))
            .SetFee(FeePolicy.BaseFee)
            .SetNonce(1)
            .SetPubKey(key1.PubKey.ToBytes()); // Set representative key

        // Generate temporary unsigned transaction to compute digests
        var txToSign = builder.BuildUnsigned();

        // Sign offline with 2 keys
        var sig1 = TransactionSigner.Sign(txToSign, key1.ToBytes());
        var sig2 = TransactionSigner.Sign(txToSign, key3.ToBytes());

        // Aggregate signatures into multi-sig payload
        var finalTx = builder.BuildAndSignMultiSig(2, pubKeys, new[] { sig1, sig2 });

        Assert.Equal(multiSigAddr, finalTx.From.Encoded);
        Assert.True(finalTx.VerifySignature());
    }

    [Fact]
    public void TransactionBuilder_OtherTransactionTypes_BuildCorrectly()
    {
        var fromAddr = WalletUtils.DeriveAddress(MakeKey(0x01).PubKey.ToBytes(), ChainId);
        var validatorPub = new byte[32];
        validatorPub[0] = 0xff;

        // Bond
        var bondTx = new TransactionBuilder(ChainId)
            .SetBond(fromAddr, Money.FromCoins(100m), validatorPub)
            .BuildUnsigned();
        Assert.Equal(TransactionKind.Bond, bondTx.Kind);
        Assert.Equal(100m, bondTx.Amount.Coins);
        Assert.True(bondTx.ValidatorPubKey.SequenceEqual(validatorPub));

        // Unbond
        var unbondTx = new TransactionBuilder(ChainId)
            .SetUnbond(fromAddr, lockPeriod: 1000)
            .BuildUnsigned();
        Assert.Equal(TransactionKind.Unbond, unbondTx.Kind);
        Assert.Equal(1000, unbondTx.LockPeriod);

        // Withdraw
        var withdrawTx = new TransactionBuilder(ChainId)
            .SetWithdraw(fromAddr)
            .BuildUnsigned();
        Assert.Equal(TransactionKind.Withdraw, withdrawTx.Kind);

        // Rotate
        var rotateTx = new TransactionBuilder(ChainId)
            .SetRotateValidatorKey(fromAddr, validatorPub)
            .BuildUnsigned();
        Assert.Equal(TransactionKind.RotateValidatorKey, rotateTx.Kind);
        Assert.True(rotateTx.ValidatorPubKey.SequenceEqual(validatorPub));
    }
}
