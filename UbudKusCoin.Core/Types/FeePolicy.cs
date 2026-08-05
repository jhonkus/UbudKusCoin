namespace UbudKusCoin.Core.Types;

/// <summary>
/// Deterministic fee and size policy for the mempool and block validation.
/// All values are integer fixed-point base units (see <see cref="Money"/>).
/// </summary>
public static class FeePolicy
{
    /// <summary>Minimum fee a transaction must offer to be relayed/accepted into the mempool.</summary>
    public static readonly Money MinRelayFee = Money.FromCoins(0.0001m);

    /// <summary>Base transaction fee paid by the sender (in addition to any tip).</summary>
    public static readonly Money BaseFee = Money.FromCoins(0.0001m);

    /// <summary>Maximum total fee a single transaction may declare (anti-overspend on the chain).</summary>
    public static readonly Money MaxFeePerTx = Money.FromCoins(10m);

    /// <summary>Maximum serialized size of a single transaction envelope (bytes).</summary>
    public const int MaxTxSizeBytes = 1024;

    /// <summary>Maximum number of transactions a single sender may have pending in the mempool.</summary>
    public const int MaxPendingPerSender = 64;

    /// <summary>Maximum total number of transactions in the mempool (bounded memory).</summary>
    public const int MaxTotalPending = 10_000;

    /// <summary>Maximum serialized size of a single transaction's public key (compressed secp256k1 = 33 bytes).</summary>
    public const int MaxPubKeyBytes = 33;

    /// <summary>Maximum serialized size of an ECDSA signature (DER, ~70-72 bytes).</summary>
    public const int MaxSignatureBytes = 72;
}
