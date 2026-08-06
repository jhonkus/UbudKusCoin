using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Core.Consensus;

public sealed record ApplicationCheckResult(bool Accepted, string Message);

public sealed record ApplicationProposalResult(
    bool Accepted,
    IReadOnlyList<Transaction> Transactions,
    string Message);

public sealed record ApplicationFinalizeResult(
    bool Accepted,
    State? State,
    byte[] AppHash,
    string Message);

/// <summary>
/// Deterministic application boundary for an ABCI adapter. It owns mempool
/// admission and state transitions; the consensus engine owns rounds, votes,
/// proposer selection, and commit certificates.
/// </summary>
public sealed class ConsensusApplicationStateMachine
{
    private readonly object _gate = new();
    private readonly Address _validator;
    private readonly int _maxProposalBytes;
    private readonly Func<long> _clock;
    private State _state;

    public ConsensusApplicationStateMachine(
        State state,
        Address validator,
        int maxProposalBytes = 1_000_000,
        Func<long>? clock = null)
    {
        if (maxProposalBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxProposalBytes));
        }

        _state = state ?? throw new ArgumentNullException(nameof(state));
        _validator = validator;
        _maxProposalBytes = maxProposalBytes;
        _clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    public State State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public ApplicationCheckResult CheckTx(Transaction transaction)
    {
        lock (_gate)
        {
            return CheckTransaction(_state, transaction, _clock());
        }
    }

    public ApplicationProposalResult PrepareProposal(IEnumerable<Transaction> transactions, int? maxBytes = null)
    {
        if (transactions is null)
        {
            throw new ArgumentNullException(nameof(transactions));
        }

        lock (_gate)
        {
            var budget = maxBytes ?? _maxProposalBytes;
            if (budget <= 0)
            {
                return new ApplicationProposalResult(false, Array.Empty<Transaction>(), "Proposal byte limit must be positive.");
            }

            var selected = new List<Transaction>();
            var candidateState = _state;
            var timestamp = Math.Max(_clock(), _state.TimeStamp + 1);
            var usedBytes = 0;

            foreach (var transaction in transactions
                .Where(x => x is not null)
                .OrderBy(x => x.ComputeIdHex(), StringComparer.Ordinal))
            {
                var size = transaction.ComputeSerializedSize();
                if (usedBytes + size > budget)
                {
                    continue;
                }

                var candidateBlock = BuildBlock(candidateState, new[] { transaction }, timestamp);
                var applied = StateTransition.ComputeResultingState(candidateState, candidateBlock);
                if (!applied.Success)
                {
                    continue;
                }

                selected.Add(transaction);
                candidateState = applied.NewState!;
                usedBytes += size;
            }

            return new ApplicationProposalResult(true, selected, "Proposal prepared.");
        }
    }

    public ApplicationCheckResult ProcessProposal(IEnumerable<Transaction> transactions, long timeStamp)
    {
        if (transactions is null)
        {
            return new ApplicationCheckResult(false, "Proposal transactions are required.");
        }

        lock (_gate)
        {
            var list = transactions.ToList();
            var block = BuildBlock(_state, list, timeStamp);
            var result = StateTransition.ComputeResultingState(_state, block);
            return result.Success
                ? new ApplicationCheckResult(true, "Proposal accepted.")
                : new ApplicationCheckResult(false, result.Error ?? "Proposal rejected.");
        }
    }

    public ApplicationFinalizeResult FinalizeBlock(IEnumerable<Transaction> transactions, long timeStamp)
    {
        if (transactions is null)
        {
            return new ApplicationFinalizeResult(false, null, Array.Empty<byte>(), "Block transactions are required.");
        }

        lock (_gate)
        {
            var block = BuildBlock(_state, transactions.ToList(), timeStamp);
            var result = StateTransition.ApplyCommittedBlock(_state, block);
            if (!result.Success)
            {
                return new ApplicationFinalizeResult(false, null, Array.Empty<byte>(), result.Error ?? "Block rejected.");
            }

            _state = result.NewState!;
            return new ApplicationFinalizeResult(true, _state, _state.ComputeStateRoot(), "Block finalized.");
        }
    }

    private static ApplicationCheckResult CheckTransaction(State state, Transaction transaction, long now)
    {
        if (transaction is null || !transaction.IsEnvelopeWellFormed(state.ChainId) || !transaction.VerifySignature())
        {
            return new ApplicationCheckResult(false, "Invalid transaction envelope or signature.");
        }

        if ((transaction.ValidFrom > 0 && now < transaction.ValidFrom)
            || (transaction.ValidUntil > 0 && now > transaction.ValidUntil))
        {
            return new ApplicationCheckResult(false, "Transaction is outside its validity window.");
        }

        var sender = state.GetAccount(transaction.From);
        if (sender is null || transaction.Nonce != sender.Nonce + 1)
        {
            return new ApplicationCheckResult(false, "Invalid nonce or unknown sender.");
        }

        return transaction.Amount + transaction.Fee <= sender.Balance
            ? new ApplicationCheckResult(true, "Transaction accepted.")
            : new ApplicationCheckResult(false, "Insufficient balance.");
    }

    private Block BuildBlock(State state, IEnumerable<Transaction> transactions, long timeStamp)
    {
        var list = transactions.ToList();
        var block = new Block
        {
            ChainId = state.ChainId,
            Height = state.Height + 1,
            TimeStamp = Math.Max(timeStamp, state.TimeStamp + 1),
            PrevHash = state.Head,
            MerkleRoot = Merkle.ComputeRoot(list.Select(x => x.ComputeId()).ToArray()),
            Validator = _validator,
            Reward = Money.Zero,
            Txs = list
        };
        var resulting = StateTransition.ComputeResultingState(state, block);
        block.StateRoot = resulting.Success ? resulting.NewState!.ComputeStateRoot() : Merkle.ZeroRoot;
        return block;
    }
}
