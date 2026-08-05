using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class ConsensusMultiNodeTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;
    private static readonly string[] Mnemonics =
    {
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about",
        "legal winner thank year wave sausage worth useful legal winner thank yellow",
        "letter advice cage absurd amount doctor acoustic avoid letter advice cage above"
    };

    [Fact]
    public void ThreeNodes_ReachFinalityWithTwoThirdsStake()
    {
        var paths = Enumerable.Range(0, 3).Select(_ => TempPath()).ToArray();
        try
        {
            var wallets = Mnemonics.Select(MakeWallet).ToArray();
            var validatorSet = MakeValidatorSet(wallets);
            var nodes = paths.Select(path => new CanonicalNodeService(ChainId, path, validatorSet)).ToArray();
            var proposer = validatorSet.SelectProposer(ChainId, 2, 0);
            var proposerIndex = wallets
                .Select((wallet, index) => (wallet, index))
                .Single(x => AddressFor(x.wallet).Encoded == proposer.Address.Encoded)
                .index;

            var built = nodes[proposerIndex].CreateAndCommitBlock(wallets[proposerIndex]);
            Assert.True(built.Accepted, built.Message);
            foreach (var node in nodes.Where((_, index) => index != proposerIndex))
            {
                var received = node.Add(CanonicalNodeService.ToGrpc(built.Block));
                Assert.True(received.Accepted, received.Message);
            }

            foreach (var node in nodes)
            {
                Assert.True(node.SubmitVote(node.CreateVote(built.Block, wallets[0])).Accepted);
                var quorumVote = node.SubmitVote(node.CreateVote(built.Block, wallets[1]));
                Assert.True(quorumVote.Finalized, quorumVote.Message);
                Assert.Equal(2L, node.Finality.FinalizedHeight);
            }
        }
        finally
        {
            foreach (var path in paths)
            {
                DeleteSnapshot(path);
            }
        }
    }

    [Fact]
    public void ConflictingVote_IsRejectedAndMinorityStakeCannotFinalize()
    {
        var path = TempPath();
        var minorityPath = TempPath();
        try
        {
            var wallets = Mnemonics.Select(MakeWallet).ToArray();
            var validatorSet = MakeValidatorSet(wallets);
            var node = new CanonicalNodeService(ChainId, path, validatorSet);
            var proposer = validatorSet.SelectProposer(ChainId, 2, 0);
            var proposerIndex = wallets.Select((wallet, index) => (wallet, index))
                .Single(x => AddressFor(x.wallet).Encoded == proposer.Address.Encoded).index;
            var built = node.CreateAndCommitBlock(wallets[proposerIndex]);
            Assert.True(built.Accepted, built.Message);

            Assert.True(node.SubmitVote(node.CreateVote(built.Block, wallets[0])).Accepted);
            var conflictingBlock = new Block
            {
                Version = built.Block.Version,
                ChainId = built.Block.ChainId,
                Height = built.Block.Height,
                TimeStamp = built.Block.TimeStamp + 1,
                PrevHash = built.Block.PrevHash,
                MerkleRoot = built.Block.MerkleRoot,
                StateRoot = built.Block.StateRoot,
                Validator = built.Block.Validator,
                Reward = built.Block.Reward
            };
            var conflict = node.CreateVote(conflictingBlock, wallets[0]);
            var conflictResult = node.SubmitVote(conflict);
            Assert.False(conflictResult.Accepted);

            // Validators 1 and 2 control only six of ten stake units, below 2/3+1.
            var minorityNode = new CanonicalNodeService(ChainId, minorityPath, validatorSet);
            Assert.True(minorityNode.Add(CanonicalNodeService.ToGrpc(built.Block)).Accepted);
            Assert.True(minorityNode.SubmitVote(minorityNode.CreateVote(built.Block, wallets[1])).Accepted);
            var minority = minorityNode.SubmitVote(minorityNode.CreateVote(built.Block, wallets[2]));
            Assert.False(minority.Finalized);
            Assert.Equal(1L, minorityNode.Finality.FinalizedHeight);
        }
        finally
        {
            DeleteSnapshot(path);
            DeleteSnapshot(minorityPath);
        }
    }

    private static WalletService MakeWallet(string mnemonic)
        => new()
        {
            KeyPair = WalletService.GenerateKeyPair(new NBitcoin.Mnemonic(mnemonic), 0)
        };

    private static ValidatorSet MakeValidatorSet(IReadOnlyList<WalletService> wallets)
    {
        var validators = wallets.Select((wallet, index) =>
        {
            var pubKey = wallet.GetPublicKey().PubKey.ToBytes();
            return new Validator(
                AddressFor(wallet),
                pubKey,
                Money.FromCoins(index == 0 ? 4m : 3m));
        });
        return new ValidatorSet(validators);
    }

    private static Address AddressFor(WalletService wallet)
        => Address.FromPublicKey(ChainInfo.AddressVersion(ChainId), wallet.GetPublicKey().PubKey.ToBytes());

    private static string TempPath()
        => Path.Combine(Path.GetTempPath(), $"ukc-consensus-{Guid.NewGuid():N}.json");

    private static void DeleteSnapshot(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".finality")) File.Delete(path + ".finality");
    }
}
