using System;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public class MoneyTests
{
    [Fact]
    public void Money_FromCoins_ConvertsToBaseUnits()
    {
        var m = Money.FromCoins(1.5m);
        Assert.Equal(150_000_000L, m.BaseUnits);
        Assert.Equal(1.5m, m.Coins);
    }

    [Fact]
    public void Money_Addition_IsExact()
    {
        var a = Money.FromCoins(0.1m);   // 10,000,000
        var b = Money.FromCoins(0.2m);   // 20,000,000
        var sum = a + b;
        Assert.Equal(30_000_000L, sum.BaseUnits);
        Assert.Equal(0.3m, sum.Coins);
    }

    [Fact]
    public void Money_Subtraction_IsExact()
    {
        var a = Money.FromCoins(1.0m);
        var b = Money.FromCoins(0.7m);
        var diff = a - b;
        Assert.Equal(30_000_000L, diff.BaseUnits);
        Assert.Equal(0.3m, diff.Coins);
    }

    [Fact]
    public void Money_RejectsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.FromCoins(-0.1m));
    }

    [Fact]
    public void Money_Subtraction_RejectsNegativeResult()
    {
        var a = Money.FromCoins(0.1m);
        var b = Money.FromCoins(0.2m);
        Assert.Throws<InvalidOperationException>(() => a - b);
    }

    [Theory]
    [InlineData(0.1d, 0.2d, 0.3d)]
    public void Money_NoFloatingPointPrecisionLoss(decimal aC, decimal bC, decimal expected)
    {
        // Using decimal constant inputs; verify no binary floating error.
        var a = Money.FromCoins(aC);
        var b = Money.FromCoins(bC);
        Assert.Equal((long)(expected * Money.BaseUnitsPerCoin), (a + b).BaseUnits);
    }
}
