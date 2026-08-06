namespace UbudKusCoin.Core.Types;

/// <summary>Consensus-owned staking position persisted in the application state.</summary>
public sealed class StakePositionState
{
    public required Address Address { get; init; }
    public required byte[] PubKey { get; init; }
    public byte[] ConsensusPubKey { get; set; } = Array.Empty<byte>();
    public Money Amount { get; set; }
    public long BondedHeight { get; set; }
    public long UnlockHeight { get; set; }
    public bool Jailed { get; set; }

    public StakePositionState Clone() => new()
    {
        Address = Address,
        PubKey = PubKey.ToArray(),
        ConsensusPubKey = ConsensusPubKey.ToArray(),
        Amount = Amount,
        BondedHeight = BondedHeight,
        UnlockHeight = UnlockHeight,
        Jailed = Jailed
    };
}
