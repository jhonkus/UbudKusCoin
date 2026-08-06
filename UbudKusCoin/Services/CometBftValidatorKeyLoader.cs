using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NBitcoin.DataEncoders;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Services;

public static class CometBftValidatorKeyLoader
{
    public static byte[] LoadRequiredPublicKey(bool requireExplicitConfiguration = false)
    {
        var publicKey = LoadConfiguredPublicKey(requireExplicitConfiguration);
        ValidatePublicKey(publicKey);

        var genesisKeys = TryLoadGenesisPublicKeys();
        if (genesisKeys.Count > 0 && !genesisKeys.Any(x => x.SequenceEqual(publicKey)))
        {
            throw new InvalidDataException(
                "The local CometBFT validator key is not present in the configured genesis validator set.");
        }

        return publicKey;
    }

    public static byte[] LoadConfiguredPublicKey(bool requireExplicitConfiguration = false)
    {
        var publicKey = TryLoadPublicKey(requireExplicitConfiguration);
        ValidatePublicKey(publicKey);
        return publicKey;
    }

    public static void ValidatePublicKey(byte[] publicKey)
    {
        if (publicKey is null || publicKey.Length != 32)
        {
            throw new InvalidDataException("A CometBFT validator public key must be exactly 32 Ed25519 bytes.");
        }
    }

    public static bool IsGenesisOrActiveConsensusKey(byte[] publicKey, State state)
    {
        ValidatePublicKey(publicKey);
        ArgumentNullException.ThrowIfNull(state);

        return TryLoadGenesisPublicKeys().Any(x => x.SequenceEqual(publicKey))
            || state.Stakes.Any(x => x.ConsensusPubKey.SequenceEqual(publicKey));
    }

    public static byte[] TryLoadPublicKey(bool requireExplicitConfiguration = false)
    {
        var configured = DotNetEnv.Env.GetString("COMETBFT_VALIDATOR_PUBKEY_HEX", string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                var publicKey = Encoders.Hex.DecodeData(configured);
                ValidatePublicKey(publicKey);
                return publicKey;
            }
            catch (Exception exception) when (exception is FormatException or InvalidDataException)
            {
                return Array.Empty<byte>();
            }
        }

        if (requireExplicitConfiguration)
        {
            return Array.Empty<byte>();
        }

        var home = DotNetEnv.Env.GetString("COMETBFT_HOME", string.Empty);
        if (string.IsNullOrWhiteSpace(home))
        {
            return Array.Empty<byte>();
        }

        var path = Path.Combine(home, "config", "priv_validator_key.json");
        if (!File.Exists(path))
        {
            return Array.Empty<byte>();
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var value = document.RootElement
                .GetProperty("pub_key")
                .GetProperty("value")
                .GetString();
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<byte>();

            var publicKey = Convert.FromBase64String(value);
            ValidatePublicKey(publicKey);
            return publicKey;
        }
        catch (Exception exception) when (exception is IOException or JsonException or KeyNotFoundException
            or FormatException or InvalidDataException)
        {
            return Array.Empty<byte>();
        }
    }

    public static Address? TryResolveApplicationAddress(byte[] cometAddress, uint chainId)
    {
        if (cometAddress is null || cometAddress.Length == 0)
        {
            return null;
        }

        foreach (var publicKey in TryLoadGenesisPublicKeys())
        {
            var derivedCometAddress = System.Security.Cryptography.SHA256.HashData(publicKey)[..20];
            if (derivedCometAddress.SequenceEqual(cometAddress))
            {
                return Address.FromPublicKey(ChainInfo.AddressVersion(chainId), publicKey);
            }
        }

        var localPublicKey = TryLoadPublicKey();
        if (localPublicKey.Length > 0
            && System.Security.Cryptography.SHA256.HashData(localPublicKey)[..20].SequenceEqual(cometAddress))
        {
            return Address.FromPublicKey(ChainInfo.AddressVersion(chainId), localPublicKey);
        }

        return null;
    }

    private static IReadOnlyList<byte[]> TryLoadGenesisPublicKeys()
    {
        var home = DotNetEnv.Env.GetString("COMETBFT_HOME", string.Empty);
        var path = string.IsNullOrWhiteSpace(home)
            ? string.Empty
            : Path.Combine(home, "config", "genesis.json");
        if (!File.Exists(path))
        {
            return Array.Empty<byte[]>();
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("validators")
                .EnumerateArray()
                .Select(x => Convert.FromBase64String(x.GetProperty("pub_key").GetProperty("value").GetString()!))
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or JsonException or KeyNotFoundException or FormatException)
        {
            return Array.Empty<byte[]>();
        }
    }
}
