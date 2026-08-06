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
        Key key, ulong nonce, Money amount, Money fee, long lockPeriod = 0)
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
