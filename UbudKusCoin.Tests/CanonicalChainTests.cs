using System.Linq;
using Key = NBitcoin.Key;
using uint256 = NBitcoin.uint256;
using CoreBlock = UbudKusCoin.Core.Types.Block;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class CanonicalChainTests
{
    private const uint ChainId = ChainInfo.ChainIdTestnet;

    [Fact]
    public void InvalidBlock_IsQuarantinedWithoutChangingHead()
    {
        var chain = new CanonicalChain(ChainId);
        var before = chain.Head.Block.ComputeHeaderHashHex();
        var block = BuildBlock(chain.State, new Key(Enumerable.Repeat((byte)7, 32).ToArray()), 1);
        block.PrevHash[0] ^= 0xFF;

        Assert.False(chain.TryAccept(block, out var error));
        Assert.Contains("Unknown parent", error);
        Assert.Equal(before, chain.Head.Block.ComputeHeaderHashHex());
        Assert.Single(chain.Quarantine);
    }

    [Fact]
    public void LongerValidFork_BecomesCanonicalHead()
    {
        var chain = new CanonicalChain(ChainId);
        var keyA = new Key(Enumerable.Repeat((byte)8, 32).ToArray());
        var keyB = new Key(Enumerable.Repeat((byte)9, 32).ToArray());
        var first = BuildBlock(chain.State, keyA, 1);

        Assert.True(chain.TryAccept(first, out var firstError), firstError);

        var secondA = BuildBlock(chain.State, keyA, 2);
        var parentState = chain.Candidates.Single(x => x.Block.ComputeHeaderHashHex() == first.ComputeHeaderHashHex()).State;
        var secondB = BuildBlock(parentState, keyB, 3);

        Assert.True(chain.TryAccept(secondA, out var secondError), secondError);
        Assert.True(chain.TryAccept(secondB, out var forkError), forkError);
        Assert.Equal(3L, chain.Head.Block.Height);

        var secondBNode = chain.Candidates.Single(x => x.Block.ComputeHeaderHashHex() == secondB.ComputeHeaderHashHex());
        var thirdB = BuildBlock(secondBNode.State, keyB, 4);
        Assert.True(chain.TryAccept(thirdB, out var thirdError), thirdError);
        Assert.Equal(thirdB.ComputeHeaderHashHex(), chain.Head.Block.ComputeHeaderHashHex());
    }

    private static CoreBlock BuildBlock(State state, Key validatorKey, byte timestampOffset)
    {
        var validator = Address.FromPublicKey(ChainInfo.AddressVersion(ChainId), validatorKey.PubKey.ToBytes());
        var block = new CoreBlock
        {
            ChainId = ChainId,
            Height = state.Height + 1,
            TimeStamp = state.TimeStamp + timestampOffset,
            PrevHash = state.Head,
            MerkleRoot = Merkle.ComputeRoot(System.Array.Empty<byte[]>()),
            Validator = validator,
            Reward = Money.Zero,
            Txs = new(),
        };

        var result = StateTransition.ComputeResultingState(state, block);
        Assert.True(result.Success, result.Error);
        block.StateRoot = result.NewState!.ComputeStateRoot();
        block.ValidatorPubKey = validatorKey.PubKey.ToBytes();
        block.ValidatorSignature = validatorKey.Sign(new uint256(block.ComputeHeaderHash())).ToDER();
        return block;
    }
}
