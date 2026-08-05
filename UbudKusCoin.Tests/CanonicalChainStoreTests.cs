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
}
