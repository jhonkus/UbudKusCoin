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

        if (block.Height != 1 && !block.VerifyValidatorSignature())
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

            var total = tx.Amount + tx.Fee;
            if (total > sender.Balance)
            {
                return StateTransitionResult.Fail("Insufficient balance.");
            }

            sender.Balance -= total;
            sender.Nonce = tx.Nonce;

            var recipient = next.EnsureAccount(tx.To);
            recipient.Balance += tx.Amount;

            // Fees accrue to the block validator.
            validator.Balance += tx.Fee;
        }

        return StateTransitionResult.Ok(next);
    }
}
