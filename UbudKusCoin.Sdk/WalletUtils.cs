using System;
using System.Collections.Generic;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Sdk;

/// <summary>
/// Static utility methods for address derivation and key verification.
/// </summary>
public static class WalletUtils
{
    /// <summary>
    /// Derives a standard single-signature address from a public key.
    /// </summary>
    public static string DeriveAddress(byte[] publicKey, uint chainId)
    {
        var version = ChainInfo.AddressVersion(chainId);
        return Address.FromPublicKey(version, publicKey).Encoded;
    }

    /// <summary>
    /// Derives a multi-signature address from a set of public keys and threshold.
    /// </summary>
    public static string DeriveMultiSigAddress(int threshold, IEnumerable<byte[]> publicKeys, uint chainId)
    {
        var version = ChainInfo.AddressVersion(chainId);
        return Address.FromMultiSig(version, (uint)threshold, publicKeys).Encoded;
    }
}
