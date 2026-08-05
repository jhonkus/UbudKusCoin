namespace UbudKusCoin.Core.Types;

/// <summary>
/// Protocol-level constants shared across the network. Chain IDs separate
/// environments so transactions/blocks are never replayed across networks.
/// </summary>
public static class ChainInfo
{
    /// <summary>Current transaction format version.</summary>
    public const uint TxVersion = 1;

    /// <summary>Mainnet chain identifier.</summary>
    public const uint ChainIdMainnet = 1;

    /// <summary>Testnet chain identifier.</summary>
    public const uint ChainIdTestnet = 2;

/// <summary>Reserved/undefined chain identifier (must not be used in signed txs).</summary>
    public const uint ChainIdUndefined = 0;

    /// <summary>
    /// Returns the address version byte for a given chain id so that testnet and
    /// mainnet addresses are mutually incompatible (replay protection at the
    /// address layer).
    /// </summary>
    public static byte AddressVersion(uint chainId)
        => chainId == ChainIdMainnet ? Address.MainnetVersion : Address.TestnetVersion;
}
