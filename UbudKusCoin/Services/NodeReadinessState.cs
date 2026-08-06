using System;

namespace UbudKusCoin.Services;

public sealed record NodeReadinessSnapshot(
    bool ApplicationReady,
    bool AbciSocketReady,
    bool ConsensusReady,
    bool Ready,
    string ConsensusEngine,
    string ConsensusMessage,
    DateTimeOffset CheckedAtUtc);

public static class NodeReadinessState
{
    private static readonly object Sync = new();
    private static bool applicationReady;
    private static bool abciSocketReady;
    private static ConsensusEngineStatus consensusStatus = new(false, "unknown", "Consensus engine has not been checked.");
    private static ConsensusEngineMode consensusMode = ConsensusEngineMode.Development;
    private static DateTimeOffset checkedAtUtc = DateTimeOffset.UnixEpoch;

    public static void SetApplicationReady(bool ready)
    {
        lock (Sync)
        {
            applicationReady = ready;
        }
    }

    public static void SetAbciSocketReady(bool ready)
    {
        lock (Sync)
        {
            abciSocketReady = ready;
        }
    }

    public static void SetConsensusMode(ConsensusEngineMode mode)
    {
        lock (Sync)
        {
            consensusMode = mode;
        }
    }

    public static void SetConsensusStatus(ConsensusEngineStatus status)
    {
        lock (Sync)
        {
            consensusStatus = status;
            checkedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public static NodeReadinessSnapshot Snapshot()
    {
        lock (Sync)
        {
            var consensusReady = consensusMode != ConsensusEngineMode.CometBft || consensusStatus.Healthy;
            var ready = applicationReady && abciSocketReady && consensusReady;
            return new NodeReadinessSnapshot(
                applicationReady,
                abciSocketReady,
                consensusReady,
                ready,
                consensusStatus.Engine,
                consensusStatus.Message,
                checkedAtUtc);
        }
    }
}
