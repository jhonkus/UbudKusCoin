namespace UbudKusCoin.Core.Types;

/// <summary>
/// Fixed-point monetary value. All amounts in the protocol are expressed in
/// integer base units (1 UKC = 100,000,000 base units). Floating point is
/// never used for money in consensus-critical code.
/// </summary>
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public const long BaseUnitsPerCoin = 100_000_000L;

    /// <summary>The value in base units. Always non-negative.</summary>
    public long BaseUnits { get; }

    public Money(long baseUnits)
    {
        if (baseUnits < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseUnits), "Money cannot be negative.");
        }

        BaseUnits = baseUnits;
    }

    public static Money Zero => new(0);

    /// <summary>Creates a Money from a whole+decimal coin amount (e.g. 1.5 UKC).</summary>
    public static Money FromCoins(decimal coins)
    {
        if (coins < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(coins), "Money cannot be negative.");
        }

        return new Money((long)(coins * BaseUnitsPerCoin));
    }

    public decimal Coins => (decimal)BaseUnits / BaseUnitsPerCoin;

    public static Money operator +(Money a, Money b) => new(checked(a.BaseUnits + b.BaseUnits));

    public static Money operator -(Money a, Money b)
    {
        if (a.BaseUnits < b.BaseUnits)
        {
            throw new InvalidOperationException("Subtraction would result in negative money.");
        }

        return new Money(checked(a.BaseUnits - b.BaseUnits));
    }

    public static bool operator <(Money a, Money b) => a.BaseUnits < b.BaseUnits;
    public static bool operator >(Money a, Money b) => a.BaseUnits > b.BaseUnits;
    public static bool operator <=(Money a, Money b) => a.BaseUnits <= b.BaseUnits;
    public static bool operator >=(Money a, Money b) => a.BaseUnits >= b.BaseUnits;
    public static bool operator ==(Money a, Money b) => a.BaseUnits == b.BaseUnits;
    public static bool operator !=(Money a, Money b) => a.BaseUnits != b.BaseUnits;

    public bool Equals(Money other) => BaseUnits == other.BaseUnits;
    public override bool Equals(object? obj) => obj is Money m && Equals(m);
    public override int GetHashCode() => BaseUnits.GetHashCode();
    public int CompareTo(Money other) => BaseUnits.CompareTo(other.BaseUnits);
    public override string ToString() => $"{Coins} UKC";
}
