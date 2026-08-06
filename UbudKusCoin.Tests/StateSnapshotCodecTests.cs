using System;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class StateSnapshotCodecTests
{
    [Fact]
    public void EncodeDecode_PreservesStateAndIsDeterministic()
    {
        var address = Address.FromPublicKey(Address.TestnetVersion, new byte[] { 2, 1, 2, 3 });
        var state = new State(ChainInfo.ChainIdTestnet, 7, new byte[32], 1_700_000_007);
        state.EnsureAccount(address).Balance = Money.FromCoins(12m);
        state.SetStake(new StakePositionState
        {
            Address = address,
            PubKey = new byte[] { 2, 1, 2, 3 },
            ConsensusPubKey = new byte[32],
            Amount = Money.FromCoins(3m),
            BondedHeight = 2,
            UnlockHeight = 0,
            Jailed = false
        });

        var encoded = StateSnapshotCodec.Encode(state);
        Assert.True(StateSnapshotCodec.TryDecode(encoded, out var restored, out var error), error);
        Assert.Equal(encoded, StateSnapshotCodec.Encode(restored!));
        Assert.Equal(state.ComputeStateRoot(), restored!.ComputeStateRoot());
        Assert.Equal(state.Height, restored.Height);
        Assert.Equal(state.GetStake(address)!.Amount, restored.GetStake(address)!.Amount);
    }

    [Fact]
    public void EncodeDecode_PreservesCanonicalHeadAnchor()
    {
        var block = Genesis.CreateBlock(ChainInfo.ChainIdTestnet);
        var state = StateTransition.ApplyBlock(
            Genesis.CreateState(ChainInfo.ChainIdTestnet), block).NewState!;
        var encoded = StateSnapshotCodec.Encode(state, block);

        Assert.True(StateSnapshotCodec.TryDecode(encoded, out var restored, out var head, out var error), error);
        Assert.Equal(block.ComputeHeaderHash(), head!.ComputeHeaderHash());
        Assert.Equal(restored!.ComputeStateRoot(), head.StateRoot);
    }

    [Fact]
    public void Decode_RejectsTamperedPayload()
    {
        var state = Genesis.CreateState(ChainInfo.ChainIdTestnet);
        var encoded = StateSnapshotCodec.Encode(state);
        encoded[^1] ^= 0x01;

        Assert.False(StateSnapshotCodec.TryDecode(encoded, out _, out var error));
        Assert.Contains("snapshot", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decode_RejectsDuplicateAccounts()
    {
        var state = new State(ChainInfo.ChainIdTestnet, 0, new byte[] { 1 }, 0);
        state.EnsureAccount(Address.FromPublicKey(Address.TestnetVersion, new byte[] { 1, 2, 3 }));
        var encoded = StateSnapshotCodec.Encode(state);
        var json = System.Text.Encoding.UTF8.GetString(encoded);
        var accountsStart = json.IndexOf("\"accounts\":[", StringComparison.Ordinal) + "\"accounts\":[".Length;
        var accountsEnd = json.IndexOf("],\"stakes\"", accountsStart, StringComparison.Ordinal);
        var accountPayload = json[accountsStart..accountsEnd];
        json = json[..accountsEnd] + "," + accountPayload + json[accountsEnd..];

        Assert.False(StateSnapshotCodec.TryDecode(System.Text.Encoding.UTF8.GetBytes(json), out _, out var error));
        Assert.Contains("duplicate", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decode_RejectsOversizedPayloadWithoutThrowing()
    {
        var oversized = new byte[StateSnapshotCodec.MaxEncodedBytes + 1];

        var exception = Record.Exception(() =>
            Assert.False(StateSnapshotCodec.TryDecode(oversized, out _, out _)));

        Assert.Null(exception);
    }
}
