using System.Buffers.Binary;
using System.Security.Cryptography;
using UbudKusCoin.Core.Hashing;
using UbudKusCoin.Core.Signing;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// Canonical, versioned transaction envelope. Amount and fee are integer
/// fixed-point (see <see cref="Money"/>). A per-sender monotonic nonce plus the
/// chain id provide replay protection. The signature covers only the canonical
/// digest (never string concatenation, never a hash-of-hash).
/// </summary>
public sealed class Transaction
{
    public uint Version { get; set; } = ChainInfo.TxVersion;
    public uint ChainId { get; set; } = ChainInfo.ChainIdUndefined;
    public ulong Nonce { get; set; }
    public Address From { get; set; }
    public Address To { get; set; }
    public Money Amount { get; set; }
    public Money Fee { get; set; }
    public long ValidFrom { get; set; }   // unix seconds (0 = no restriction)
    public long ValidUntil { get; set; }  // unix seconds (0 = no expiry)
    public byte[] PubKey { get; set; } = Array.Empty<byte>(); // compressed ECDSA pubkey
    public byte[] Signature { get; set; } = Array.Empty<byte>(); // DER ECDSA over digest

    /// <summary>
    /// Canonical, collision-resistant digest of all signed fields. The
    /// signature is NOT included (it signs this digest). Deterministic across
    /// nodes and languages.
    /// </summary>
    public byte[] ComputeDigest()
    {
        using var ms = new MemoryStream();
        HashUtils.AppendLe32(ms, Version);
        HashUtils.AppendLe32(ms, ChainId);
        HashUtils.AppendLe64(ms, Nonce);
        HashUtils.AppendLengthPrefixed(ms, From.Encoded);
        HashUtils.AppendLengthPrefixed(ms, To.Encoded);
        HashUtils.AppendLe64(ms, (ulong)Amount.BaseUnits);
        HashUtils.AppendLe64(ms, (ulong)Fee.BaseUnits);
        HashUtils.AppendLe64(ms, (ulong)ValidFrom);
        HashUtils.AppendLe64(ms, (ulong)ValidUntil);
        HashUtils.AppendLengthPrefixed(ms, PubKey);
        return ms.ToArray();
    }

    /// <summary>Canonical transaction id (= double SHA-256 of the digest).</summary>
    public byte[] ComputeId()
    {
        return HashUtils.DoubleSha256(ComputeDigest());
    }

public string ComputeIdHex()
    {
        return Convert.ToHexStringLower(ComputeId());
    }

    /// <summary>
    /// Returns the approximate serialized size of this transaction envelope in
    /// bytes. Used to enforce the size policy before a transaction is accepted.
    /// </summary>
    public int ComputeSerializedSize()
    {
        return 4             // version
             + 4             // chain id
             + 8             // nonce
             + From.Encoded.Length
             + To.Encoded.Length
             + 8             // amount
             + 8             // fee
             + 8             // valid_from
             + 8             // valid_until
             + PubKey.Length
             + Signature.Length;
    }

    /// <summary>
    /// Validates the envelope's syntax and policy (not the signature, nonce
    /// ordering, or balance — those are checked by the mempool/state machine).
    /// This is the first gate before any signature work is done.
    /// </summary>
    public bool IsEnvelopeWellFormed(uint chainId)
    {
        if (Version != ChainInfo.TxVersion)
        {
            return false;
        }

        if (ChainId != chainId)
        {
            return false;
        }

        if (From.Encoded is null || To.Encoded is null)
        {
            return false;
        }

        // Sender and recipient must belong to the same network as the chain.
        if (From.Version != ChainInfo.AddressVersion(chainId) ||
            To.Version != ChainInfo.AddressVersion(chainId))
        {
            return false;
        }

        // No self-transactions, no null recipient.
        if (From.Encoded == To.Encoded)
        {
            return false;
        }

        if (Amount.BaseUnits <= 0)
        {
            return false;
        }

        if (Fee < FeePolicy.MinRelayFee)
        {
            return false;
        }

        if (Fee > FeePolicy.MaxFeePerTx)
        {
            return false;
        }

        if (PubKey.Length is < 33 or > FeePolicy.MaxPubKeyBytes)
        {
            return false;
        }

        if (Signature.Length is <= 0 or > FeePolicy.MaxSignatureBytes)
        {
            return false;
        }

        if (ComputeSerializedSize() > FeePolicy.MaxTxSizeBytes)
        {
            return false;
        }

        // Time-lock sanity: valid_until must be after valid_from (when both set).
        if (ValidFrom > 0 && ValidUntil > 0 && ValidUntil <= ValidFrom)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Verifies the ECDSA signature over the canonical digest and that the
    /// signer's public key matches the sender address. Never throws.
    /// </summary>
    public bool VerifySignature()
    {
        return TransactionSigner.VerifyForSender(this, PubKey, Signature);
    }
}
