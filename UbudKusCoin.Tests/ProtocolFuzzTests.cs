using System;
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
}
