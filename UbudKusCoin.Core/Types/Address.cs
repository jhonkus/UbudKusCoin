using System.Security.Cryptography;
using UbudKusCoin.Core.Hashing;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// Versioned, checksummed address (Base58Check-style). The version byte
/// separates networks (mainnet vs testnet) so a testnet address can never be
/// used on mainnet, and the 4-byte checksum prevents typo-induced fund loss.
/// </summary>
public readonly struct Address
{
    public const byte MainnetVersion = 0x00;
    public const byte TestnetVersion = 0x6F;

    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    public byte Version { get; }
    public byte[] Payload { get; }
    public string Encoded { get; }

    public Address(byte version, byte[] payload)
    {
        if (payload is null || payload.Length == 0)
        {
            throw new ArgumentException("Address payload cannot be empty.", nameof(payload));
        }

        Version = version;
        Payload = payload;
        Encoded = Encode(version, payload);
    }

    /// <summary>Creates an address from a compressed public key (payload = SHA-256 of pubkey).</summary>
    public static Address FromPublicKey(byte version, ReadOnlySpan<byte> compressedPubKey)
    {
        byte[] payload = HashUtils.Sha256(compressedPubKey);
        return new Address(version, payload);
    }

    public static Address Parse(string encoded)
    {
        if (!TryParse(encoded, out Address addr))
        {
            throw new FormatException("Invalid UbudKusCoin address.");
        }

        return addr;
    }

    public static bool TryParse(string encoded, out Address address)
    {
        address = default;
        if (string.IsNullOrWhiteSpace(encoded))
        {
            return false;
        }

        byte[] decoded;
        try
        {
            decoded = Base58Decode(encoded);
        }
        catch
        {
            return false;
        }

        // version(1) + payload + checksum(4)
        if (decoded.Length < 6)
        {
            return false;
        }

        byte version = decoded[0];
        byte[] payload = decoded[1..^4];
        byte[] checksum = decoded[^4..];

        byte[] expected = HashUtils.DoubleSha256(decoded[..^4])[..4];
        if (!CryptographicOperations.FixedTimeEquals(checksum, expected))
        {
            return false;
        }

        address = new Address(version, payload);
        return true;
    }

    private static string Encode(byte version, byte[] payload)
    {
        byte[] withVersion = new byte[1 + payload.Length];
        withVersion[0] = version;
        payload.CopyTo(withVersion, 1);

        byte[] checksum = HashUtils.DoubleSha256(withVersion)[..4];

        byte[] full = new byte[withVersion.Length + 4];
        withVersion.CopyTo(full, 0);
        checksum.CopyTo(full, withVersion.Length);

        return Base58Encode(full);
    }

    private static string Base58Encode(byte[] data)
    {
        var result = new System.Text.StringBuilder();
        int zeros = 0;
        while (zeros < data.Length && data[zeros] == 0)
        {
            zeros++;
            result.Append('1');
        }

        var number = data.Skip(zeros).ToArray();
        if (number.Length == 0)
        {
            return result.ToString();
        }

        var encoded = new System.Collections.Generic.List<byte>();
        var x = number.ToList();
        while (x.Count > 0)
        {
            int remainder = 0;
            var quotient = new System.Collections.Generic.List<byte>();
            foreach (byte b in x)
            {
                int acc = remainder * 256 + b;
                quotient.Add((byte)(acc / 58));
                remainder = acc % 58;
            }

            encoded.Add((byte)remainder);
            x = quotient.SkipWhile(q => q == 0).ToList();
        }

        for (int i = encoded.Count - 1; i >= 0; i--)
        {
            result.Append(Base58Alphabet[encoded[i]]);
        }

        return result.ToString();
    }

    private static byte[] Base58Decode(string input)
    {
        int zeros = 0;
        while (zeros < input.Length && input[zeros] == '1')
        {
            zeros++;
        }

        var number = input.Skip(zeros)
            .Select(c => Base58Alphabet.IndexOf(c))
            .ToList();

        if (number.Any(c => c < 0))
        {
            throw new FormatException("Address contains invalid Base58 character.");
        }

var decoded = new System.Collections.Generic.List<byte>();
        var b58 = number.ToList();
        while (b58.Count > 0)
        {
            int remainder = 0;
            var quotient = new System.Collections.Generic.List<int>();
            foreach (int digit in b58)
            {
                int acc = remainder * 58 + digit;
                quotient.Add(acc / 256);
                remainder = acc % 256;
            }

            decoded.Add((byte)remainder);
            b58 = quotient.SkipWhile(q => q == 0).ToList();
        }

        for (int i = 0; i < zeros; i++)
        {
            decoded.Add(0);
        }

        decoded.Reverse();
        return decoded.ToArray();
    }

    public override string ToString() => Encoded;
    public override bool Equals(object? obj) => obj is Address a && a.Encoded == Encoded;
    public override int GetHashCode() => Encoded.GetHashCode();
}
