using System;
using System.Collections.Generic;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using Xunit;
using BtcKey = NBitcoin.Key;

namespace UbudKusCoin.Tests;

public class AbciMisbehaviorSlashingTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;
    private static readonly byte Version = ChainInfo.AddressVersion(ChainId);

    private static BtcKey MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new BtcKey(bytes);
    }

    private static Address MakeAddress(BtcKey key)
        => Address.FromPublicKey(Version, key.PubKey.ToBytes());

    [Fact]
    public void DuplicateVote_Misbehavior_SlashesStakeAndJailsValidator()
    {
        var validatorKey = MakeKey(0x01);
        var validatorAddr = MakeAddress(validatorKey);

        var state = new State(ChainId);
        var acc = state.EnsureAccount(validatorAddr);
        acc.Balance = Money.FromCoins(1000m);

        // Bond 300 UKC stake
        var stakeAmount = Money.FromCoins(300m);
        var stakePosition = new StakePositionState
        {
            Address = validatorAddr,
            PubKey = validatorKey.PubKey.ToBytes(),
            Amount = stakeAmount,
            ConsensusPubKey = new byte[32],
            Jailed = false
        };
        state.SetStake(stakePosition);

        var block = new Block
        {
            ChainId = ChainId,
            Height = 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = validatorAddr,
            Reward = Money.Zero,
            Txs = new List<Transaction>(),
            Evidence = new List<ConsensusEvidence>
            {
                new ConsensusEvidence(ConsensusEvidenceKind.DuplicateVote, validatorAddr, 1)
            }
        };

        var result = StateTransition.ComputeResultingState(state, block);
        Assert.True(result.Success, result.Error);

        var nextState = result.NewState!;
        var slashedStake = nextState.GetStake(validatorAddr);

        Assert.NotNull(slashedStake);
        Assert.True(slashedStake.Jailed);
        // Slashed by 1/3 (300 - 100 = 200 UKC remaining)
        Assert.Equal(Money.FromCoins(200m).BaseUnits, slashedStake.Amount.BaseUnits);
    }

    [Fact]
    public void LightClientAttack_Misbehavior_SlashesStakeAndJailsValidator()
    {
        var validatorKey = MakeKey(0x02);
        var validatorAddr = MakeAddress(validatorKey);

        var state = new State(ChainId);
        state.EnsureAccount(validatorAddr).Balance = Money.FromCoins(1000m);

        var stakePosition = new StakePositionState
        {
            Address = validatorAddr,
            PubKey = validatorKey.PubKey.ToBytes(),
            Amount = Money.FromCoins(600m),
            Jailed = false
        };
        state.SetStake(stakePosition);

        var block = new Block
        {
            ChainId = ChainId,
            Height = 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = validatorAddr,
            Reward = Money.Zero,
            Txs = new List<Transaction>(),
            Evidence = new List<ConsensusEvidence>
            {
                new ConsensusEvidence(ConsensusEvidenceKind.LightClientAttack, validatorAddr, 1)
            }
        };

        var result = StateTransition.ComputeResultingState(state, block);
        Assert.True(result.Success, result.Error);

        var nextState = result.NewState!;
        var slashedStake = nextState.GetStake(validatorAddr);

        Assert.NotNull(slashedStake);
        Assert.True(slashedStake.Jailed);
        // Slashed by 1/3 (600 - 200 = 400 UKC remaining)
        Assert.Equal(Money.FromCoins(400m).BaseUnits, slashedStake.Amount.BaseUnits);
    }

    [Fact]
    public void InvalidEvidenceHeight_IsRejectedByStateTransition()
    {
        var validatorKey = MakeKey(0x03);
        var validatorAddr = MakeAddress(validatorKey);

        var state = new State(ChainId);
        var block = new Block
        {
            ChainId = ChainId,
            Height = 2,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = validatorAddr,
            Reward = Money.Zero,
            Txs = new List<Transaction>(),
            Evidence = new List<ConsensusEvidence>
            {
                // Evidence height (5) is greater than block height (2)
                new ConsensusEvidence(ConsensusEvidenceKind.DuplicateVote, validatorAddr, 5)
            }
        };

        var result = StateTransition.ComputeResultingState(state, block);
        Assert.False(result.Success);
        Assert.Contains("invalid", result.Error, StringComparison.OrdinalIgnoreCase);
    }
}
