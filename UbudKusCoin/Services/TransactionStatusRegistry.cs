#nullable enable

using System;
using System.Collections.Concurrent;

namespace UbudKusCoin.Services;

public sealed record TransactionStatusSnapshot(
    string TxId,
    string Status,
    string Message,
    long? Height,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Tracks transaction admission between ABCI CheckTx and canonical commit.
/// Canonical chain lookup remains authoritative after a process restart.
/// </summary>
public static class TransactionStatusRegistry
{
    private static readonly ConcurrentDictionary<string, TransactionStatusSnapshot> Entries = new(StringComparer.OrdinalIgnoreCase);

    public static void MarkPending(string txId, string message)
        => Entries[Normalize(txId)] = new TransactionStatusSnapshot(
            Normalize(txId), "pending", message, null, DateTimeOffset.UtcNow);

    public static void MarkConfirmed(string txId, long height)
        => Entries[Normalize(txId)] = new TransactionStatusSnapshot(
            Normalize(txId), "confirmed", "Transaction committed in the canonical chain.", height, DateTimeOffset.UtcNow);

    public static void MarkRejected(string txId, string message)
        => Entries[Normalize(txId)] = new TransactionStatusSnapshot(
            Normalize(txId), "rejected", message, null, DateTimeOffset.UtcNow);

    public static bool TryGet(string txId, out TransactionStatusSnapshot? snapshot)
        => Entries.TryGetValue(Normalize(txId), out snapshot);

    private static string Normalize(string txId)
        => txId.Trim().ToLowerInvariant();
}
