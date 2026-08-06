using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Types;
using CoreMoney = UbudKusCoin.Core.Types.Money;
using CoreBlock = UbudKusCoin.Core.Types.Block;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class ConsensusDriverTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;

    [Fact]
    public void ProposerSelection_IsDeterministicAndStakeWeighted()
    {
        var validators = MakeValidators();
        var first = new ValidatorSet(validators.Select(x => x.Validator));
        var second = new ValidatorSet(validators.AsEnumerable().Reverse().Select(x => x.Validator));

        Assert.Equal(first.SelectProposer(ChainId, 10, 2).Address, second.SelectProposer(ChainId, 10, 2).Address);
    }

    [Fact]
    public void QuorumCertificate_RequiresTwoThirdsStake()
    {
        var validators = MakeValidators();
        var set = new ValidatorSet(validators.Select(x => x.Validator));
        var state = new State(ChainId);
        var driver = new DeterministicBftDriver(state, set);
        var blockHash = "block-hash";

        var first = SignVote(validators[0], 1, 0, blockHash);
        var second = SignVote(validators[1], 1, 0, blockHash);
        Assert.True(driver.AddVote(first, out var noCertificate, out var firstError), firstError);
        Assert.Null(noCertificate);
        Assert.True(driver.AddVote(second, out var certificate, out var secondError), secondError);
        Assert.NotNull(certificate);
        Assert.True(certificate!.Verify(set));
    }

    [Fact]
    public void Equivocation_IsRejectedAndRecorded()
    {
        var validators = MakeValidators();
        var driver = new DeterministicBftDriver(new State(ChainId), new ValidatorSet(validators.Select(x => x.Validator)));
        var first = SignVote(validators[0], 1, 0, "block-a");
        var conflicting = SignVote(validators[0], 1, 0, "block-b");

        Assert.True(driver.AddVote(first, out _, out var firstError), firstError);
        Assert.False(driver.AddVote(conflicting, out _, out var error));
        Assert.Contains("equivocation", error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, driver.EquivocationEvidence.Count);
    }

    [Fact]
    public void ProposalValidation_RequiresSelectedProposer()
    {
        var validators = MakeValidators();
        var set = new ValidatorSet(validators.Select(x => x.Validator));
        var state = new State(ChainId);
        var driver = new DeterministicBftDriver(state, set);
        var proposer = driver.Proposer(1, 0);
        var block = new CoreBlock
        {
            ChainId = ChainId,
            Height = 1,
            TimeStamp = 1,
            PrevHash = state.Head,
            MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>()),
            Validator = proposer.Address,
            Reward = CoreMoney.Zero
        };
        var resulting = StateTransition.ComputeResultingState(state, block);
        block.StateRoot = resulting.NewState!.ComputeStateRoot();

        Assert.True(driver.ValidateProposal(block, 0, out var error), error);
    }

    [Fact]
    public void RoundChange_AllowsNextSelectedProposerToMakeProgress()
    {
        var validators = MakeValidators();
        var set = new ValidatorSet(validators.Select(x => x.Validator));
        var state = new State(ChainId);
        var driver = new DeterministicBftDriver(state, set);
        var initial = driver.Proposer(1, 0);
        var round = 1u;
        while (set.SelectProposer(ChainId, 1, round).Address.Encoded == initial.Address.Encoded && round < 100)
        {
            round++;
        }

        var next = driver.Proposer(1, round);
        var nextKey = validators.Single(x => x.Address.Equals(next.Address)).Key;
        var block = BuildEmptyBlock(state, next, nextKey);

        Assert.NotEqual(initial.Address.Encoded, next.Address.Encoded);
        Assert.True(driver.ValidateProposal(block, round, out var error), error);
    }

    [Fact]
    public void Commit_RequiresQuorumAndAdvancesFinality()
    {
        var validators = MakeValidators();
        var set = new ValidatorSet(validators.Select(x => x.Validator));
        var state = new State(ChainId);
        var driver = new DeterministicBftDriver(state, set);
        var proposer = driver.Proposer(1, 0);
        var proposerKey = validators.Single(x => x.Address.Equals(proposer.Address)).Key;
        var block = BuildEmptyBlock(state, proposer, proposerKey);
        var first = SignVote(validators[0], block.Height, 0, block.ComputeHeaderHashHex());
        var second = SignVote(validators[1], block.Height, 0, block.ComputeHeaderHashHex());

        Assert.True(driver.AddVote(first, out _, out var firstError), firstError);
        Assert.True(driver.AddVote(second, out var certificate, out var secondError), secondError);
        Assert.NotNull(certificate);
        Assert.True(driver.Commit(block, certificate!, out var commitError), commitError);
        Assert.Equal(1L, driver.Finality.FinalizedHeight);
        Assert.Equal(block.ComputeHeaderHashHex(), driver.Finality.FinalizedHash);
    }

    [Fact]
    public void DelayedCertificate_CannotRewindFinality()
    {
        var validators = MakeValidators();
        var set = new ValidatorSet(validators.Select(x => x.Validator));
        var state = new State(ChainId);
        var driver = new DeterministicBftDriver(state, set);
        var proposer = driver.Proposer(1, 0);
        var proposerKey = validators.Single(x => x.Address.Equals(proposer.Address)).Key;
        var block = BuildEmptyBlock(state, proposer, proposerKey);
        var first = SignVote(validators[0], block.Height, 0, block.ComputeHeaderHashHex());
        var second = SignVote(validators[1], block.Height, 0, block.ComputeHeaderHashHex());

        Assert.True(driver.AddVote(first, out _, out var firstError), firstError);
        Assert.True(driver.AddVote(second, out var certificate, out var secondError), secondError);
        Assert.NotNull(certificate);
        Assert.True(driver.Commit(block, certificate!, out var commitError), commitError);
        var finalizedHash = driver.Finality.FinalizedHash;

        Assert.True(driver.AddVote(first, out _, out firstError), firstError);
        Assert.True(driver.AddVote(second, out var delayedCertificate, out secondError), secondError);
        Assert.NotNull(delayedCertificate);
        Assert.False(driver.Commit(block, delayedCertificate!, out var delayedError));
        Assert.Contains("sequential", delayedError, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(finalizedHash, driver.Finality.FinalizedHash);
        Assert.Equal(1, driver.Finality.FinalizedHeight);
    }

    [Fact]
    public void StakingLedger_EnforcesLockAndSlashesEquivocators()
    {
        var validator = MakeValidators()[0];
        var ledger = new StakingLedger();

        Assert.True(ledger.Bond(validator.Address, validator.Key.PubKey.ToBytes(), CoreMoney.FromCoins(10m), 1, out var bondError), bondError);
        Assert.True(ledger.RequestUnbond(validator.Address, 2, 5, out var unbondError), unbondError);
        Assert.False(ledger.Withdraw(validator.Address, 6, out _, out _));
        Assert.True(ledger.Withdraw(validator.Address, 7, out var withdrawn, out var withdrawError), withdrawError);
        Assert.Equal(CoreMoney.FromCoins(10m), withdrawn);

        Assert.True(ledger.Bond(validator.Address, validator.Key.PubKey.ToBytes(), CoreMoney.FromCoins(10m), 8, out bondError), bondError);
        Assert.True(ledger.Slash(validator.Address, CoreMoney.FromCoins(3m), out var slashError), slashError);
        Assert.Throws<ArgumentException>(() => ledger.CreateValidatorSet());
    }

    private static List<ValidatorWithKey> MakeValidators()
    {
        return new[] { 1, 2, 3 }.Select((seed, index) =>
        {
            var bytes = new byte[32];
            bytes[0] = (byte)seed;
            bytes[31] = 1;
            var key = new Key(bytes);
            var address = Address.FromPublicKey(ChainInfo.AddressVersion(ChainId), key.PubKey.ToBytes());
            return new ValidatorWithKey(
                new Validator(address, key.PubKey.ToBytes(), CoreMoney.FromCoins(index == 0 ? 4m : 3m)), key);
        }).ToList();
    }

    private static ConsensusVote SignVote(ValidatorWithKey validator, long height, uint round, string blockHash)
    {
        var vote = new ConsensusVote
        {
            ChainId = ChainId,
            Height = height,
            Round = round,
            BlockHash = blockHash,
            Validator = validator.Address,
            PubKey = validator.Key.PubKey.ToBytes()
        };
        vote.Signature = validator.Key.Sign(new uint256(vote.ComputeDigest())).ToDER();
        return vote;
    }

    private static CoreBlock BuildEmptyBlock(State state, Validator validator, Key key)
    {
        var block = new CoreBlock
        {
            ChainId = ChainId,
            Height = state.Height + 1,
            TimeStamp = state.TimeStamp + 1,
            PrevHash = state.Head,
            MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>()),
            Validator = validator.Address,
            Reward = CoreMoney.Zero
        };
        var resulting = StateTransition.ComputeResultingState(state, block);
        block.StateRoot = resulting.NewState!.ComputeStateRoot();
        if (block.Height > 1)
        {
            block.ValidatorPubKey = key.PubKey.ToBytes();
            block.ValidatorSignature = key.Sign(new uint256(block.ComputeHeaderHash())).ToDER();
        }

        return block;
    }

    private sealed record ValidatorWithKey(Validator Validator, Key Key)
    {
        public Address Address => Validator.Address;
    }
}
