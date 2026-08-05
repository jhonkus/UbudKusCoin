using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace UbudKusCoin.Core.Hashing;

/// <summary>
/// Canonical, deterministic hashing primitives for the UbudKusCoin protocol.
///
/// All multi-byte integers are serialized little-endian; all variable-length
/// fields are length-prefixed (4-byte little-endian length) to eliminate
/// string-concatenation ambiguity. This is the foundation for reproducible
/// transaction and block hashes across nodes and languages.
/// </summary>
public static class HashUtils
{
    /// <summary>SHA-256 of the given bytes.</summary>
    public static byte[] Sha256(ReadOnlySpan<byte> data)
    {
        return SHA256.HashData(data);
    }

    /// <summary>Lowercase hex of the SHA-256 of the given bytes.</summary>
    public static string Sha256Hex(ReadOnlySpan<byte> data)
    {
        return Convert.ToHexStringLower(Sha256(data));
    }

    /// <summary>Double SHA-256 (SHA-256 of SHA-256), used for checksums.</summary>
    public static byte[] DoubleSha256(ReadOnlySpan<byte> data)
    {
        return Sha256(Sha256(data));
    }

    /// <summary>Appends a 4-byte little-endian unsigned integer.</summary>
    public static void AppendLe32(MemoryStream ms, uint value)
    {
        Span<byte> tmp = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(tmp, value);
        ms.Write(tmp);
    }

    /// <summary>Appends an 8-byte little-endian unsigned integer.</summary>
    public static void AppendLe64(MemoryStream ms, ulong value)
    {
        Span<byte> tmp = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(tmp, value);
        ms.Write(tmp);
    }

    /// <summary>Appends a length-prefixed byte field (4-byte little-endian length).</summary>
    public static void AppendLengthPrefixed(MemoryStream ms, ReadOnlySpan<byte> field)
    {
        AppendLe32(ms, (uint)field.Length);
        ms.Write(field);
    }

    /// <summary>Appends a length-prefixed UTF-8 string field.</summary>
    public static void AppendLengthPrefixed(MemoryStream ms, string value)
    {
        AppendLengthPrefixed(ms, Encoding.UTF8.GetBytes(value));
    }

    /// <summary>Appends raw bytes with no length prefix.</summary>
    public static void AppendRaw(MemoryStream ms, ReadOnlySpan<byte> data)
    {
        ms.Write(data);
    }
}
