using System;
using System.Linq;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public class GenesisTests
{
    [Fact]
    public void CreateState_IsDeterministic_SameChainIdSameRoot()
    {
        var s1 = Genesis.CreateState(ChainInfo.ChainIdTestnet);
        var s2 = Genesis.CreateState(ChainInfo.ChainIdTestnet);

        Assert.Equal(s1.ComputeStateRoot(), s2.ComputeStateRoot());
    }

    [Fact]
    public void CreateState_DifferentChainId_DifferentAddresses()
    {
        var testnet = Genesis.CreateState(ChainInfo.ChainIdTestnet);
        var mainnet = Genesis.CreateState(ChainInfo.ChainIdMainnet);

        var testnetFirst = testnet.Accounts.OrderBy(a => a.Address.Encoded).First().Address;
        var mainnetFirst = mainnet.Accounts.OrderBy(a => a.Address.Encoded).First().Address;

        Assert.NotEqual(testnetFirst, mainnetFirst);
    }

    [Fact]
    public void CreateBlock_StateRootMatchesGenesisState()
    {
        var state = Genesis.CreateState(ChainInfo.ChainIdTestnet);
        var block = Genesis.CreateBlock(ChainInfo.ChainIdTestnet);

        Assert.Equal(state.ComputeStateRoot(), block.StateRoot);
    }

    [Fact]
    public void ApplyGenesisBlock_ReturnsNewState()
    {
        var genesisState = Genesis.CreateState(ChainInfo.ChainIdTestnet);
        var block = Genesis.CreateBlock(ChainInfo.ChainIdTestnet);

        var result = StateTransition.ApplyBlock(genesisState, block);

        Assert.True(result.Success, result.Error);
        Assert.Equal(1L, result.NewState!.Height);
        Assert.True(result.NewState.Head.SequenceEqual(block.ComputeHeaderHash()));
    }

    [Fact]
    public void GenesisBlock_ApplyResult_IsDeterministic()
    {
        var r1 = StateTransition.ApplyBlock(Genesis.CreateState(ChainInfo.ChainIdTestnet), Genesis.CreateBlock(ChainInfo.ChainIdTestnet));
        var r2 = StateTransition.ApplyBlock(Genesis.CreateState(ChainInfo.ChainIdTestnet), Genesis.CreateBlock(ChainInfo.ChainIdTestnet));

        Assert.True(r1.Success && r2.Success);
        Assert.Equal(r1.NewState!.ComputeStateRoot(), r2.NewState!.ComputeStateRoot());
        Assert.True(r1.NewState.Head.SequenceEqual(r2.NewState.Head));
    }

    [Fact]
    public void GenesisAccounts_HavePositiveBalances()
    {
        var state = Genesis.CreateState(ChainInfo.ChainIdTestnet);
        Assert.True(state.Accounts.All(a => a.Balance.BaseUnits > 0));
    }
}
