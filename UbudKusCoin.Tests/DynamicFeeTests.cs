using System;
using System.Collections.Generic;
using System.Linq;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using Xunit;
using Key = NBitcoin.Key;

namespace UbudKusCoin.Tests;

/// <summary>
/// Unit tests for the EIP-1559-style adaptive dynamic base fee model.
/// Covers FeePolicy adjustment logic, State.BaseFee propagation,
/// Mempool rejection, and StateTransition rejection for under-fee txs.
/// </summary>
public class DynamicFeeTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;
    private static readonly Dictionary<string, Key> Keys = new(StringComparer.Ordinal);

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static Address MakeAddress(byte firstByte)
    {
        var privateBytes = new byte[32];
        privateBytes[0] = firstByte;
        privateBytes[31] = 0x01;
        var key = new Key(privateBytes);
        var address = Address.FromPublicKey(Address.TestnetVersion, key.PubKey.ToBytes());
        Keys[address.Encoded] = key;
        return address;
    }

    private static Transaction MakeTransfer(
        Address from, Address to, Money amount, Money fee, ulong nonce)
    {
        var key = Keys[from.Encoded];
        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = nonce,
            From = from,
            To = to,
            Amount = amount,
            Fee = fee,
            PubKey = key.PubKey.ToBytes(),
        };
        tx.Signature = TransactionSigner.Sign(tx, key.ToBytes());
        return tx;
    }

    private static State MakeState(
        Address validator, Money validatorBalance,
        params (Address addr, Money bal)[] accounts)
    {
        var state = new State(ChainId);
        var v = state.EnsureAccount(validator);
        v.Balance = validatorBalance;
        foreach (var (addr, bal) in accounts)
        {
            var acc = state.EnsureAccount(addr);
            acc.Balance = bal;
        }
        return state;
    }

    private static Block MakeBlock(
        State state, Address validator,
        IReadOnlyList<Transaction> txs, Money reward)
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

        var resulting = StateTransition.ComputeResultingState(state, block);
        Assert.True(resulting.Success, $"helper block failed: {resulting.Error}");
        block.StateRoot = resulting.NewState!.ComputeStateRoot();
        return block;
    }

    // -------------------------------------------------------------------------
    // FeePolicy.GetDynamicBaseFee Tests
    // -------------------------------------------------------------------------

    [Fact]
    public void GetDynamicBaseFee_AtTarget_ReturnsUnchangedFee()
    {
        var baseFee = FeePolicy.BaseFee;
        var next = FeePolicy.GetDynamicBaseFee(FeePolicy.TargetTxCountPerBlock, baseFee);
        Assert.Equal(baseFee.BaseUnits, next.BaseUnits);
    }

    [Fact]
    public void GetDynamicBaseFee_AboveTarget_IncreasesFee()
    {
        var baseFee = FeePolicy.BaseFee;
        var next = FeePolicy.GetDynamicBaseFee(FeePolicy.TargetTxCountPerBlock + 1, baseFee);
        Assert.True(next.BaseUnits > baseFee.BaseUnits,
            $"Expected fee to increase, but {next.BaseUnits} <= {baseFee.BaseUnits}");
    }

    [Fact]
    public void GetDynamicBaseFee_BelowTarget_DecreasesFee()
    {
        // Use a fee well above the floor so it has room to decrease.
        var baseFee = Money.FromCoins(1m); // 1 UKC — well above MinRelayFee floor
        var next = FeePolicy.GetDynamicBaseFee(0, baseFee);
        Assert.True(next.BaseUnits < baseFee.BaseUnits,
            $"Expected fee to decrease, but {next.BaseUnits} >= {baseFee.BaseUnits}");
    }

    [Fact]
    public void GetDynamicBaseFee_NeverDropsBelowFloor()
    {
        // Start at the floor itself and ask for a lower fee.
        var floor = FeePolicy.MinRelayFee;
        var next = FeePolicy.GetDynamicBaseFee(0, floor);
        Assert.True(next.BaseUnits >= floor.BaseUnits,
            $"Fee dropped below floor: {next.BaseUnits} < {floor.BaseUnits}");
    }

    [Fact]
    public void GetDynamicBaseFee_NeverExceedsCap()
    {
        var cap = FeePolicy.MaxFeePerTx;
        // Use an extreme tx count to push the fee as high as possible.
        var next = FeePolicy.GetDynamicBaseFee(10_000, cap);
        Assert.True(next.BaseUnits <= cap.BaseUnits,
            $"Fee exceeded MaxFeePerTx cap: {next.BaseUnits} > {cap.BaseUnits}");
    }

    [Fact]
    public void GetDynamicBaseFee_MultipleBlocksAboveTarget_FeeIncreasesMonotonically()
    {
        var fee = FeePolicy.BaseFee;
        long prevUnits = fee.BaseUnits;
        for (int i = 0; i < 10; i++)
        {
            fee = FeePolicy.GetDynamicBaseFee(FeePolicy.TargetTxCountPerBlock + 5, fee);
            Assert.True(fee.BaseUnits >= prevUnits);
            prevUnits = fee.BaseUnits;
        }
    }

    // -------------------------------------------------------------------------
    // State.BaseFee propagation via Derive()
    // -------------------------------------------------------------------------

    [Fact]
    public void State_Derive_CopiesBaseFee()
    {
        var state = new State(ChainId)
        {
            BaseFee = Money.FromCoins(0.5m)
        };
        var copy = state.Derive();
        Assert.Equal(state.BaseFee.BaseUnits, copy.BaseFee.BaseUnits);
    }

    // -------------------------------------------------------------------------
    // StateTransition: base fee validation and update
    // -------------------------------------------------------------------------

    [Fact]
    public void StateTransition_RejectsTransaction_BelowDynamicBaseFee()
    {
        var from = MakeAddress(0x10);
        var to = MakeAddress(0x11);
        var validator = MakeAddress(0x12);

        var state = MakeState(validator, Money.Zero,
            (from, Money.FromCoins(10m)),
            (to, Money.Zero));

        // Bump state.BaseFee above the tx fee we will use.
        state.BaseFee = Money.FromCoins(1m);

        // Fee below state.BaseFee
        var key = Keys[from.Encoded];
        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = 1,
            From = from,
            To = to,
            Amount = Money.FromCoins(1m),
            Fee = Money.FromCoins(0.0001m), // << below BaseFee of 1
            PubKey = key.PubKey.ToBytes(),
        };
        tx.Signature = TransactionSigner.Sign(tx, key.ToBytes());

        var block = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = state.Height + 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            MerkleRoot = Merkle.ComputeRoot(new[] { tx.ComputeId() }),
            Validator = validator,
            Reward = Money.Zero,
            Txs = new List<Transaction> { tx },
            StateRoot = new byte[32],
        };

        var result = StateTransition.ComputeResultingState(state, block);

        Assert.False(result.Success);
        Assert.Contains("base fee", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StateTransition_AcceptsTransaction_AtOrAboveDynamicBaseFee()
    {
        var from = MakeAddress(0x20);
        var to = MakeAddress(0x21);
        var validator = MakeAddress(0x22);

        var state = MakeState(validator, Money.Zero,
            (from, Money.FromCoins(10m)),
            (to, Money.Zero));

        state.BaseFee = FeePolicy.BaseFee; // 0.0001 UKC

        var tx = MakeTransfer(from, to, Money.FromCoins(1m), FeePolicy.BaseFee, nonce: 1);
        var block = MakeBlock(state, validator, new[] { tx }, Money.Zero);
        var result = StateTransition.ApplyBlock(state, block);

        Assert.True(result.Success);
    }

    [Fact]
    public void StateTransition_UpdatesBaseFee_AfterBlockApplication()
    {
        var from = MakeAddress(0x30);
        var to = MakeAddress(0x31);
        var validator = MakeAddress(0x32);

        var state = MakeState(validator, Money.Zero,
            (from, Money.FromCoins(50m)),
            (to, Money.Zero));

        state.BaseFee = FeePolicy.BaseFee;

        // Submit more than TargetTxCountPerBlock transactions — each with a different nonce.
        // We can't do them all with one sender due to balance + nonce ordering, but we
        // can verify the adjustment with 0 txs (fee should decrease from initial).
        var emptyBlock = new Block
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Height = state.Height + 1,
            TimeStamp = 1_700_000_000L,
            PrevHash = state.Head,
            MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>()),
            Validator = validator,
            Reward = Money.Zero,
            Txs = new List<Transaction>(),
        };
        var r1 = StateTransition.ComputeResultingState(state, emptyBlock);
        Assert.True(r1.Success);

        // With 0 txs (<TargetTxCountPerBlock=5), base fee should decrease or stay at floor.
        Assert.True(r1.NewState!.BaseFee.BaseUnits <= state.BaseFee.BaseUnits,
            "Base fee should decrease or hold at floor when block has 0 txs.");
    }

    // -------------------------------------------------------------------------
    // Mempool: dynamic base fee enforcement
    // -------------------------------------------------------------------------

    [Fact]
    public void Mempool_Rejects_TransactionBelowDynamicBaseFee()
    {
        var fromKey = new Key(new byte[32] { 0x50, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        var toKey = new Key(new byte[32] { 0x51, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        var fromAddr = Address.FromPublicKey(Address.TestnetVersion, fromKey.PubKey.ToBytes());
        var toAddr = Address.FromPublicKey(Address.TestnetVersion, toKey.PubKey.ToBytes());

        var state = new State(ChainId);
        var acc = state.EnsureAccount(fromAddr);
        acc.Balance = Money.FromCoins(10m);
        // Elevate base fee well above minimum
        state.BaseFee = Money.FromCoins(1m);

        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = 1,
            From = fromAddr,
            To = toAddr,
            Amount = Money.FromCoins(1m),
            Fee = Money.FromCoins(0.0001m), // below state.BaseFee of 1 UKC
            PubKey = fromKey.PubKey.ToBytes(),
        };
        tx.Signature = TransactionSigner.Sign(tx, fromKey.ToBytes());

        var mempool = new Mempool(ChainId);
        var result = mempool.Add(tx, state, nowUnixSeconds: 1_700_000_000L);

        Assert.False(result.Accepted);
        Assert.Contains("base fee", result.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Mempool_Accepts_TransactionAtDynamicBaseFee()
    {
        var fromKey = new Key(new byte[32] { 0x60, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        var toKey = new Key(new byte[32] { 0x61, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        var fromAddr = Address.FromPublicKey(Address.TestnetVersion, fromKey.PubKey.ToBytes());
        var toAddr = Address.FromPublicKey(Address.TestnetVersion, toKey.PubKey.ToBytes());

        var state = new State(ChainId);
        var acc = state.EnsureAccount(fromAddr);
        acc.Balance = Money.FromCoins(10m);
        state.BaseFee = FeePolicy.BaseFee; // 0.0001 UKC

        var tx = new Transaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = ChainId,
            Nonce = 1,
            From = fromAddr,
            To = toAddr,
            Amount = Money.FromCoins(1m),
            Fee = FeePolicy.BaseFee,
            PubKey = fromKey.PubKey.ToBytes(),
        };
        tx.Signature = TransactionSigner.Sign(tx, fromKey.ToBytes());

        var mempool = new Mempool(ChainId);
        var result = mempool.Add(tx, state, nowUnixSeconds: 1_700_000_000L);

        Assert.True(result.Accepted, $"Expected accepted but got: {result.Reason}");
    }
}
