using System;
using System.IO;
using System.Linq;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class CanonicalSyncVerifierTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;
    private const string MnemonicWords = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    [Fact]
    public void PeerHeadValidation_UsesHeightAndHash()
    {
        var path = TempPath("peer-head");
        try
        {
            var service = new CanonicalNodeService(ChainId, path);
            var block = service.CreateAndCommitBlock(MakeWallet()).Block;
            var grpc = CanonicalNodeService.ToGrpc(block);
            var computedHash = CanonicalSyncVerifier.ComputeBlockHash(grpc);

            Assert.True(CanonicalSyncVerifier.TryValidatePeerHead(
                grpc,
                1,
                service.Chain.Head.Block.PrevHash.Length == 0
                    ? string.Empty
                    : Convert.ToHexStringLower(service.Chain.Head.Block.PrevHash),
                computedHash,
                out var error), error);

            grpc.StateRoot = Google.Protobuf.ByteString.CopyFrom(new byte[32]);
            Assert.False(CanonicalSyncVerifier.TryValidatePeerHead(
                grpc,
                1,
                service.Chain.Head.Block.PrevHash.Length == 0
                    ? string.Empty
                    : Convert.ToHexStringLower(service.Chain.Head.Block.PrevHash),
                computedHash,
                out error));
            Assert.Contains("hash", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteSnapshot(path);
        }
    }

    [Fact]
    public void CanonicalRangeValidation_RejectsBrokenSequence()
    {
        var path = TempPath("range");
        try
        {
            var service = new CanonicalNodeService(ChainId, path);
            var first = CanonicalNodeService.ToGrpc(service.CreateAndCommitBlock(MakeWallet()).Block);
            var second = CanonicalNodeService.ToGrpc(service.CreateAndCommitBlock(MakeWallet()).Block);
            var firstHash = CanonicalSyncVerifier.ComputeBlockHash(first);
            var secondHash = CanonicalSyncVerifier.ComputeBlockHash(second);
            second.StateRoot = Google.Protobuf.ByteString.CopyFrom(new byte[32]);

            Assert.True(CanonicalSyncVerifier.TryValidateCanonicalRange(
                new[] { first },
                1,
                Convert.ToHexStringLower(first.PrevHash.ToByteArray()),
                firstHash,
                out var firstError), firstError);

            Assert.False(CanonicalSyncVerifier.TryValidateCanonicalRange(
                new[] { first, second },
                1,
                Convert.ToHexStringLower(first.PrevHash.ToByteArray()),
                secondHash,
                out var secondError));
            Assert.Contains("head hash", secondError, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteSnapshot(path);
        }
    }

    private static WalletService MakeWallet()
        => new()
        {
            KeyPair = WalletService.GenerateKeyPair(new NBitcoin.Mnemonic(MnemonicWords), 0)
        };

    private static string TempPath(string name)
        => Path.Combine(Path.GetTempPath(), $"ukc-sync-{name}-{Guid.NewGuid():N}.json");

    private static void DeleteSnapshot(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
