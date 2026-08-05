namespace UbudKusCoin.Core.Types;

/// <summary>Result of attempting to add a transaction to the mempool.</summary>
public sealed class MempoolAddResult
{
    public bool Accepted { get; }
    public string? Reason { get; }

    private MempoolAddResult(bool accepted, string? reason)
    {
        Accepted = accepted;
        Reason = reason;
    }

    public static MempoolAddResult Ok() => new(true, null);
    public static MempoolAddResult Reject(string reason) => new(false, reason);
}

/// <summary>
/// A deterministic, bounded, spam-resistant transaction pool. It holds only
/// transactions that pass the envelope, signature, nonce-ordering, and balance
/// checks against a given <see cref="State"/>. It enforces a total cap and a
/// per-sender cap, and de-duplicates by canonical transaction id.
///
/// The pool is pure (no I/O). Removal is explicit by id (or for a block's set),
/// never a blind "clear everything", so a block that only includes some txs does
/// not drop the rest.
/// </summary>
public sealed class Mempool
{
    private readonly uint _chainId;
    private readonly Dictionary<string, Transaction> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _bySender = new(StringComparer.Ordinal);

    private int _maxTotal = FeePolicy.MaxTotalPending;
    private int _maxPerSender = FeePolicy.MaxPendingPerSender;

    public Mempool(uint chainId)
    {
        _chainId = chainId;
    }

    /// <summary>Total pending transactions.</summary>
    public int Count => _byId.Count;

    /// <summary>All pending transaction ids (canonical hex).</summary>
    public IReadOnlyCollection<string> Ids => _byId.Keys;

    /// <summary>All pending transactions.</summary>
    public IReadOnlyCollection<Transaction> Transactions => _byId.Values;

    /// <summary>Whether the pool already contains the transaction id.</summary>
    public bool Contains(string txIdHex) => _byId.ContainsKey(txIdHex);

    public bool Contains(Transaction tx) => _byId.ContainsKey(tx.ComputeIdHex());

    /// <summary>Gets a pending transaction by its canonical id.</summary>
    public Transaction? Get(string txIdHex)
        => _byId.TryGetValue(txIdHex, out var tx) ? tx : null;

    /// <summary>
    /// Attempts to add a transaction. Returns a rejected result with a specific
    /// reason otherwise. The transaction must be well-formed, correctly signed,
    /// have the correct next nonce for its sender, and leave the sender with a
    /// non-negative balance after this and all other pending txs from that sender.
    /// </summary>
    public MempoolAddResult Add(Transaction tx, State state, long nowUnixSeconds)
    {
        if (!tx.IsEnvelopeWellFormed(_chainId))
        {
            return MempoolAddResult.Reject("Malformed transaction envelope.");
        }

        var id = tx.ComputeIdHex();
        if (_byId.ContainsKey(id))
        {
            return MempoolAddResult.Reject("Duplicate transaction.");
        }

        if (_byId.Count >= _maxTotal)
        {
            return MempoolAddResult.Reject("Mempool is full.");
        }

        if (!tx.VerifySignature())
        {
            return MempoolAddResult.Reject("Invalid signature.");
        }

        // Time-lock enforcement against the current time.
        if (tx.ValidFrom > 0 && nowUnixSeconds < tx.ValidFrom)
        {
            return MempoolAddResult.Reject("Transaction is not valid yet.");
        }

        if (tx.ValidUntil > 0 && nowUnixSeconds > tx.ValidUntil)
        {
            return MempoolAddResult.Reject("Transaction has expired.");
        }

        if (!_bySender.TryGetValue(tx.From.Encoded, out var senderSet))
        {
            senderSet = new HashSet<string>(StringComparer.Ordinal);
            _bySender[tx.From.Encoded] = senderSet;
        }

        if (senderSet.Count >= _maxPerSender)
        {
            return MempoolAddResult.Reject("Per-sender mempool limit reached.");
        }

        var account = state.GetAccount(tx.From);
        if (account is null)
        {
            return MempoolAddResult.Reject("Sender account does not exist.");
        }

        // The next nonce must equal the account nonce + number of pending txs
        // already queued for this sender (enforce strict nonce ordering).
        ulong expectedNonce = account.Nonce + 1 + (ulong)senderSet.Count;
        if (tx.Nonce != expectedNonce)
        {
            return MempoolAddResult.Reject("Invalid nonce (out of order).");
        }

        // Balance check includes all pending txs from this sender.
        Money pendingSpend = Money.Zero;
        foreach (var pendingId in senderSet)
        {
            var pending = _byId[pendingId];
            pendingSpend += pending.Amount + pending.Fee;
        }

        if ((tx.Amount + tx.Fee + pendingSpend) > account.Balance)
        {
            return MempoolAddResult.Reject("Insufficient balance.");
        }

        _byId[id] = tx;
        senderSet.Add(id);
        return MempoolAddResult.Ok();
    }

    /// <summary>Removes a single transaction by id (e.g., when it is mined).</summary>
    public bool Remove(string txIdHex)
    {
        if (!_byId.TryGetValue(txIdHex, out var tx))
        {
            return false;
        }

        _byId.Remove(txIdHex);
        if (_bySender.TryGetValue(tx.From.Encoded, out var senderSet))
        {
            senderSet.Remove(txIdHex);
            if (senderSet.Count == 0)
            {
                _bySender.Remove(tx.From.Encoded);
            }
        }

        return true;
    }

    /// <summary>
    /// Removes a set of transactions (e.g., those included in a block). This is
    /// the safe replacement for the old blind <c>DeleteAll</c> — it only drops
    /// the txs that were actually committed, leaving the rest intact.
    /// </summary>
    public void RemoveRange(IEnumerable<Transaction> mined)
    {
        foreach (var tx in mined)
        {
            Remove(tx.ComputeIdHex());
        }
    }

    /// <summary>Removes all transactions whose sender is the given account.</summary>
    public void RemoveBySender(string senderEncoded)
    {
        if (!_bySender.TryGetValue(senderEncoded, out var senderSet))
        {
            return;
        }

        foreach (var id in senderSet.ToList())
        {
            _byId.Remove(id);
        }

        _bySender.Remove(senderEncoded);
    }

    /// <summary>Clears the entire pool (used only on explicit reset/snapshot restore).</summary>
    public void Clear()
    {
        _byId.Clear();
        _bySender.Clear();
    }
}
