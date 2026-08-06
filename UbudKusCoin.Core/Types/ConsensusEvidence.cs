namespace UbudKusCoin.Core.Types;

public enum ConsensusEvidenceKind : uint
{
    DuplicateVote = 1,
    LightClientAttack = 2
}

/// <summary>Evidence already authenticated and committed by the consensus engine.</summary>
public sealed record ConsensusEvidence(
    ConsensusEvidenceKind Kind,
    Address Validator,
    long Height);
