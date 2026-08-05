using System.Buffers.Binary;
using System.Security.Cryptography;
using UbudKusCoin.Core.Hashing;

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
}
