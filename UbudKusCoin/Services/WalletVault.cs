#nullable enable
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using NBitcoin;

namespace UbudKusCoin.Services;

public sealed record WalletVaultSnapshot(string MnemonicWords, int DerivationPath, DateTimeOffset CreatedAtUtc);

public static class WalletVault
{
    private const string DefaultAlgorithm = "auto";
    private const string DpapiAlgorithm = "dpapi";
    private const string AesGcmAlgorithm = "aes-gcm";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool TryLoad(string path, out WalletVaultSnapshot? snapshot)
    {
        snapshot = null;
        if (!File.Exists(path))
        {
            return false;
        }

        var envelope = JsonSerializer.Deserialize<WalletVaultEnvelope>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("Wallet vault is empty.");
        byte[] payload;
        if (string.Equals(envelope.Algorithm, DpapiAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new PlatformNotSupportedException("DPAPI wallet vaults require Windows.");
            }

            payload = DecryptDpapi(envelope);
        }
        else if (string.Equals(envelope.Algorithm, AesGcmAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            payload = DecryptAesGcm(envelope);
        }
        else
        {
            throw new InvalidDataException($"Unsupported wallet vault algorithm '{envelope.Algorithm}'.");
        }

        snapshot = JsonSerializer.Deserialize<WalletVaultSnapshot>(payload, JsonOptions)
            ?? throw new InvalidDataException("Wallet vault snapshot is empty.");
        return true;
    }

    public static WalletVaultSnapshot LoadOrCreate(string path, string? mnemonicWords = null, int derivationPath = 0)
    {
        if (TryLoad(path, out var existing) && existing is not null)
        {
            return existing;
        }

        var words = string.IsNullOrWhiteSpace(mnemonicWords)
            ? new Mnemonic(Wordlist.English, WordCount.Twelve).ToString()
            : new Mnemonic(mnemonicWords).ToString();
        var snapshot = new WalletVaultSnapshot(words, derivationPath, DateTimeOffset.UtcNow);
        Save(path, snapshot);
        return snapshot;
    }

    public static void Save(string path, WalletVaultSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(snapshot);

        var payload = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
        var envelope = CreateEnvelope(payload);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(envelope, JsonOptions));
        File.Move(temporary, path, true);
    }

    private static WalletVaultEnvelope CreateEnvelope(byte[] payload)
    {
        var mode = DotNetEnv.Env.GetString("WALLET_STORAGE_MODE", DefaultAlgorithm).Trim().ToLowerInvariant();
        if (mode == DefaultAlgorithm)
        {
            mode = OperatingSystem.IsWindows() ? DpapiAlgorithm : AesGcmAlgorithm;
        }

        return mode switch
        {
            DpapiAlgorithm when OperatingSystem.IsWindows() => CreateDpapiEnvelope(payload),
            DpapiAlgorithm => throw new InvalidOperationException(
                "WALLET_STORAGE_MODE=dpapi is only supported on Windows."),
            AesGcmAlgorithm => EncryptAesGcm(payload),
            _ => throw new InvalidOperationException("WALLET_STORAGE_MODE must be auto, dpapi, or aes-gcm.")
        };
    }

    private static WalletVaultEnvelope EncryptAesGcm(byte[] payload)
    {
        var key = LoadAesKey();
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[payload.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(key, 16))
        {
            aes.Encrypt(nonce, payload, ciphertext, tag);
        }

        return new WalletVaultEnvelope
        {
            Version = 1,
            Algorithm = AesGcmAlgorithm,
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            Ciphertext = Convert.ToBase64String(ciphertext)
        };
    }

    private static byte[] DecryptAesGcm(WalletVaultEnvelope envelope)
    {
        var key = LoadAesKey();
        var nonce = Convert.FromBase64String(envelope.Nonce ?? string.Empty);
        var tag = Convert.FromBase64String(envelope.Tag ?? string.Empty);
        var ciphertext = Convert.FromBase64String(envelope.Ciphertext ?? string.Empty);
        var plaintext = new byte[ciphertext.Length];
        using (var aes = new AesGcm(key, 16))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return plaintext;
    }

    [SupportedOSPlatform("windows")]
    private static WalletVaultEnvelope CreateDpapiEnvelope(byte[] payload)
        => new()
        {
            Version = 1,
            Algorithm = DpapiAlgorithm,
            Payload = Convert.ToBase64String(ProtectedData.Protect(payload, null, DataProtectionScope.CurrentUser))
        };

    [SupportedOSPlatform("windows")]
    private static byte[] DecryptDpapi(WalletVaultEnvelope envelope)
        => ProtectedData.Unprotect(
            Convert.FromBase64String(envelope.Payload ?? string.Empty),
            null,
            DataProtectionScope.CurrentUser);

    private static byte[] LoadAesKey()
    {
        var configured = DotNetEnv.Env.GetString("WALLET_ENCRYPTION_KEY", string.Empty);
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException(
                "WALLET_ENCRYPTION_KEY is required when WALLET_STORAGE_MODE=aes-gcm or auto on non-Windows hosts.");
        }

        var key = Convert.FromBase64String(configured);
        if (key.Length != 32)
        {
            throw new InvalidOperationException("WALLET_ENCRYPTION_KEY must be a base64-encoded 32-byte key.");
        }

        return key;
    }

    private sealed class WalletVaultEnvelope
    {
        public int Version { get; set; }
        public string Algorithm { get; set; } = string.Empty;
        public string? Payload { get; set; }
        public string? Nonce { get; set; }
        public string? Tag { get; set; }
        public string? Ciphertext { get; set; }
    }
}
