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
    public void Decode_RejectsTamperedPayload()
    {
        var state = Genesis.CreateState(ChainInfo.ChainIdTestnet);
        var encoded = StateSnapshotCodec.Encode(state);
        encoded[^1] ^= 0x01;

        Assert.False(StateSnapshotCodec.TryDecode(encoded, out _, out var error));
        Assert.Contains("snapshot", error, StringComparison.OrdinalIgnoreCase);
    }
}
