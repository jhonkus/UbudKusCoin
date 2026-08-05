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
}
