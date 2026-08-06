using System;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class TransactionCodecTests
{
    [Fact]
    public void EncodeDecode_PreservesCanonicalEnvelope()
    {
        var transaction = new Transaction
        {
            ChainId = ChainInfo.ChainIdTestnet,
            Nonce = 7,
            From = MakeAddress(1),
            To = MakeAddress(2),
            Amount = new Money(123),
            Fee = new Money(4),
            ValidFrom = 10,
            ValidUntil = 20,
            PubKey = new byte[] { 1, 2, 3 },
            Signature = new byte[] { 4, 5, 6 }
        };

        var encoded = TransactionCodec.Encode(transaction);
        Assert.True(TransactionCodec.TryDecode(encoded, out var decoded, out var error), error);
        Assert.Equal(transaction.ComputeDigest(), decoded!.ComputeDigest());
        Assert.Equal(encoded, TransactionCodec.Encode(decoded));
    }

    [Fact]
    public void Decode_RejectsTrailingBytes()
    {
        var transaction = new Transaction
        {
            ChainId = ChainInfo.ChainIdTestnet,
            From = MakeAddress(1),
            To = MakeAddress(2),
            Amount = new Money(1),
            Fee = new Money(1)
        };
        var encoded = TransactionCodec.Encode(transaction).AsSpan().ToArray();
        Array.Resize(ref encoded, encoded.Length + 1);

        Assert.False(TransactionCodec.TryDecode(encoded, out _, out var error));
        Assert.Contains("Trailing", error);
    }

    private static Address MakeAddress(byte value)
        => new(ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet), new[] { value, value, value, value });
}
