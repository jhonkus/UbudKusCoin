using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using Xunit;
using Key = NBitcoin.Key;
using Money = UbudKusCoin.Core.Types.Money;
using Transaction = UbudKusCoin.Core.Types.Transaction;
using Block = UbudKusCoin.Core.Types.Block;

namespace UbudKusCoin.Tests;

public sealed class StakingStateTransitionTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;

    [Fact]
    public void BondUnbondWithdraw_FollowsDeterministicLockLifecycle()
    {
        var (staker, key) = CreateAddress(0x11);
        var (validator, _) = CreateAddress(0x22);
        var state = new State(ChainId);
        state.EnsureAccount(staker).Balance = Money.FromCoins(10m);
        state.EnsureAccount(validator);

        var bond = MakeTransaction(TransactionKind.Bond, staker, staker, key, 1,
            Money.FromCoins(3m), Money.FromCoins(0.1m));
        state = Apply(state, validator, bond);
        Assert.Equal(Money.FromCoins(6.9m), state.GetAccount(staker)!.Balance);
        Assert.Equal(Money.FromCoins(3m), state.GetStake(staker)!.Amount);

        var unbond = MakeTransaction(TransactionKind.Unbond, staker, staker, key, 2,
            Money.Zero, Money.FromCoins(0.1m), lockPeriod: 2);
        state = Apply(state, validator, unbond);
        Assert.Equal(4, state.GetStake(staker)!.UnlockHeight);

        var earlyWithdraw = MakeTransaction(TransactionKind.Withdraw, staker, staker, key, 3,
            Money.Zero, Money.FromCoins(0.1m));
        var rejected = TryApply(state, validator, earlyWithdraw);
        Assert.False(rejected.Success);
        Assert.Contains("unlocked", rejected.Error);

        state = Apply(state, validator, Array.Empty<Transaction>());
        var withdraw = MakeTransaction(TransactionKind.Withdraw, staker, staker, key, 3,
            Money.Zero, Money.FromCoins(0.1m));
        state = Apply(state, validator, withdraw);

        Assert.Null(state.GetStake(staker));
        Assert.Equal(Money.FromCoins(9.7m), state.GetAccount(staker)!.Balance);
        Assert.Equal(3ul, state.GetAccount(staker)!.Nonce);
    }

    [Fact]
    public void BondWithDifferentPublicKey_IsRejectedForExistingPosition()
    {
        var (staker, key) = CreateAddress(0x31);
        var (otherKeyAddress, otherKey) = CreateAddress(0x32);
        var (validator, _) = CreateAddress(0x33);
        var state = new State(ChainId);
        state.EnsureAccount(staker).Balance = Money.FromCoins(10m);
        state.EnsureAccount(validator);
        state = Apply(state, validator, MakeTransaction(TransactionKind.Bond, staker, staker, key, 1,
            Money.FromCoins(2m), Money.FromCoins(0.1m)));

        var invalid = MakeTransaction(TransactionKind.Bond, staker, staker, otherKey, 2,
            Money.FromCoins(1m), Money.FromCoins(0.1m));
        Assert.False(TryApply(state, validator, invalid).Success);
        Assert.NotEqual(otherKeyAddress, staker);
    }

    [Fact]
    public void RotateValidatorKey_ReplacesConsensusKeyAndRejectsUnauthorizedSigner()
    {
        var (staker, key) = CreateAddress(0x51);
        var (otherAddress, otherKey) = CreateAddress(0x52);
        var (validator, _) = CreateAddress(0x53);
        var state = new State(ChainId);
        state.EnsureAccount(staker).Balance = Money.FromCoins(10m);
        state.EnsureAccount(otherAddress).Balance = Money.FromCoins(10m);
        state.EnsureAccount(validator);
        state = Apply(state, validator, MakeTransaction(TransactionKind.Bond, staker, staker, key, 1,
            Money.FromCoins(2m), Money.FromCoins(0.1m)));

        var rotatedKey = Enumerable.Repeat((byte)0xA5, 32).ToArray();
        state = Apply(state, validator, MakeTransaction(TransactionKind.RotateValidatorKey,
            staker, staker, key, 2, Money.Zero, Money.FromCoins(0.1m), validatorPublicKey: rotatedKey));
        Assert.Equal(rotatedKey, state.GetStake(staker)!.ConsensusPubKey);

        var unauthorized = MakeTransaction(TransactionKind.RotateValidatorKey,
            staker, staker, otherKey, 3, Money.Zero, Money.FromCoins(0.1m),
            validatorPublicKey: Enumerable.Repeat((byte)0xB6, 32).ToArray());
        var rejected = TryApply(state, validator, unauthorized);
        Assert.False(rejected.Success);
        Assert.Contains("envelope or signature", rejected.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateVoteEvidence_SlashesAndJailsValidatorDeterministically()
    {
        var (staker, key) = CreateAddress(0x41);
        var (validator, _) = CreateAddress(0x42);
        var state = new State(ChainId);
        state.EnsureAccount(staker).Balance = Money.FromCoins(10m);
        state.EnsureAccount(validator);
        state = Apply(state, validator, MakeTransaction(TransactionKind.Bond, staker, staker, key, 1,
            Money.FromCoins(3m), Money.FromCoins(0.1m)));

        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = state.Height + 1,
            TimeStamp = 1_700_000_010L,
            PrevHash = state.Head,
            Validator = validator,
            Reward = Money.Zero,
            Evidence = new List<ConsensusEvidence>
            {
                new(ConsensusEvidenceKind.DuplicateVote, staker, 1)
            }
        };
        block.MerkleRoot = Merkle.ComputeRoot(System.Array.Empty<byte[]>());
        var calculated = StateTransition.ComputeResultingState(state, block);
        Assert.True(calculated.Success, calculated.Error);
        block.StateRoot = calculated.NewState!.ComputeStateRoot();

        var applied = StateTransition.ApplyCommittedBlock(state, block);

        Assert.True(applied.Success, applied.Error);
        Assert.True(applied.NewState!.GetStake(staker)!.Jailed);
        Assert.Equal(Money.FromCoins(2m), applied.NewState.GetStake(staker)!.Amount);
    }

    private static State Apply(State state, Address validator, params Transaction[] transactions)
    {
        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = state.Height + 1,
            TimeStamp = 1_700_000_000L + state.Height + 1,
            PrevHash = state.Head,
            Validator = validator,
            Reward = Money.Zero,
            Txs = transactions.ToList()
        };
        block.MerkleRoot = Merkle.ComputeRoot(block.Txs.Select(t => t.ComputeId()).ToArray());
        var calculated = StateTransition.ComputeResultingState(state, block);
        Assert.True(calculated.Success, calculated.Error);
        block.StateRoot = calculated.NewState!.ComputeStateRoot();
        var applied = StateTransition.ApplyCommittedBlock(state, block);
        Assert.True(applied.Success, applied.Error);
        return applied.NewState!;
    }

    private static StateTransitionResult TryApply(State state, Address validator, Transaction transaction)
    {
        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = state.Height + 1,
            TimeStamp = 1_700_000_000L + state.Height + 1,
            PrevHash = state.Head,
            Validator = validator,
            Reward = Money.Zero,
            Txs = new List<Transaction> { transaction }
        };
        block.MerkleRoot = Merkle.ComputeRoot(block.Txs.Select(t => t.ComputeId()).ToArray());
        return StateTransition.ComputeResultingState(state, block);
    }

    private static Transaction MakeTransaction(TransactionKind kind, Address from, Address to,
        Key key, ulong nonce, Money amount, Money fee, long lockPeriod = 0,
        byte[]? validatorPublicKey = null)
    {
        var transaction = new Transaction
        {
            ChainId = ChainId,
            Kind = kind,
            Nonce = nonce,
            From = from,
            To = to,
            Amount = amount,
            Fee = fee,
            LockPeriod = lockPeriod,
            PubKey = key.PubKey.ToBytes()
        };
        if (kind is TransactionKind.Bond or TransactionKind.RotateValidatorKey)
            transaction.ValidatorPubKey = validatorPublicKey
                ?? System.Security.Cryptography.SHA256.HashData(transaction.PubKey);
        transaction.Signature = TransactionSigner.Sign(transaction, key.ToBytes());
        return transaction;
    }

    private static (Address Address, Key Key) CreateAddress(byte firstByte)
    {
        var privateBytes = new byte[32];
        privateBytes[0] = firstByte;
        privateBytes[31] = 1;
        var key = new Key(privateBytes);
        return (Address.FromPublicKey(Address.TestnetVersion, key.PubKey.ToBytes()), key);
    }
}
