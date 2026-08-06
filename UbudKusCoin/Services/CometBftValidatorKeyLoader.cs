using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NBitcoin.DataEncoders;

namespace UbudKusCoin.Services;

public static class CometBftValidatorKeyLoader
{
    public static byte[] TryLoadPublicKey()
    {
        var configured = DotNetEnv.Env.GetString("COMETBFT_VALIDATOR_PUBKEY_HEX", string.Empty);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            try
            {
                return Encoders.Hex.DecodeData(configured);
            }
            catch (FormatException)
            {
                return Array.Empty<byte>();
            }
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
            return string.IsNullOrWhiteSpace(value) ? Array.Empty<byte>() : Convert.FromBase64String(value);
        }
        catch (Exception exception) when (exception is IOException or JsonException or KeyNotFoundException or FormatException)
        {
            return Array.Empty<byte>();
        }
    }
}
