using System;
using System.Linq;
using UbudKusCoin.Core.Hashing;
using UbudKusCoin.Core.Types;
using Xunit;

namespace UbudKusCoin.Tests;

public class MerkleTests
{
    private static byte[] Leaf(string s) => HashUtils.DoubleSha256(System.Text.Encoding.UTF8.GetBytes(s));

    [Fact]
    public void Merkle_Empty_ReturnsZeroRoot()
    {
        var root = Merkle.ComputeRoot(Array.Empty<byte[]>());
        Assert.Equal(Merkle.ZeroRoot, root);
    }

    [Fact]
    public void Merkle_SingleLeaf_IsThatLeaf()
    {
        var leaf = Leaf("a");
        var root = Merkle.ComputeRoot(new[] { leaf });
        Assert.Equal(leaf, root);
    }

    [Fact]
    public void Merkle_TwoLeaves_Deterministic()
    {
        var a = Leaf("a");
        var b = Leaf("b");
        var root1 = Merkle.ComputeRoot(new[] { a, b });
        var root2 = Merkle.ComputeRoot(new[] { a, b });
        Assert.Equal(root1, root2);
    }

    [Fact]
    public void Merkle_OrderIsSignificant()
    {
        var a = Leaf("a");
        var b = Leaf("b");
        var ab = Merkle.ComputeRoot(new[] { a, b });
        var ba = Merkle.ComputeRoot(new[] { b, a });
        Assert.NotEqual(ab, ba);
    }

    [Fact]
    public void Merkle_OddLeaves_DuplicatesLast()
    {
        var a = Leaf("a");
        var b = Leaf("b");
        var c = Leaf("c");
        var cPair = Merkle.ComputeRoot(new[] { c, c });
        var left = HashUtils.DoubleSha256(a.Concat(b).ToArray());
        var expected = HashUtils.DoubleSha256(left.Concat(cPair).ToArray());

        var root = Merkle.ComputeRoot(new[] { a, b, c });
        Assert.Equal(expected, root);
    }
}
