namespace UbudKusCoin.Core.Types;

/// <summary>Outcome of applying a block to a state.</summary>
public sealed class StateTransitionResult
{
    public bool Success { get; }
    public string? Error { get; }
    public State? NewState { get; }

    private StateTransitionResult(bool success, string? error, State? newState)
    {
        Success = success;
        Error = error;
        NewState = newState;
    }

    public static StateTransitionResult Ok(State s) => new(true, null, s);
    public static StateTransitionResult Fail(string error) => new(false, error, null);
}

/// <summary>
/// Deterministic, atomic state transition. Applying a block never mutates the
/// input <see cref="State"/>; it works on a derived copy and only returns it if
/// the entire block is valid, so there is never a partially-applied state.
///
/// Rules enforced:
///   - block.chain_id == state.chain_id
///   - block.height == state.height + 1
///   - block.prev_hash == state.head
///   - transaction merkle root matches block.merkle_root
///   - each transfer: nonce == sender.nonce + 1 and balance >= amount + fee
///   - sender pays amount+fee; recipient receives amount; fee goes to validator
///   - validator receives the coinbase subsidy (block.reward)
///   - resulting state_root == block.state_root
/// </summary>
public static class StateTransition
{
    /// <summary>
    /// Validates and applies a block deterministically and atomically. The input
    /// state is never mutated. All validity rules (including the state root) are
    /// enforced here.
    /// </summary>
    public static StateTransitionResult ApplyBlock(State state, Block block)
        => ApplyBlock(state, block, requireValidatorSignature: true);

    /// <summary>
    /// Applies a block already committed by the external consensus engine.
    /// Validator authentication belongs to the engine commit, while all
    /// application state and state-root rules remain enforced here.
    /// </summary>
    public static StateTransitionResult ApplyCommittedBlock(State state, Block block)
        => ApplyBlock(state, block, requireValidatorSignature: false);

    public static StateTransitionResult ApplyCommittedBlock(
        State state, Block block, IReadOnlyList<ConsensusEvidence> evidence)
    {
        block.Evidence = evidence.ToList();
        return ApplyBlock(state, block, requireValidatorSignature: false);
    }

    private static StateTransitionResult ApplyBlock(State state, Block block, bool requireValidatorSignature)
    {
        if (block.ChainId != state.ChainId)
        {
            return StateTransitionResult.Fail("ChainId mismatch.");
        }

        if (block.Height != state.Height + 1)
        {
            return StateTransitionResult.Fail("Invalid height.");
        }

        if (block.TimeStamp <= state.TimeStamp)
        {
            return StateTransitionResult.Fail("Invalid timestamp.");
        }

        if (block.Version != ChainInfo.TxVersion
            || block.Validator.Version != ChainInfo.AddressVersion(state.ChainId))
        {
            return StateTransitionResult.Fail("Invalid block version or validator address.");
        }

        if (requireValidatorSignature && block.Height != 1 && !block.VerifyValidatorSignature())
        {
            return StateTransitionResult.Fail("Invalid validator signature.");
        }

        if (!block.PrevHash.SequenceEqual(state.Head))
        {
            return StateTransitionResult.Fail("PrevHash mismatch.");
        }

        var merkle = Merkle.ComputeRoot(block.Txs.Select(t => t.ComputeId()).ToArray());
        if (!merkle.SequenceEqual(block.MerkleRoot))
        {
            return StateTransitionResult.Fail("Transaction Merkle root mismatch.");
        }

        if (block.Txs.Select(t => t.ComputeIdHex()).Distinct(StringComparer.Ordinal).Count() != block.Txs.Count)
        {
            return StateTransitionResult.Fail("Duplicate transaction in block.");
        }

var applied = ComputeResultingState(state, block);
        if (!applied.Success)
        {
            return applied;
        }

        var next = applied.NewState!;
        var newRoot = next.ComputeStateRoot();
        if (!newRoot.SequenceEqual(block.StateRoot))
        {
            return StateTransitionResult.Fail("State root mismatch.");
        }

        next.Advance(block.Height, block.TimeStamp, block.ComputeHeaderHash());
        return StateTransitionResult.Ok(next);
    }

