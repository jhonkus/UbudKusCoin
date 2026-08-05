using UbudKusCoin.Core.Hashing;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// Merkle root computation over an ordered list of leaves. Odd leaves are
/// duplicated; an empty list yields the zero root. The order of leaves is
/// significant (the caller must define a canonical order).
/// </summary>
public static class Merkle
{
    public static readonly byte[] ZeroRoot = new byte[32];

    public static byte[] ComputeRoot(IReadOnlyList<byte[]> leaves)
    {
        if (leaves is null || leaves.Count == 0)
        {
            return ZeroRoot;
        }

        if (leaves.Any(l => l is null || l.Length != 32))
        {
            throw new ArgumentException("Each Merkle leaf must be exactly 32 bytes.", nameof(leaves));
        }

        var level = new List<byte[]>(leaves);

        while (level.Count > 1)
        {
            var next = new List<byte[]>((level.Count + 1) / 2);
            for (int i = 0; i < level.Count; i += 2)
            {
                byte[] left = level[i];
                byte[] right = (i + 1 < level.Count) ? level[i + 1] : level[i];
                next.Add(HashUtils.DoubleSha256(left.Concat(right).ToArray()));
            }

            level = next;
        }

        return level[0];
    }
}
