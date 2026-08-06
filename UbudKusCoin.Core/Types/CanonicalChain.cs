namespace UbudKusCoin.Core.Types;

/// <summary>
/// In-memory canonical chain index. Every accepted block is applied to the
/// exact parent state before it becomes a candidate; invalid candidates are
/// retained as quarantine evidence and never alter the active state.
/// </summary>
public sealed class CanonicalChain
{
    private readonly Dictionary<string, ChainNode> nodes = new(StringComparer.Ordinal);
    private readonly List<QuarantinedBlock> quarantine = new();

    public CanonicalChain(uint chainId)
    {
        var genesis = Genesis.CreateBlock(chainId);
        var state = Genesis.CreateState(chainId);
        var result = StateTransition.ApplyBlock(state, genesis);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Genesis is invalid: {result.Error}");
        }

        var node = new ChainNode(genesis, result.NewState!);
        nodes[genesis.ComputeHeaderHashHex()] = node;
        Head = node;
    }

    public ChainNode Head { get; private set; }
    public State State => Head.State;
    public IReadOnlyList<QuarantinedBlock> Quarantine => quarantine;
    public IReadOnlyCollection<ChainNode> Candidates => nodes.Values;

    public static bool TryRestoreSnapshot(Block head, State state, out CanonicalChain? chain, out string error)
    {
        chain = null;
        error = string.Empty;
        if (head.ChainId != state.ChainId
            || head.Height != state.Height
            || !head.StateRoot.SequenceEqual(state.ComputeStateRoot())
            || !head.ComputeHeaderHash().SequenceEqual(state.Head))
        {
            error = "Snapshot anchor does not match the application state.";
            return false;
        }

        var restored = new CanonicalChain(state.ChainId);
        restored.nodes.Clear();
        var node = new ChainNode(head, state);
        restored.nodes[head.ComputeHeaderHashHex()] = node;
        restored.Head = node;
        chain = restored;
        return true;
    }

    public void AddQuarantine(Block block, string reason)
        => quarantine.Add(new QuarantinedBlock(block, reason));

    public IReadOnlyList<Block> GetCanonicalBlocks(long startHeight)
    {
        var result = new List<Block>();
        var current = Head;
        while (current.Block.Height > startHeight)
        {
            result.Add(current.Block);
            var parentHash = Convert.ToHexStringLower(current.Block.PrevHash);
            if (!nodes.TryGetValue(parentHash, out current!))
            {
                break;
            }
        }

        result.Reverse();
        return result;
    }

    public bool IsCanonical(Block block)
        => GetCanonicalBlocks(block.Height - 1)
            .Any(x => x.ComputeHeaderHashHex() == block.ComputeHeaderHashHex());

    public bool TryAccept(Block block, out string error)
    {
        var hash = block.ComputeHeaderHashHex();
        if (nodes.ContainsKey(hash))
        {
            error = "Duplicate block.";
            AddQuarantine(block, error);
            return false;
        }

        var parentHash = Convert.ToHexStringLower(block.PrevHash);
        if (!nodes.TryGetValue(parentHash, out var parent))
        {
            error = "Unknown parent block.";
            AddQuarantine(block, error);
            return false;
        }

        var result = StateTransition.ApplyBlock(parent.State, block);
        if (!result.Success)
        {
            error = result.Error ?? "Block rejected.";
            AddQuarantine(block, error);
            return false;
        }

        var candidate = new ChainNode(block, result.NewState!);
        nodes[hash] = candidate;
        if (IsPreferred(candidate, Head))
        {
            Head = candidate;
        }

        error = string.Empty;
        return true;
    }

    public bool TryAcceptCommitted(Block block, out string error)
    {
        var hash = block.ComputeHeaderHashHex();
        if (nodes.ContainsKey(hash))
        {
            error = "Duplicate block.";
            AddQuarantine(block, error);
            return false;
        }

        var parentHash = Convert.ToHexStringLower(block.PrevHash);
        if (!nodes.TryGetValue(parentHash, out var parent))
        {
            error = "Unknown parent block.";
            AddQuarantine(block, error);
            return false;
        }

        var result = StateTransition.ApplyCommittedBlock(parent.State, block);
        if (!result.Success)
        {
            error = result.Error ?? "Committed block rejected.";
            AddQuarantine(block, error);
            return false;
        }

        var candidate = new ChainNode(block, result.NewState!);
        nodes[hash] = candidate;
        if (IsPreferred(candidate, Head))
        {
            Head = candidate;
        }

        error = string.Empty;
        return true;
    }

    private static bool IsPreferred(ChainNode candidate, ChainNode current)
    {
        if (candidate.Block.Height != current.Block.Height)
        {
            return candidate.Block.Height > current.Block.Height;
        }

        return string.CompareOrdinal(candidate.Block.ComputeHeaderHashHex(), current.Block.ComputeHeaderHashHex()) < 0;
    }
}

public sealed record ChainNode(Block Block, State State);

public sealed record QuarantinedBlock(Block Block, string Reason);
