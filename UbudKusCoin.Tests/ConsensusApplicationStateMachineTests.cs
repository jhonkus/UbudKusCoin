using System;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class ConsensusApplicationStateMachineTests
{
    [Fact]
    public void FinalizeBlock_AdvancesStateAtomicallyWithoutLocalValidatorSignature()
    {
        var validator = Address.FromPublicKey(
            ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet),
            Convert.FromHexString("0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798"));
        var machine = new ConsensusApplicationStateMachine(
            new State(ChainInfo.ChainIdTestnet),
            validator,
            clock: () => 10);

        var result = machine.FinalizeBlock(Array.Empty<Transaction>(), 10);

        Assert.True(result.Accepted, result.Message);
        Assert.NotNull(result.State);
        Assert.Equal(1, result.State!.Height);
        Assert.Equal(result.State.ComputeStateRoot(), result.AppHash);
    }

    [Fact]
    public void PrepareAndProcessProposal_AreDeterministicForEmptyProposal()
    {
        var validator = Address.FromPublicKey(
            ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet),
            Convert.FromHexString("0279be667ef9dcbbac55a06295ce870b07029bfcdb2dce28d959f2815b16f81798"));
        var machine = new ConsensusApplicationStateMachine(new State(ChainInfo.ChainIdTestnet), validator, clock: () => 20);

        var prepared = machine.PrepareProposal(Array.Empty<Transaction>());
        var processed = machine.ProcessProposal(prepared.Transactions, 20);

        Assert.True(prepared.Accepted, prepared.Message);
        Assert.True(processed.Accepted, processed.Message);
        Assert.Empty(prepared.Transactions);
    }
}
