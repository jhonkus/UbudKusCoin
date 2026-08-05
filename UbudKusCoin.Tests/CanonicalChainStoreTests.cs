using System;
using System.IO;
using UbudKusCoin.Core.Types;
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
}
