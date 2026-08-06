using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class TransactionStatusRegistryTests
{
    [Fact]
    public void StatusTransitionsAreCaseInsensitiveAndKeepLatestState()
    {
        const string txId = "ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789";

        TransactionStatusRegistry.MarkPending(txId, "accepted by mempool");
        Assert.True(TransactionStatusRegistry.TryGet(txId.ToLowerInvariant(), out var pending));
        Assert.Equal("pending", pending!.Status);

        TransactionStatusRegistry.MarkConfirmed(txId.ToLowerInvariant(), 12);
        Assert.True(TransactionStatusRegistry.TryGet(txId, out var confirmed));
        Assert.Equal("confirmed", confirmed!.Status);
        Assert.Equal(12, confirmed.Height);
    }
}
