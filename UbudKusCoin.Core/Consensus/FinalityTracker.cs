using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Core.Consensus;

public sealed class FinalityTracker
{
    public long FinalizedHeight { get; private set; }
    public string FinalizedHash { get; private set; } = string.Empty;

    public bool Restore(long height, string hash, CanonicalChain chain, out string error)
    {
        var candidate = chain.Candidates.FirstOrDefault(x => x.Block.Height == height
            && x.Block.ComputeHeaderHashHex() == hash);
        if (candidate is null || !chain.IsCanonical(candidate.Block) || height < 1)
        {
            error = "Persisted finality does not match the canonical chain.";
            return false;
        }

        FinalizedHeight = height;
        FinalizedHash = hash;
        error = string.Empty;
        return true;
    }

    public bool TryFinalize(Block block, QuorumCertificate certificate, ValidatorSet validatorSet, out string error)
    {
        var hash = block.ComputeHeaderHashHex();
        if (certificate.Height != block.Height || certificate.BlockHash != hash
            || !certificate.Verify(validatorSet))
        {
            error = "Invalid finality certificate.";
            return false;
        }

        if (block.Height != FinalizedHeight + 1)
        {
            error = "Finality must advance sequentially.";
            return false;
        }

        FinalizedHeight = block.Height;
        FinalizedHash = hash;
        error = string.Empty;
        return true;
    }
}
