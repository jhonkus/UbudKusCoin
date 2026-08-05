using System;
using System.IO;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class CanonicalNodeIntegrationTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;
    private const string MnemonicWords = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Fact]
    public void TwoNodes_ExchangeAndAgreeOnCanonicalHead()
    {
        var firstPath = TempPath("first");
        var secondPath = TempPath("second");
        try
        {
            var first = new CanonicalNodeService(ChainId, firstPath);
            var second = new CanonicalNodeService(ChainId, secondPath);
            var wallet = MakeWallet();

            var built = first.CreateAndCommitBlock(wallet);
            Assert.True(built.Accepted, built.Message);

            var received = second.Add(CanonicalNodeService.ToGrpc(built.Block));
            Assert.True(received.Accepted, received.Message);
            Assert.Equal(first.Chain.State.Head, second.Chain.State.Head);
            Assert.Equal(first.Chain.State.ComputeStateRoot(), second.Chain.State.ComputeStateRoot());
        }
        finally
        {
            DeleteSnapshot(firstPath);
            DeleteSnapshot(secondPath);
        }
    }

    [Fact]
    public void TwoNodes_RejectTamperedAndDuplicateBlocks()
    {
        var firstPath = TempPath("tamper-first");
        var secondPath = TempPath("tamper-second");
        try
        {
            var first = new CanonicalNodeService(ChainId, firstPath);
            var second = new CanonicalNodeService(ChainId, secondPath);
            var built = first.CreateAndCommitBlock(MakeWallet());
            Assert.True(built.Accepted, built.Message);

            var tampered = CanonicalNodeService.ToGrpc(built.Block);
            var tamperedRoot = tampered.StateRoot.ToByteArray();
            tamperedRoot[0] ^= 0xFF;
            tampered.StateRoot = Google.Protobuf.ByteString.CopyFrom(tamperedRoot);
            var rejected = second.Add(tampered);

            Assert.False(rejected.Accepted);
            Assert.Equal(1L, second.Chain.State.Height);
            Assert.Single(second.Chain.Quarantine);

            var accepted = second.Add(CanonicalNodeService.ToGrpc(built.Block));
            Assert.True(accepted.Accepted, accepted.Message);
            var duplicate = second.Add(CanonicalNodeService.ToGrpc(built.Block));

            Assert.False(duplicate.Accepted);
            Assert.True(second.Chain.Quarantine.Count == 2);
            Assert.Equal(2L, second.Chain.State.Height);
        }
        finally
        {
            DeleteSnapshot(firstPath);
            DeleteSnapshot(secondPath);
        }
    }

    private static WalletService MakeWallet()
        => new()
        {
            KeyPair = WalletService.GenerateKeyPair(new NBitcoin.Mnemonic(MnemonicWords), 0)
        };

    private static string TempPath(string name)
        => Path.Combine(Path.GetTempPath(), $"ukc-node-{name}-{Guid.NewGuid():N}.json");

    private static void DeleteSnapshot(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
