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

    [Fact]
    public void FinalityVote_IsPersistedAndRestored()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ukc-finality-{Guid.NewGuid():N}.json");
        try
        {
            var wallet = MakeWallet();
            var pubKey = wallet.GetPublicKey().PubKey.ToBytes();
            var address = Address.FromPublicKey(ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet), pubKey);
            var validators = new ValidatorSet(new[] { new Validator(address, pubKey, Money.FromCoins(1m)) });
            var node = new CanonicalNodeService(ChainInfo.ChainIdTestnet, path, validators);
            var built = node.CreateAndCommitBlock(wallet);
            var voteResult = node.SubmitVote(node.CreateVote(built.Block, wallet));

            Assert.True(built.Accepted, built.Message);
            Assert.True(voteResult.Finalized, voteResult.Message);
            Assert.Equal(2L, node.Finality.FinalizedHeight);

            var restored = new CanonicalNodeService(ChainInfo.ChainIdTestnet, path, validators);
            Assert.Equal(2L, restored.Finality.FinalizedHeight);
            Assert.Equal(node.Finality.FinalizedHash, restored.Finality.FinalizedHash);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".finality")) File.Delete(path + ".finality");
        }
    }

    [Fact]
    public void ExternalCommit_PersistsFinalityAndRestoresIt()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ukc-external-finality-{Guid.NewGuid():N}.json");
        try
        {
            var wallet = MakeWallet();
            var pubKey = wallet.GetPublicKey().PubKey.ToBytes();
            var address = Address.FromPublicKey(ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet), pubKey);
            var node = new CanonicalNodeService(ChainInfo.ChainIdTestnet, path);

            var committed = node.AcceptExternalCommit(
                Array.Empty<Transaction>(),
                Genesis.GenesisTime + 1,
                address);

            Assert.True(committed.Accepted, committed.Message);
            Assert.Equal(2L, node.Finality.FinalizedHeight);

            var restored = new CanonicalNodeService(ChainInfo.ChainIdTestnet, path);
            Assert.Equal(2L, restored.Finality.FinalizedHeight);
            Assert.Equal(node.Finality.FinalizedHash, restored.Finality.FinalizedHash);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".finality")) File.Delete(path + ".finality");
        }
    }

    [Fact]
    public void ExternalCommit_IsIdempotentForSameExternalHeightAndTransactions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ukc-external-replay-{Guid.NewGuid():N}.json");
        try
        {
            var wallet = MakeWallet();
            var validator = Address.FromPublicKey(
                ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet),
                wallet.GetPublicKey().PubKey.ToBytes());
            var node = new CanonicalNodeService(ChainInfo.ChainIdTestnet, path);

            var first = node.AcceptExternalCommit(Array.Empty<Transaction>(), Genesis.GenesisTime + 1,
                validator, externalHeight: 1);
            var second = node.AcceptExternalCommit(Array.Empty<Transaction>(), Genesis.GenesisTime + 1,
                validator, externalHeight: 1);

            Assert.True(first.Accepted, first.Message);
            Assert.True(second.Accepted, second.Message);
            Assert.Equal(first.Block.ComputeHeaderHashHex(), second.Block.ComputeHeaderHashHex());
            Assert.Equal(2L, node.Chain.State.Height);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".finality")) File.Delete(path + ".finality");
        }
    }

    [Fact]
    public void ExternalCommit_IsIdempotentWhenCanonicalHeightMatchesExternalHeight()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ukc-external-height-replay-{Guid.NewGuid():N}.json");
        try
        {
            var wallet = MakeWallet();
            var validator = Address.FromPublicKey(
                ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet),
                wallet.GetPublicKey().PubKey.ToBytes());
            var node = new CanonicalNodeService(ChainInfo.ChainIdTestnet, path);

            var first = node.AcceptExternalCommit(Array.Empty<Transaction>(), Genesis.GenesisTime + 1,
                validator, externalHeight: 1);
            var replay = node.AcceptExternalCommit(Array.Empty<Transaction>(), Genesis.GenesisTime + 1,
                validator, externalHeight: 2);

            Assert.True(first.Accepted, first.Message);
            Assert.True(replay.Accepted, replay.Message);
            Assert.Equal(first.Block.ComputeHeaderHashHex(), replay.Block.ComputeHeaderHashHex());
            Assert.Equal(2L, node.Chain.State.Height);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".finality")) File.Delete(path + ".finality");
        }
    }

    [Fact]
    public void CanonicalGrpcTransaction_PreservesStakingFields()
    {
        var transaction = new Transaction
        {
            ChainId = ChainInfo.ChainIdTestnet,
            Kind = TransactionKind.Unbond,
            Nonce = 4,
            From = new Address(ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet), new byte[] { 1, 2, 3, 4 }),
            To = new Address(ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet), new byte[] { 1, 2, 3, 4 }),
            Amount = Money.Zero,
            Fee = FeePolicy.MinRelayFee,
            LockPeriod = 120,
            PubKey = new byte[33],
            Signature = new byte[] { 1 }
        };
        var block = new Block { ChainId = transaction.ChainId, Validator = transaction.From, Txs = new() { transaction } };

        var encoded = CanonicalNodeService.ToGrpc(block);

        Assert.Equal((uint)TransactionKind.Unbond, encoded.Transactions[0].Kind);
        Assert.Equal(120, encoded.Transactions[0].LockPeriod);
    }

    [Fact]
    public void RestoreSnapshot_RehydratesCanonicalHeadAndFinality()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"ukc-snapshot-source-{Guid.NewGuid():N}.json");
        var targetPath = Path.Combine(Path.GetTempPath(), $"ukc-snapshot-target-{Guid.NewGuid():N}.json");
        try
        {
            var source = new CanonicalNodeService(ChainInfo.ChainIdTestnet, sourcePath);
            var wallet = MakeWallet();
            var validator = Address.FromPublicKey(
                ChainInfo.AddressVersion(ChainInfo.ChainIdTestnet),
                wallet.GetPublicKey().PubKey.ToBytes());
            var committed = source.AcceptExternalCommit(
                Array.Empty<Transaction>(), Genesis.GenesisTime + 1, validator, externalHeight: 1);
            Assert.True(committed.Accepted, committed.Message);

            var target = new CanonicalNodeService(ChainInfo.ChainIdTestnet, targetPath);
            Assert.True(target.RestoreSnapshot(source.Chain.State, source.Chain.Head.Block, out var error), error);

            Assert.Equal(source.Chain.State.Height, target.Chain.State.Height);
            Assert.Equal(source.Chain.State.Head, target.Chain.State.Head);
            Assert.Equal(source.Chain.State.ComputeStateRoot(), target.Chain.State.ComputeStateRoot());
            Assert.Equal(source.Finality.FinalizedHeight, target.Finality.FinalizedHeight);
        }
        finally
        {
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            if (File.Exists(targetPath)) File.Delete(targetPath);
            if (File.Exists(sourcePath + ".finality")) File.Delete(sourcePath + ".finality");
            if (File.Exists(targetPath + ".finality")) File.Delete(targetPath + ".finality");
        }
    }

    private static WalletService MakeWallet()
        => new()
        {
            KeyPair = WalletService.GenerateKeyPair(
                new NBitcoin.Mnemonic("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about"), 0)
        };
}
