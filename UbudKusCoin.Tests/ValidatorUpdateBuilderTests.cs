using System;
using System.Linq;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Grpc;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class ValidatorUpdateBuilderTests
{
    [Fact]
    public void Build_IsDeterministicAndMapsActivePower()
    {
        var first = Stake("02" + new string('1', 64), "a", 3m);
        var second = Stake("03" + new string('2', 64), "b", 7m);
        var state = new State(ChainInfo.ChainIdTestnet);
        state.SetStake(second);
        state.SetStake(first);

        var updates = ValidatorUpdateBuilder.Build(new State(ChainInfo.ChainIdTestnet), state);

        var expected = new[] { first, second }
            .OrderBy(x => x.Address.Encoded, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(2, updates.Count);
        Assert.Equal(expected[0].PubKey, updates[0].PubKey.Secp256K1.ToByteArray());
        Assert.Equal(expected[0].Amount.BaseUnits, updates[0].Power);
        Assert.Equal(expected[1].PubKey, updates[1].PubKey.Secp256K1.ToByteArray());
        Assert.Equal(expected[1].Amount.BaseUnits, updates[1].Power);
    }

    [Fact]
    public void Build_EmitsZeroPowerForJailedUnbondingAndRemovedValidators()
    {
        var removed = Stake("02" + new string('1', 64), "a", 2m);
        var jailed = Stake("03" + new string('2', 64), "b", 4m);
        jailed.Jailed = true;
        var unbonding = Stake("02" + new string('3', 64), "c", 5m);
        unbonding.UnlockHeight = 20;
        var malformed = Stake("02" + new string('4', 62), "d", 6m);
        var previous = new State(ChainInfo.ChainIdTestnet);
        previous.SetStake(removed);
        previous.SetStake(jailed);

        var current = new State(ChainInfo.ChainIdTestnet);
        current.SetStake(jailed);
        current.SetStake(unbonding);
        current.SetStake(malformed);
        var updates = ValidatorUpdateBuilder.Build(previous, current);

        Assert.Equal(3, updates.Count);
        Assert.All(updates, update => Assert.Equal(0, update.Power));
        Assert.Contains(updates, update => update.PubKey.Secp256K1.ToByteArray().SequenceEqual(removed.PubKey));
        Assert.Contains(updates, update => update.PubKey.Secp256K1.ToByteArray().SequenceEqual(unbonding.PubKey));
        Assert.DoesNotContain(updates, update => update.PubKey.Secp256K1.Length == malformed.PubKey.Length);
    }

    private static StakePositionState Stake(string publicKey, string suffix, decimal amount)
    {
        var bytes = Convert.FromHexString(publicKey);
        return new StakePositionState
        {
            Address = Address.FromPublicKey(Address.TestnetVersion, bytes),
            PubKey = bytes,
            Amount = Money.FromCoins(amount),
            BondedHeight = suffix[0],
            UnlockHeight = 0,
            Jailed = false
        };
    }
}
