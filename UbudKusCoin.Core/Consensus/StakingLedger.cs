using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Core.Consensus;

public sealed class StakingLedger
{
    private readonly Dictionary<string, StakePosition> positions = new(StringComparer.Ordinal);

    public IReadOnlyCollection<StakePosition> Positions => positions.Values;

    public bool Bond(Address address, byte[] pubKey, Money amount, long currentHeight, out string error)
    {
        if (amount <= Money.Zero)
        {
            error = "Stake amount must be positive.";
            return false;
        }

        if (!positions.TryGetValue(address.Encoded, out var position))
        {
            positions[address.Encoded] = new StakePosition(address, pubKey, amount, currentHeight, 0, false);
        }
        else
        {
            if (position.Jailed || position.UnlockHeight > currentHeight)
            {
                error = "Stake position is locked or jailed.";
                return false;
            }

            position.Amount += amount;
        }

        error = string.Empty;
        return true;
    }

    public bool RequestUnbond(Address address, long currentHeight, long lockPeriod, out string error)
    {
        if (!positions.TryGetValue(address.Encoded, out var position) || position.Amount <= Money.Zero)
        {
            error = "No active stake found.";
            return false;
        }

        if (lockPeriod <= 0)
        {
            error = "Lock period must be positive.";
            return false;
        }

        position.UnlockHeight = checked(currentHeight + lockPeriod);
        error = string.Empty;
        return true;
    }

    public bool Withdraw(Address address, long currentHeight, out Money amount, out string error)
    {
        amount = Money.Zero;
        if (!positions.TryGetValue(address.Encoded, out var position))
        {
            error = "No stake position found.";
            return false;
        }

        if (position.UnlockHeight == 0 || currentHeight < position.UnlockHeight)
        {
            error = "Stake is still locked.";
            return false;
        }

        amount = position.Amount;
        positions.Remove(address.Encoded);
        error = string.Empty;
        return true;
    }

    public bool Slash(Address address, Money amount, out string error)
    {
        if (!positions.TryGetValue(address.Encoded, out var position) || amount <= Money.Zero)
        {
            error = "No active stake found.";
            return false;
        }

        if (amount > position.Amount)
        {
            error = "Slash amount exceeds stake.";
            return false;
        }

        position.Amount -= amount;
        position.Jailed = true;
        position.UnlockHeight = 0;
        error = string.Empty;
        return true;
    }

    public ValidatorSet CreateValidatorSet()
    {
        return new ValidatorSet(positions.Values
            .Where(x => !x.Jailed && x.Amount > Money.Zero)
            .Select(x => new Validator(x.Address, x.PubKey, x.Amount)));
    }
}

public sealed class StakePosition
{
    public StakePosition(Address address, byte[] pubKey, Money amount, long bondedHeight, long unlockHeight, bool jailed)
    {
        Address = address;
        PubKey = pubKey;
        Amount = amount;
        BondedHeight = bondedHeight;
        UnlockHeight = unlockHeight;
        Jailed = jailed;
    }

    public Address Address { get; }
    public byte[] PubKey { get; }
    public Money Amount { get; set; }
    public long BondedHeight { get; }
    public long UnlockHeight { get; set; }
    public bool Jailed { get; set; }
}
