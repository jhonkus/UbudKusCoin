using System.Text.Json;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// Signed-network input for deterministic genesis construction. Production
/// deployments should distribute this file out of band and pin its digest.
/// </summary>
public sealed class GenesisManifest
{
    public uint ChainId { get; set; }
    public long GenesisTime { get; set; }
    public long InitialRewardBaseUnits { get; set; }
    public string ValidatorPublicKeyHex { get; set; } = string.Empty;
    public List<GenesisAccount> Accounts { get; set; } = new();

    public GenesisManifest Clone()
        => new()
        {
            ChainId = ChainId,
            GenesisTime = GenesisTime,
            InitialRewardBaseUnits = InitialRewardBaseUnits,
            ValidatorPublicKeyHex = ValidatorPublicKeyHex,
            Accounts = Accounts.Select(x => x with { }).ToList()
        };

    public void Validate(uint? expectedChainId = null)
    {
        if (ChainId == ChainInfo.ChainIdUndefined || (expectedChainId is not null && ChainId != expectedChainId))
            throw new InvalidDataException("Genesis manifest chain_id is invalid or does not match the node.");
        if (GenesisTime <= 0)
            throw new InvalidDataException("Genesis manifest genesis_time must be positive.");
        if (InitialRewardBaseUnits < 0)
            throw new InvalidDataException("Genesis manifest initial reward cannot be negative.");
        if (Accounts.Count == 0)
            throw new InvalidDataException("Genesis manifest must define at least one account.");

        var validatorKey = DecodePublicKey(ValidatorPublicKeyHex, "validator_public_key");
        var addresses = new HashSet<string>(StringComparer.Ordinal);
        foreach (var account in Accounts)
        {
            if (account.BalanceBaseUnits <= 0)
                throw new InvalidDataException("Genesis account balances must be positive.");
            var publicKey = DecodePublicKey(account.PublicKeyHex, "account public key");
            var address = Address.FromPublicKey(ChainInfo.AddressVersion(ChainId), publicKey);
            if (!addresses.Add(address.Encoded))
                throw new InvalidDataException("Genesis manifest contains duplicate accounts.");
        }

        if (!addresses.Contains(Address.FromPublicKey(ChainInfo.AddressVersion(ChainId), validatorKey).Encoded))
            throw new InvalidDataException("Genesis validator must also be a funded genesis account.");
    }

    public static GenesisManifest Load(string path, uint? expectedChainId = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A genesis manifest path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("Genesis manifest was not found.", path);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var manifest = JsonSerializer.Deserialize<GenesisManifest>(File.ReadAllText(path), options)
            ?? throw new InvalidDataException("Genesis manifest is empty.");
        manifest.Validate(expectedChainId);
        return manifest;
    }

    private static byte[] DecodePublicKey(string value, string field)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromHexString(value);
            _ = new NBitcoin.PubKey(bytes);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            throw new InvalidDataException($"Genesis {field} is not a valid secp256k1 public key.", exception);
        }

        return bytes;
    }
}

public sealed record GenesisAccount(string PublicKeyHex, long BalanceBaseUnits);
