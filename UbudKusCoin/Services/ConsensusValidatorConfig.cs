#nullable enable

using System;
using System.Globalization;
using System.Linq;
using NBitcoin.DataEncoders;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Services;

public static class ConsensusValidatorConfig
{
    /// <summary>
    /// Parses VALIDATOR_SET as address:compressed_pubkey_hex:stake_coins entries.
    /// A blank value is a deliberate single-validator development mode.
    /// </summary>
    public static ValidatorSet Load(uint chainId, WalletService wallet, byte[]? defaultPublicKey = null)
    {
        var raw = DotNetEnv.Env.GetString("VALIDATOR_SET", string.Empty);
        if (string.IsNullOrWhiteSpace(raw))
        {
            var pubKey = defaultPublicKey is { Length: > 0 }
                ? defaultPublicKey
                : wallet.GetPublicKey().PubKey.ToBytes();
            var address = Address.FromPublicKey(ChainInfo.AddressVersion(chainId), pubKey);
            return new ValidatorSet(new[] { new Validator(address, pubKey, Money.FromCoins(1m)) });
        }

        var validators = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => ParseEntry(entry, chainId))
            .ToArray();
        return new ValidatorSet(validators);
    }

    private static Validator ParseEntry(string entry, uint chainId)
    {
        var fields = entry.Split(':', StringSplitOptions.TrimEntries);
        if (fields.Length != 3 || !Address.TryParse(fields[0], out var address)
            || address.Version != ChainInfo.AddressVersion(chainId))
        {
            throw new FormatException("VALIDATOR_SET entry must be address:pubkey_hex:stake_coins.");
        }

        var pubKey = Encoders.Hex.DecodeData(fields[1]);
        var expected = Address.FromPublicKey(address.Version, pubKey);
        if (expected.Encoded != address.Encoded)
        {
            throw new FormatException("Validator public key does not match its address.");
        }

        if (!decimal.TryParse(fields[2], NumberStyles.Number, CultureInfo.InvariantCulture, out var stake))
        {
            throw new FormatException("Validator stake is not a valid decimal amount.");
        }

        return new Validator(address, pubKey, Money.FromCoins(stake));
    }
}