    /// <summary>
    /// Applies the block to a derived copy and returns the resulting state,
    /// without checking the block's declared state root. Used by block builders
    /// to compute the correct state root. Returns a failed result with the
    /// specific reason if a transaction violates a deterministic rule (nonce,
    /// balance, missing sender).
    /// </summary>
    public static StateTransitionResult ComputeResultingState(State state, Block block)
    {
        // Work on a copy; commit only if the whole block is valid.
        var next = state.Derive();

        // Coinbase subsidy to the validator.
        var validator = next.EnsureAccount(block.Validator);
        validator.Balance += block.Reward;

        foreach (var evidence in block.Evidence)
        {
            if (evidence.Kind is not (ConsensusEvidenceKind.DuplicateVote or ConsensusEvidenceKind.LightClientAttack))
                return StateTransitionResult.Fail("Unsupported consensus evidence.");
            if (evidence.Height <= 0 || evidence.Height > block.Height)
                return StateTransitionResult.Fail("Consensus evidence height is invalid.");

            var stake = next.GetStake(evidence.Validator);
            if (stake is null || stake.Jailed)
                continue;

            // Slash one third and jail. Slashed coins are burned, never paid to
            // the proposer, so evidence cannot become a reward mechanism.
            stake.Amount = new Money(stake.Amount.BaseUnits - stake.Amount.BaseUnits / 3);
            stake.Jailed = true;
            stake.UnlockHeight = 0;
        }

        foreach (var tx in block.Txs)
        {
            if (!tx.IsEnvelopeWellFormed(state.ChainId) || !tx.VerifySignature())
            {
                return StateTransitionResult.Fail("Invalid transaction envelope or signature.");
            }

            if ((tx.ValidFrom > 0 && block.TimeStamp < tx.ValidFrom)
                || (tx.ValidUntil > 0 && block.TimeStamp > tx.ValidUntil))
            {
                return StateTransitionResult.Fail("Transaction is outside its validity window.");
            }

            var sender = next.GetAccount(tx.From);
            if (sender is null)
            {
                return StateTransitionResult.Fail("Sender account does not exist.");
            }

            if (tx.Nonce != sender.Nonce + 1)
            {
                return StateTransitionResult.Fail("Invalid nonce.");
            }

            var operationCost = tx.Kind is TransactionKind.Transfer or TransactionKind.Bond
                ? tx.Amount
                : Money.Zero;
            var total = operationCost + tx.Fee;
            if (total > sender.Balance)
            {
                return StateTransitionResult.Fail("Insufficient balance.");
            }

            sender.Balance -= total;
            sender.Nonce = tx.Nonce;

            // Fees accrue to the block validator.
            validator.Balance += tx.Fee;

            switch (tx.Kind)
            {
                case TransactionKind.Transfer:
                    next.EnsureAccount(tx.To).Balance += tx.Amount;
                    break;
                case TransactionKind.Bond:
                {
                    var existing = next.GetStake(tx.From);
                    if (existing is not null && (existing.Jailed || existing.UnlockHeight != 0))
                        return StateTransitionResult.Fail("Stake position is jailed or unbonding.");

                    if (existing is null)
                    {
                        next.SetStake(new StakePositionState
                        {
                            Address = tx.From,
                            PubKey = tx.PubKey.ToArray(),
                            Amount = tx.Amount,
                            BondedHeight = block.Height
                        });
                    }
                    else
                    {
                        if (!existing.PubKey.SequenceEqual(tx.PubKey))
                            return StateTransitionResult.Fail("Stake public key mismatch.");
                        existing.Amount += tx.Amount;
                    }
                    break;
                }
                case TransactionKind.Unbond:
                {
                    var stake = next.GetStake(tx.From);
                    if (stake is null || stake.Jailed || stake.UnlockHeight != 0)
                        return StateTransitionResult.Fail("Stake position cannot be unbonded.");
                    stake.UnlockHeight = checked(block.Height + tx.LockPeriod);
                    break;
                }
                case TransactionKind.Withdraw:
                {
                    var stake = next.GetStake(tx.From);
                    if (stake is null || stake.UnlockHeight == 0 || block.Height < stake.UnlockHeight)
                        return StateTransitionResult.Fail("Stake position is not unlocked.");
                    sender.Balance += stake.Amount;
                    next.RemoveStake(tx.From);
                    break;
                }
            }
        }

        return StateTransitionResult.Ok(next);
    }
}
