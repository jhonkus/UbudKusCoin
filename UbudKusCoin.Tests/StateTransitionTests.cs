using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public class StateTransitionTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;

    private static Address MakeAddress(byte firstByte = 0x02)
    {
        var pub = new byte[33];
        RandomNumberGenerator.Fill(pub);
        pub[0] = firstByte;
        return Address.FromPublicKey(Address.TestnetVersion, pub);
    }

private static Transaction MakeTransfer(Address from, Address to, Money amount, Money fee, ulong nonce)
    {
        var pubKey = new byte[33];
        pubKey[0] = 0x02;
        return new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = nonce,
            From = from,
            To = to,
            Amount = amount,
            Fee = fee,
            PubKey = pubKey,
        };
    }

    private static State MakeState(Address validator, Money validatorBalance, params (Address addr, Money bal)[] accounts)
    {
        var state = new State(ChainId);
        foreach (var (addr, bal) in accounts)
        {
            var acc = state.EnsureAccount(addr);
            acc.Balance = bal;
        }

        var v = state.EnsureAccount(validator);
        v.Balance = validatorBalance;
        return state;
    }

    private static Block MakeBlock(State state, Address validator, IReadOnlyList<Transaction> txs, Money reward)
    {
        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = state.Height + 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            MerkleRoot = Merkle.ComputeRoot(txs.Select(t => t.ComputeId()).ToArray()),
            Validator = validator,
            Reward = reward,
            Txs = txs.ToList(),
        };

