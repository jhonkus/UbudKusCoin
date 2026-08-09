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

    /// <summary>
    /// Target number of transactions per block used by the dynamic fee adjustment
    /// mechanism. When a block's tx count exceeds this target, the next base fee
    /// increases; when it is below, the base fee decreases.
    /// </summary>
    public const int TargetTxCountPerBlock = 5;

    /// <summary>
    /// Maximum per-block base fee adjustment magnitude expressed as a fraction
    /// (1/8 = 12.5%). Mirrors EIP-1559's 12.5% elasticity bound.
    /// </summary>
    public const int BaseFeeChangeDenominator = 8;

    /// <summary>
    /// Computes the dynamic base fee for the next block from the parent block's
    /// transaction count and base fee. The algorithm is modelled on EIP-1559:
    /// <list type="bullet">
    ///   <item>If parentTxCount &gt; TargetTxCountPerBlock, increase by up to 12.5%.</item>
    ///   <item>If parentTxCount &lt; TargetTxCountPerBlock, decrease by up to 12.5%.</item>
    ///   <item>Result is clamped to [MinRelayFee, MaxFeePerTx].</item>
    /// </list>
    /// </summary>
    public static Money GetDynamicBaseFee(int parentTxCount, Money parentBaseFee)
    {
        long current = parentBaseFee.BaseUnits;

        long next;
        if (parentTxCount > TargetTxCountPerBlock)
        {
            long delta = Math.Max(1L,
                current * (parentTxCount - TargetTxCountPerBlock)
                / (TargetTxCountPerBlock * BaseFeeChangeDenominator));
            next = current + delta;
        }
        else if (parentTxCount < TargetTxCountPerBlock)
        {
            long delta = Math.Max(1L,
                current * (TargetTxCountPerBlock - parentTxCount)
                / (TargetTxCountPerBlock * BaseFeeChangeDenominator));
            next = current - delta;
        }
        else
        {
            next = current;
        }

        // Clamp to [MinRelayFee, MaxFeePerTx].
        next = Math.Clamp(next, MinRelayFee.BaseUnits, MaxFeePerTx.BaseUnits);
        return new Money(next);
    }
}
