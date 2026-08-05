using System;
using System.IO;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class CanonicalChainStoreTests
{
    [Fact]
    public void SaveAndLoad_RebuildsGenesisThroughValidation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ukc-chain-{Guid.NewGuid():N}.json");
        try
        {
            var original = new CanonicalChain(ChainInfo.ChainIdTestnet);
            new CanonicalChainStore(path).Save(original);

            var restored = new CanonicalChainStore(path).Load();

            Assert.Equal(original.State.ChainId, restored.State.ChainId);
            Assert.Equal(original.State.Height, restored.State.Height);
            Assert.Equal(original.State.Head, restored.State.Head);
            Assert.Equal(original.State.ComputeStateRoot(), restored.State.ComputeStateRoot());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void NodeBuilder_CreatesSignedCanonicalBlock()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ukc-builder-{Guid.NewGuid():N}.json");
        try
        {
            var wallet = new WalletService
            {
                KeyPair = WalletService.GenerateKeyPair(
                    new NBitcoin.Mnemonic("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about"), 0)
            };
            var node = new CanonicalNodeService(ChainInfo.ChainIdTestnet, path);

            var result = node.CreateAndCommitBlock(wallet);

            Assert.True(result.Accepted, result.Message);
            Assert.True(result.Block.VerifyValidatorSignature());
            Assert.Equal(2L, node.Chain.State.Height);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ConfiguredNode_QuarantinesNonProposerBlock()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"ukc-source-{Guid.NewGuid():N}.json");
        var targetPath = Path.Combine(Path.GetTempPath(), $"ukc-target-{Guid.NewGuid():N}.json");
        try
        {
            var selectedWallet = MakeWallet();
            var otherWallet = new WalletService
            {
                KeyPair = WalletService.GenerateKeyPair(
                    new NBitcoin.Mnemonic("legal winner thank year wave sausage worth useful legal winner thank yellow"), 0)
            };
            var publicKey = selectedWallet.GetPublicKey().PubKey.ToBytes();
            var address = Address.FromPublicKey(ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet), publicKey);
            var validators = new ValidatorSet(new[] { new Validator(address, publicKey, Money.FromCoins(1m)) });
            var source = new CanonicalNodeService(ChainInfo.ChainIdTestnet, sourcePath);
            var target = new CanonicalNodeService(ChainInfo.ChainIdTestnet, targetPath, validators);
            var built = source.CreateAndCommitBlock(otherWallet);

            var result = target.Add(CanonicalNodeService.ToGrpc(built.Block));

            Assert.False(result.Accepted);
            Assert.Equal(1L, target.Chain.State.Height);
            Assert.Single(target.Chain.Quarantine);
        }
        finally
        {
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            if (File.Exists(targetPath)) File.Delete(targetPath);
        }
    }

    private static WalletService MakeWallet()
        => new()
        {
            KeyPair = WalletService.GenerateKeyPair(
                new NBitcoin.Mnemonic("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about"), 0)
        };
}
