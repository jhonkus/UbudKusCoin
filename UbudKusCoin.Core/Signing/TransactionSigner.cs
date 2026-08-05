using NBitcoin;
using UbudKusCoin.Core.Types;
using Transaction = UbudKusCoin.Core.Types.Transaction;

namespace UbudKusCoin.Core.Signing;

/// <summary>
/// Binds an ECDSA signature over the canonical transaction digest using the
/// secp256k1 curve. We delegate to NBitcoin's audited secp256k1 implementation
/// (the built-in .NET <c>ECDsa</c> does not support secp256k1) rather than
/// inventing our own crypto. The signature covers the canonical digest returned
/// by <see cref="Transaction.ComputeDigest"/>.
/// </summary>
public static class TransactionSigner
{
    /// <summary>
    /// Signs the canonical transaction digest with the given private key bytes.
    /// Returns the low-S DER signature as bytes.
    /// </summary>
public static byte[] Sign(Transaction tx, byte[] privateKeyBytes)
    {
        var key = new Key(privateKeyBytes);
        // Sign the canonical, fixed-length transaction id (double SHA-256 of the
        // digest). uint256 requires exactly 32 bytes.
        var id = new uint256(tx.ComputeId());
        var ecdsaSig = key.Sign(id);
        return ecdsaSig.ToDER();
    }

    /// <summary>
    /// Signs gracefully, returning false (and a null signature) if the private
    /// key is malformed, instead of throwing.
    /// </summary>
    public static bool TrySign(Transaction tx, byte[] privateKeyBytes, out byte[]? signature)
    {
        signature = null;
        try
        {
            signature = Sign(tx, privateKeyBytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies that <paramref name="signature"/> is a valid ECDSA signature of
    /// the canonical transaction digest by the given compressed public key.
    /// Returns false on any malformed input (never throws).
    /// </summary>
    public static bool Verify(Transaction tx, byte[] publicKeyBytes, byte[] signature)
    {
        try
        {
            if (publicKeyBytes.Length != 33 && publicKeyBytes.Length != 65)
            {
                return false;
            }

var pubKey = new PubKey(publicKeyBytes);
            var id = new uint256(tx.ComputeId());
            return pubKey.Verify(id, signature);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies a signature and additionally checks that the sender address
    /// matches the signer's public key (address = Base58Check(SHA256(pubkey))).
    /// </summary>
    public static bool VerifyForSender(Transaction tx, byte[] publicKeyBytes, byte[] signature)
    {
        if (!Verify(tx, publicKeyBytes, signature))
        {
            return false;
        }

        var signerAddress = Address.FromPublicKey(tx.From.Version, publicKeyBytes);
        return signerAddress.Encoded == tx.From.Encoded;
    }
}
