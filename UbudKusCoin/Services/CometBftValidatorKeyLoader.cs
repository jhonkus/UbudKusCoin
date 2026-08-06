using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UbudKusCoin.Services;

public static class CometBftValidatorKeyLoader
{
    public static byte[] TryLoadPublicKey()
    {
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
