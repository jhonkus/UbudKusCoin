using System;
using System.IO;
using System.Linq;
using NBitcoin;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using CoreMoney = UbudKusCoin.Core.Types.Money;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;
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

    [Fact]
    public void DefaultManifest_ReproducesGenesisStateRoot()
    {
        var manifest = Genesis.CreateDefaultManifest(ChainInfo.ChainIdTestnet);
        manifest.Validate(ChainInfo.ChainIdTestnet);

        Assert.Equal(
            Genesis.CreateState(ChainInfo.ChainIdTestnet).ComputeStateRoot(),
            Genesis.CreateState(manifest).ComputeStateRoot());
    }

    [Fact]
    public void Manifest_RejectsUnexpectedChainId()
    {
        var manifest = Genesis.CreateDefaultManifest(ChainInfo.ChainIdTestnet);

        Assert.Throws<InvalidDataException>(() => manifest.Validate(ChainInfo.ChainIdMainnet));
    }

    [Fact]
    public void GenesisFixtureAccount_CanSignBondTransaction()
    {
        var privateKey = new byte[32];
        privateKey[^1] = 1;
        var key = new Key(privateKey);
        var publicKey = key.PubKey.ToBytes();
        var address = Address.FromPublicKey(Address.TestnetVersion, publicKey);
        var transaction = new CoreTransaction
        {
            ChainId = ChainInfo.ChainIdTestnet,
            Kind = TransactionKind.Bond,
            Nonce = 1,
            From = address,
            To = address,
            Amount = CoreMoney.FromCoins(1m),
            Fee = FeePolicy.MinRelayFee,
            PubKey = publicKey,
            ValidatorPubKey = Enumerable.Repeat((byte)7, 32).ToArray()
        };
        transaction.Signature = TransactionSigner.Sign(transaction, privateKey);

        Assert.Contains(Genesis.CreateState(ChainInfo.ChainIdTestnet).Accounts,
            account => account.Address.Equals(address));
        Assert.True(transaction.IsEnvelopeWellFormed(ChainInfo.ChainIdTestnet));
        Assert.True(transaction.VerifySignature());
    }

}
