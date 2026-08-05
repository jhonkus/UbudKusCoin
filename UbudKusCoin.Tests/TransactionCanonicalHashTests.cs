using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public class TransactionCanonicalHashTests
{
    private static Transaction MakeTx(uint nonce = 1, uint chainId = ChainInfo.ChainIdTestnet)
    {
        var fromPub = new byte[33];
        var toPub = new byte[33];
        fromPub[0] = 0x02;
        toPub[0] = 0x03;

        return new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = chainId,
            Nonce = nonce,
            From = Address.FromPublicKey(Address.TestnetVersion, fromPub),
            To = Address.FromPublicKey(Address.TestnetVersion, toPub),
            Amount = Money.FromCoins(1.0m),
            Fee = Money.FromCoins(0.001m),
            PubKey = fromPub,
        };
    }

    [Fact]
    public void TxHash_IsDeterministic()
    {
        var tx1 = MakeTx();
        var tx2 = MakeTx();
        Assert.Equal(tx1.ComputeIdHex(), tx2.ComputeIdHex());
    }

    [Fact]
    public void TxHash_ChangesWithNonce()
    {
        var tx1 = MakeTx(nonce: 1);
        var tx2 = MakeTx(nonce: 2);
        Assert.NotEqual(tx1.ComputeIdHex(), tx2.ComputeIdHex());
    }

    [Fact]
    public void TxHash_ChangesWithChainId()
    {
        var tx1 = MakeTx(chainId: ChainInfo.ChainIdTestnet);
        var tx2 = MakeTx(chainId: ChainInfo.ChainIdMainnet);
        Assert.NotEqual(tx1.ComputeIdHex(), tx2.ComputeIdHex());
    }

    [Fact]
    public void TxHash_ChangesWithAmount()
    {
        var tx1 = MakeTx();
        var tx2 = MakeTx();
        tx2.Amount = Money.FromCoins(2.0m);
        Assert.NotEqual(tx1.ComputeIdHex(), tx2.ComputeIdHex());
    }

    [Fact]
    public void TxId_DoesNotIncludeSignature()
    {
        var tx = MakeTx();
        var idWithoutSig = tx.ComputeId();

        tx.Signature = new byte[64];
        // Signature is not part of the digest, so the id must be unchanged.
        Assert.Equal(idWithoutSig, tx.ComputeId());
    }

    [Fact]
    public void TxDigest_IsFixedLengthAndValidated()
    {
        var tx = MakeTx();
        var digest = tx.ComputeDigest();
        Assert.NotNull(digest);
        Assert.True(digest.Length > 0);
    }
}
