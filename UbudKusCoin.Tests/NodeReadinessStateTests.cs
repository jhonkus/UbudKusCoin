using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class NodeReadinessStateTests
{
    [Fact]
    public void Snapshot_IsReadyOnlyWhenAllSignalsAreReady()
    {
        NodeReadinessState.SetApplicationReady(false);
        NodeReadinessState.SetAbciSocketReady(false);
        NodeReadinessState.SetConsensusMode(ConsensusEngineMode.CometBft);
        NodeReadinessState.SetConsensusStatus(new ConsensusEngineStatus(false, "cometbft", "warming up"));

        var initial = NodeReadinessState.Snapshot();
        Assert.False(initial.Ready);
        Assert.False(initial.ApplicationReady);

        NodeReadinessState.SetApplicationReady(true);
        NodeReadinessState.SetAbciSocketReady(true);
        NodeReadinessState.SetConsensusStatus(new ConsensusEngineStatus(true, "cometbft", "healthy"));

        var ready = NodeReadinessState.Snapshot();
        Assert.True(ready.Ready);
        Assert.True(ready.ApplicationReady);
        Assert.True(ready.AbciSocketReady);
        Assert.True(ready.ConsensusReady);
    }
}
