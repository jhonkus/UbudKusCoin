using System;
using System.Text;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class ProtocolFuzzTests
{
    [Fact]
    public void TransactionCodec_RandomBytesNeverEscapeAsExceptions()
    {
        var random = new Random(0x554B4332);
        for (var iteration = 0; iteration < 2_000; iteration++)
        {
            var bytes = new byte[random.Next(0, 4_096)];
            random.NextBytes(bytes);

            var exception = Record.Exception(() =>
                TransactionCodec.TryDecode(bytes, out _, out _));

            Assert.Null(exception);
        }
    }

    [Fact]
    public void SnapshotCodec_RandomBytesNeverEscapeAsExceptions()
    {
        var random = new Random(0x53594E43);
        for (var iteration = 0; iteration < 1_000; iteration++)
        {
            var bytes = new byte[random.Next(0, 16_384)];
            random.NextBytes(bytes);

            var exception = Record.Exception(() =>
                StateSnapshotCodec.TryDecode(bytes, out _, out _));

            Assert.Null(exception);
        }
    }

    [Fact]
    public void AddressParser_RandomTextNeverEscapesAsExceptions()
    {
        const string alphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz-_=+/:";
        var random = new Random(0x41444452);

        for (var iteration = 0; iteration < 5_000; iteration++)
        {
            var length = random.Next(0, 256);
            var chars = new char[length];
            for (var index = 0; index < chars.Length; index++)
            {
                chars[index] = alphabet[random.Next(alphabet.Length)];
            }

            var exception = Record.Exception(() => Address.TryParse(new string(chars), out _));

            Assert.Null(exception);
        }
    }

    [Fact]
    public void TransactionCodec_MutatedValidEnvelopesNeverEscapeAsExceptions()
    {
        var seed = TransactionCodec.Encode(new Transaction
        {
            ChainId = ChainInfo.ChainIdTestnet,
            Nonce = 17,
            From = MakeAddress(1),
            To = MakeAddress(2),
            Amount = new Money(123),
            Fee = new Money(4),
            ValidFrom = 10,
            ValidUntil = 20,
            PubKey = new byte[] { 1, 2, 3 },
            Signature = new byte[] { 4, 5, 6 }
        });
        var random = new Random(0x4D555458);

        for (var iteration = 0; iteration < 4_000; iteration++)
        {
            var mutated = (byte[])seed.Clone();
            var changes = 1 + random.Next(4);
            for (var change = 0; change < changes; change++)
            {
                mutated[random.Next(mutated.Length)] ^= (byte)(1 << random.Next(8));
            }

            var exception = Record.Exception(() =>
            {
                _ = TransactionCodec.TryDecode(mutated, out _, out _);
            });

            Assert.Null(exception);
        }
    }

    [Fact]
    public void SnapshotCodec_MutatedPayloadsNeverEscapeAsExceptions()
    {
        var state = Genesis.CreateState(ChainInfo.ChainIdTestnet);
        var seed = StateSnapshotCodec.Encode(state);
        var random = new Random(0x534D5554);

        for (var iteration = 0; iteration < 2_000; iteration++)
        {
            var mutated = (byte[])seed.Clone();
            var changes = 1 + random.Next(8);
            for (var change = 0; change < changes; change++)
            {
                mutated[random.Next(mutated.Length)] ^= (byte)(1 << random.Next(8));
            }

            var exception = Record.Exception(() =>
            {
                _ = StateSnapshotCodec.TryDecode(mutated, out _, out _);
            });

            Assert.Null(exception);
        }
    }

    [Fact]
    public void SnapshotCodec_NullJsonPropertiesAreRejected()
    {
        var malformed = Encoding.UTF8.GetBytes(
            "{\"format\":1,\"chainId\":2,\"height\":0,\"timeStamp\":0,\"head\":null,\"stateRoot\":null,\"accounts\":null,\"stakes\":null}");

        var exception = Record.Exception(() =>
        {
            Assert.False(StateSnapshotCodec.TryDecode(malformed, out _, out var error));
            Assert.Contains("snapshot", error, StringComparison.OrdinalIgnoreCase);
        });

        Assert.Null(exception);
    }

    private static Address MakeAddress(byte value)
        => new(ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet), new[] { value, value, value, value });
}
