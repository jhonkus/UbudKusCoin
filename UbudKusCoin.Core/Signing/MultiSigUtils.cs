using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin;
using UbudKusCoin.Core.Hashing;
using UbudKusCoin.Core.Types;
using Transaction = UbudKusCoin.Core.Types.Transaction;

namespace UbudKusCoin.Core.Signing;

public sealed record MultiSigStructure(uint Threshold, IReadOnlyList<byte[]> PublicKeys, IReadOnlyList<byte[]> Signatures);

public static class MultiSigUtils
{
    /// <summary>
    /// Encodes multi-signature metadata (threshold, public keys, and collected DER signatures) into a byte payload.
    /// </summary>
    public static byte[] EncodeMultiSigPayload(uint threshold, IEnumerable<byte[]> publicKeys, IEnumerable<byte[]> signatures)
    {
        var keysList = publicKeys.Select(k => k.ToArray()).ToList();
        var sigsList = signatures.Select(s => s.ToArray()).ToList();

        using var ms = new MemoryStream();
        HashUtils.AppendLe32(ms, threshold);
        HashUtils.AppendLe32(ms, (uint)keysList.Count);
        foreach (var key in keysList)
        {
            HashUtils.AppendLengthPrefixed(ms, key);
        }

        HashUtils.AppendLe32(ms, (uint)sigsList.Count);
        foreach (var sig in sigsList)
        {
            HashUtils.AppendLengthPrefixed(ms, sig);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Decodes an encoded multi-signature payload into threshold, public keys, and signatures.
    /// </summary>
    public static bool TryDecodeMultiSigPayload(byte[] payload, out MultiSigStructure? structure)
    {
        structure = null;
        if (payload is null || payload.Length < 12)
        {
            return false;
        }

        try
        {
            using var ms = new MemoryStream(payload);
            using var br = new BinaryReader(ms);

            uint threshold = br.ReadUInt32();
            uint keyCount = br.ReadUInt32();
            if (keyCount == 0 || keyCount > 100 || threshold == 0 || threshold > keyCount)
            {
                return false;
            }

            var keys = new List<byte[]>();
            for (int i = 0; i < keyCount; i++)
            {
                int len = br.ReadInt32();
                if (len <= 0 || len > 100) return false;
                keys.Add(br.ReadBytes(len));
            }

            uint sigCount = br.ReadUInt32();
            if (sigCount > 100) return false;

            var sigs = new List<byte[]>();
            for (int i = 0; i < sigCount; i++)
            {
                int len = br.ReadInt32();
                if (len <= 0 || len > 200) return false;
                sigs.Add(br.ReadBytes(len));
            }

            structure = new MultiSigStructure(threshold, keys, sigs);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies a multi-signature transaction against the specified threshold, public keys, and signatures.
    /// Ensures that at least <paramref name="threshold"/> unique valid signatures match the multi-sig set.
    /// </summary>
    public static bool VerifyForMultiSigSender(Transaction tx, uint threshold, IEnumerable<byte[]> publicKeys, IEnumerable<byte[]> signatures)
    {
        if (tx is null || tx.From.Payload is null)
        {
            return false;
        }

        var keysList = publicKeys.Select(k => k.ToArray()).ToList();
        var sigsList = signatures.Select(s => s.ToArray()).ToList();

        if (threshold == 0 || threshold > keysList.Count || sigsList.Count < threshold)
        {
            return false;
        }

        // Verify derived address matches tx.From
        var derivedAddress = Address.FromMultiSig(tx.From.Version, threshold, keysList);
        if (derivedAddress.Encoded != tx.From.Encoded)
        {
            return false;
        }

        var txId = new uint256(tx.ComputeId());
        var verifiedSigners = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sig in sigsList)
        {
            foreach (var keyBytes in keysList)
            {
                var keyHex = Convert.ToHexString(keyBytes);
                if (verifiedSigners.Contains(keyHex))
                {
                    continue;
                }

                try
                {
                    var pubKey = new PubKey(keyBytes);
                    if (pubKey.Verify(txId, sig))
                    {
                        verifiedSigners.Add(keyHex);
                        break;
                    }
                }
                catch
                {
                    // Invalid key or signature byte format
                }
            }
        }

        return verifiedSigners.Count >= threshold;
    }
}