// Apply to a copy to compute the resulting state, then set StateRoot.
        var resulting = StateTransition.ComputeResultingState(state, block);
        Assert.True(resulting.Success, $"helper block failed: {resulting.Error}");
        block.StateRoot = resulting.NewState!.ComputeStateRoot();
        return block;
    }

    [Fact]
    public void ApplyBlock_ValidTransfer_UpdatesBalancesAndNonces()
    {
        var from = MakeAddress(0x02);
        var to = MakeAddress(0x03);
        var validator = MakeAddress(0x04);

        var state = MakeState(validator, Money.FromCoins(0m),
            (from, Money.FromCoins(10m)),
            (to, Money.FromCoins(1m)));

        var tx = MakeTransfer(from, to, Money.FromCoins(2m), Money.FromCoins(0.1m), nonce: 1);
        var block = MakeBlock(state, validator, new[] { tx }, reward: Money.FromCoins(0.5m));

        var result = StateTransition.ApplyBlock(state, block);

        Assert.True(result.Success);
        Assert.Equal(Money.FromCoins(7.9m), result.NewState!.GetAccount(from)!.Balance); // 10 - 2 - 0.1
        Assert.Equal(Money.FromCoins(3m), result.NewState.GetAccount(to)!.Balance);     // 1 + 2
        Assert.Equal(Money.FromCoins(0.6m), result.NewState.GetAccount(validator)!.Balance); // 0.5 + 0.1 fee
        Assert.Equal(1ul, result.NewState.GetAccount(from)!.Nonce);
        Assert.Equal(block.Height, result.NewState.Height);
    }

    [Fact]
    public void ApplyBlock_IsDeterministic_SameInputSameOutput()
    {
        var from = MakeAddress(0x02);
        var to = MakeAddress(0x03);
        var validator = MakeAddress(0x04);

        var state1 = MakeState(validator, Money.Zero, (from, Money.FromCoins(5m)));
        var state2 = MakeState(validator, Money.Zero, (from, Money.FromCoins(5m)));

        var tx1 = MakeTransfer(from, to, Money.FromCoins(1m), Money.FromCoins(0.1m), 1);
        var tx2 = MakeTransfer(from, to, Money.FromCoins(1m), Money.FromCoins(0.1m), 1);
        var block1 = MakeBlock(state1, validator, new[] { tx1 }, Money.Zero);
        var block2 = MakeBlock(state2, validator, new[] { tx2 }, Money.Zero);

        var r1 = StateTransition.ApplyBlock(state1, block1);
        var r2 = StateTransition.ApplyBlock(state2, block2);

        Assert.True(r1.Success && r2.Success);
        Assert.Equal(r1.NewState!.ComputeStateRoot(), r2.NewState!.ComputeStateRoot());
        Assert.Equal(r1.NewState.Head, r2.NewState.Head);
    }

    [Fact]
    public void ApplyBlock_RejectsDoubleSpend_NonceReuse()
    {
        var from = MakeAddress(0x02);
        var toA = MakeAddress(0x03);
        var toB = MakeAddress(0x05);
        var validator = MakeAddress(0x04);

        var state = MakeState(validator, Money.Zero, (from, Money.FromCoins(10m)));

        // Same nonce twice => second transfer must be rejected.
        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = validator,
            Reward = Money.Zero,
            Txs = new List<Transaction>
            {
                MakeTransfer(from, toA, Money.FromCoins(1m), Money.Zero, nonce: 1),
                MakeTransfer(from, toB, Money.FromCoins(1m), Money.Zero, nonce: 1),
            },
        };
        block.MerkleRoot = Merkle.ComputeRoot(block.Txs.Select(t => t.ComputeId()).ToArray());
        block.StateRoot = new State(ChainId).ComputeStateRoot(); // will mismatch anyway

        var result = StateTransition.ApplyBlock(state, block);
        Assert.False(result.Success);
        Assert.Contains("nonce", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyBlock_RejectsInsufficientBalance()
    {
        var from = MakeAddress(0x02);
        var to = MakeAddress(0x03);
        var validator = MakeAddress(0x04);

        var state = MakeState(validator, Money.Zero, (from, Money.FromCoins(1m)));

        var tx = MakeTransfer(from, to, Money.FromCoins(5m), Money.Zero, nonce: 1);
        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = validator,
            Reward = Money.Zero,
            Txs = new List<Transaction> { tx },
        };
        block.MerkleRoot = Merkle.ComputeRoot(block.Txs.Select(t => t.ComputeId()).ToArray());
        block.StateRoot = Merkle.ZeroRoot;

        var result = StateTransition.ApplyBlock(state, block);
        Assert.False(result.Success);
        Assert.Contains("balance", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyBlock_RejectsWrongChainId()
    {
        var from = MakeAddress(0x02);
        var validator = MakeAddress(0x04);
        var state = MakeState(validator, Money.Zero, (from, Money.FromCoins(5m)));

        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainInfo.ChainIdMainnet, // mismatch
            Height = 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = validator,
            Reward = Money.Zero,
        };
        block.MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>());
        block.StateRoot = state.ComputeStateRoot();

        var result = StateTransition.ApplyBlock(state, block);
        Assert.False(result.Success);
        Assert.Contains("ChainId", result.Error);
    }

    [Fact]
    public void ApplyBlock_RejectsWrongHeight()
    {
        var validator = MakeAddress(0x04);
        var state = MakeState(validator, Money.Zero);

        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = 5, // wrong height
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = validator,
            Reward = Money.Zero,
        };
        block.MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>());
        block.StateRoot = state.ComputeStateRoot();

        var result = StateTransition.ApplyBlock(state, block);
        Assert.False(result.Success);
        Assert.Contains("height", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyBlock_RejectsBadPrevHash()
    {
        var validator = MakeAddress(0x04);
        var state = MakeState(validator, Money.Zero);

var badPrevHash = new byte[32];
        badPrevHash[0] = 0xAB;
        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = badPrevHash, // wrong prev hash
            Validator = validator,
            Reward = Money.Zero,
        };
        block.MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>());
        block.StateRoot = state.ComputeStateRoot();

        var result = StateTransition.ApplyBlock(state, block);
        Assert.False(result.Success);
        Assert.Contains("PrevHash", result.Error);
    }

    [Fact]
    public void ApplyBlock_InputStateIsNotMutated()
    {
        var from = MakeAddress(0x02);
        var validator = MakeAddress(0x04);
        var state = MakeState(validator, Money.Zero, (from, Money.FromCoins(5m)));
        var beforeRoot = state.ComputeStateRoot();

        var tx = MakeTransfer(from, MakeAddress(0x03), Money.FromCoins(1m), Money.Zero, 1);
        var block = MakeBlock(state, validator, new[] { tx }, Money.Zero);

        var result = StateTransition.ApplyBlock(state, block);
        Assert.True(result.Success);

        // Original state must be unchanged.
        Assert.Equal(beforeRoot, state.ComputeStateRoot());
        Assert.Equal(0L, state.Height);
        Assert.Equal(Money.FromCoins(5m), state.GetAccount(from)!.Balance);
    }

    [Fact]
    public void ApplyBlock_StateRootMismatch_IsRejected()
    {
        var from = MakeAddress(0x02);
        var to = MakeAddress(0x03);
        var validator = MakeAddress(0x04);
        var state = MakeState(validator, Money.Zero, (from, Money.FromCoins(5m)));

        var tx = MakeTransfer(from, to, Money.FromCoins(1m), Money.Zero, 1);
        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            Validator = validator,
            Reward = Money.Zero,
            Txs = new List<Transaction> { tx },
        };
        block.MerkleRoot = Merkle.ComputeRoot(block.Txs.Select(t => t.ComputeId()).ToArray());
        block.StateRoot = Merkle.ZeroRoot; // deliberately wrong

        var result = StateTransition.ApplyBlock(state, block);
        Assert.False(result.Success);
        Assert.Contains("State root", result.Error);
    }
}
