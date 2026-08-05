using UbudKusCoin.Core.Hashing;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Core.Consensus;

public sealed class ConsensusVote
{
    public uint ChainId { get; init; }
    public long Height { get; init; }
    public uint Round { get; init; }
    public string BlockHash { get; init; } = string.Empty;
    public Address Validator { get; init; }
    public byte[] PubKey { get; init; } = Array.Empty<byte>();
    public byte[] Signature { get; set; } = Array.Empty<byte>();

    public byte[] ComputeDigest()
    {
        using var stream = new MemoryStream();
        HashUtils.AppendLe32(stream, ChainId);
        HashUtils.AppendLe64(stream, (ulong)Height);
        HashUtils.AppendLe32(stream, Round);
        HashUtils.AppendLengthPrefixed(stream, BlockHash);
        HashUtils.AppendLengthPrefixed(stream, Validator.Encoded);
        return HashUtils.DoubleSha256(stream.ToArray());
    }

    public bool Verify(ValidatorSet validatorSet)
    {
        try
        {
            if (!validatorSet.TryGet(Validator, out var registered)
                || !registered.PubKey.SequenceEqual(PubKey)
                || PubKey.Length is not (33 or 65)
                || Signature.Length == 0)
            {
                return false;
            }

            return new NBitcoin.PubKey(PubKey).Verify(new NBitcoin.uint256(ComputeDigest()), Signature);
        }
        catch
        {
            return false;
        }
    }
}

public sealed class QuorumCertificate
{
    public long Height { get; init; }
    public uint Round { get; init; }
    public string BlockHash { get; init; } = string.Empty;
    public IReadOnlyList<ConsensusVote> Votes { get; init; } = Array.Empty<ConsensusVote>();

    public bool Verify(ValidatorSet validatorSet)
    {
        var validVotes = Votes
            .Where(x => x.Height == Height && x.Round == Round && x.BlockHash == BlockHash)
            .Where(x => x.Verify(validatorSet))
            .GroupBy(x => x.Validator.Encoded, StringComparer.Ordinal)
            .Select(x => x.First())
            .ToArray();
        var signedStake = validVotes
            .Select(x => validatorSet.TryGet(x.Validator, out var validator) ? validator.Stake : Money.Zero)
            .Aggregate(Money.Zero, (total, stake) => total + stake);

        return validVotes.Length > 0
            && (decimal)signedStake.BaseUnits * 3 >= (decimal)validatorSet.TotalStake.BaseUnits * 2 + 1;
    }
}
