using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Core.Consensus;

public interface IConsensusDriver
{
    Validator Proposer(long height, uint round);
    bool ValidateProposal(Block block, uint round, out string error);
    bool AddVote(ConsensusVote vote, out QuorumCertificate? certificate, out string error);
    bool Commit(Block block, QuorumCertificate certificate, out string error);
    IReadOnlyList<ConsensusVote> EquivocationEvidence { get; }
}

public sealed class DeterministicBftDriver : IConsensusDriver
{
    private readonly ValidatorSet validatorSet;
    private readonly State state;
    private readonly Dictionary<string, ConsensusVote> votes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ConsensusVote> votesByValidatorRound = new(StringComparer.Ordinal);
    private readonly List<ConsensusVote> equivocationEvidence = new();

    public DeterministicBftDriver(State state, ValidatorSet validatorSet)
    {
        this.state = state;
        this.validatorSet = validatorSet;
    }

    public IReadOnlyList<ConsensusVote> EquivocationEvidence => equivocationEvidence;

    public Validator Proposer(long height, uint round)
        => validatorSet.SelectProposer(state.ChainId, height, round);

    public bool ValidateProposal(Block block, uint round, out string error)
    {
        if (block.Height != state.Height + 1)
        {
            error = "Proposal height is not the next height.";
            return false;
        }

        var proposer = Proposer(block.Height, round);
        if (proposer.Address.Encoded != block.Validator.Encoded)
        {
            error = "Proposal validator is not the selected proposer.";
            return false;
        }

        var result = StateTransition.ApplyBlock(state, block);
        error = result.Error ?? string.Empty;
        return result.Success;
    }

    public bool AddVote(ConsensusVote vote, out QuorumCertificate? certificate, out string error)
    {
        certificate = null;
        if (!vote.Verify(validatorSet))
        {
            error = "Invalid consensus vote.";
            return false;
        }

        var identity = $"{vote.Validator.Encoded}:{vote.Height}:{vote.Round}";
        if (votesByValidatorRound.TryGetValue(identity, out var previous)
            && previous.BlockHash != vote.BlockHash)
        {
            equivocationEvidence.Add(previous);
            equivocationEvidence.Add(vote);
            error = "Validator equivocation detected.";
            return false;
        }

        votesByValidatorRound[identity] = vote;
        votes[$"{vote.Validator.Encoded}:{vote.BlockHash}:{vote.Height}:{vote.Round}"] = vote;
        var matching = votes.Values
            .Where(x => x.Height == vote.Height && x.Round == vote.Round && x.BlockHash == vote.BlockHash)
            .ToArray();
        var candidate = new QuorumCertificate
        {
            Height = vote.Height,
            Round = vote.Round,
            BlockHash = vote.BlockHash,
            Votes = matching
        };

        if (candidate.Verify(validatorSet))
        {
            certificate = candidate;
        }

        error = string.Empty;
        return true;
    }

    public bool Commit(Block block, QuorumCertificate certificate, out string error)
    {
        if (certificate.BlockHash != block.ComputeHeaderHashHex()
            || certificate.Height != block.Height
            || !certificate.Verify(validatorSet))
        {
            error = "Invalid quorum certificate.";
            return false;
        }

        var result = StateTransition.ApplyBlock(state, block);
        error = result.Error ?? string.Empty;
        return result.Success;
    }
}
