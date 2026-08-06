using System.Text.Json;
using System.Text.Json.Serialization;
using UbudKusCoin.Core.Hashing;

namespace UbudKusCoin.Core.Types;

/// <summary>
/// Versioned state-sync payload. The canonical JSON shape is stable because
/// accounts and stakes are sorted before serialization; the state root is
/// checked again when a payload is imported.
/// </summary>
public static class StateSnapshotCodec
{
    public const uint Format = 1;

    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static byte[] Encode(State state, Block? headBlock = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        var snapshot = new Snapshot
        {
            Format = Format,
            ChainId = state.ChainId,
            Height = state.Height,
            TimeStamp = state.TimeStamp,
            Head = state.Head.ToArray(),
            StateRoot = state.ComputeStateRoot(),
            HeadBlock = headBlock is null ? null : ToRecord(headBlock),
            Accounts = state.Accounts
                .OrderBy(x => x.Address.Encoded, StringComparer.Ordinal)
                .Select(x => new AccountRecord
                {
                    Address = x.Address.Encoded,
                    Balance = x.Balance.BaseUnits,
                    Nonce = x.Nonce,
                    PubKey = x.PubKey.ToArray()
                }).ToList(),
            Stakes = state.Stakes
                .OrderBy(x => x.Address.Encoded, StringComparer.Ordinal)
                .Select(x => new StakeRecord
                {
                    Address = x.Address.Encoded,
                    PubKey = x.PubKey.ToArray(),
                    ConsensusPubKey = x.ConsensusPubKey.ToArray(),
                    Amount = x.Amount.BaseUnits,
                    BondedHeight = x.BondedHeight,
                    UnlockHeight = x.UnlockHeight,
                    Jailed = x.Jailed
                }).ToList()
        };
        return JsonSerializer.SerializeToUtf8Bytes(snapshot, Options);
    }

    public static bool TryDecode(ReadOnlySpan<byte> encoded, out State? state, out string error)
    {
        var result = TryDecode(encoded, out state, out _, out error);
        return result;
    }

    public static bool TryDecode(
        ReadOnlySpan<byte> encoded,
        out State? state,
        out Block? headBlock,
        out string error)
    {
        state = null;
        headBlock = null;
        error = string.Empty;
        try
        {
            var snapshot = JsonSerializer.Deserialize<Snapshot>(encoded, Options)
                ?? throw new InvalidDataException("Snapshot payload is empty.");
            if (snapshot.Format != Format
                || snapshot.Head is null
                || snapshot.StateRoot is null
                || snapshot.Accounts is null
                || snapshot.Stakes is null
                || snapshot.Head.Length == 0
                || snapshot.StateRoot.Length == 0)
                throw new InvalidDataException("Unsupported or incomplete snapshot format.");

            var restored = new State(snapshot.ChainId, snapshot.Height, snapshot.Head, snapshot.TimeStamp);
            foreach (var account in snapshot.Accounts)
            {
                var restoredAccount = restored.EnsureAccount(Address.Parse(account.Address));
                restoredAccount.Balance = new Money(account.Balance);
                restoredAccount.Nonce = account.Nonce;
                restoredAccount.PubKey = account.PubKey.ToArray();
            }
            foreach (var stake in snapshot.Stakes)
            {
                restored.SetStake(new StakePositionState
                {
                    Address = Address.Parse(stake.Address),
                    PubKey = stake.PubKey.ToArray(),
                    ConsensusPubKey = stake.ConsensusPubKey.ToArray(),
                    Amount = new Money(stake.Amount),
                    BondedHeight = stake.BondedHeight,
                    UnlockHeight = stake.UnlockHeight,
                    Jailed = stake.Jailed
                });
            }

            if (!restored.ComputeStateRoot().SequenceEqual(snapshot.StateRoot))
                throw new InvalidDataException("Snapshot state root does not match its contents.");

            state = restored;
            headBlock = snapshot.HeadBlock is null ? null : FromRecord(snapshot.HeadBlock);
            if (headBlock is not null
                && (!headBlock.ComputeHeaderHash().SequenceEqual(snapshot.Head)
                    || headBlock.StateRoot.Length == 0
                    || !headBlock.StateRoot.SequenceEqual(snapshot.StateRoot)))
            {
                throw new InvalidDataException("Snapshot anchor block does not match the state.");
            }
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException
            or FormatException or ArgumentException or OverflowException)
        {
            error = $"Invalid state snapshot: {exception.Message}";
            return false;
        }
    }

    public static byte[] ComputeHash(ReadOnlySpan<byte> encoded)
        => HashUtils.Sha256(encoded);

    private sealed class Snapshot
    {
        public uint Format { get; set; }
        public uint ChainId { get; set; }
        public long Height { get; set; }
        public long TimeStamp { get; set; }
        public byte[] Head { get; set; } = Array.Empty<byte>();
        public byte[] StateRoot { get; set; } = Array.Empty<byte>();
        public BlockRecord? HeadBlock { get; set; }
        public List<AccountRecord> Accounts { get; set; } = new();
        public List<StakeRecord> Stakes { get; set; } = new();
    }

    private sealed class AccountRecord
    {
        public string Address { get; set; } = string.Empty;
        public long Balance { get; set; }
        public ulong Nonce { get; set; }
        public byte[] PubKey { get; set; } = Array.Empty<byte>();
    }

    private sealed class StakeRecord
    {
        public string Address { get; set; } = string.Empty;
        public byte[] PubKey { get; set; } = Array.Empty<byte>();
        public byte[] ConsensusPubKey { get; set; } = Array.Empty<byte>();
        public long Amount { get; set; }
        public long BondedHeight { get; set; }
        public long UnlockHeight { get; set; }
        public bool Jailed { get; set; }
    }

    private static BlockRecord ToRecord(Block block)
        => new()
        {
            Version = block.Version,
            ChainId = block.ChainId,
            Height = block.Height,
            TimeStamp = block.TimeStamp,
            PrevHash = block.PrevHash.ToArray(),
            MerkleRoot = block.MerkleRoot.ToArray(),
            StateRoot = block.StateRoot.ToArray(),
            Validator = block.Validator.Encoded,
            Reward = block.Reward.BaseUnits,
            Txs = block.Txs.Select(ToRecord).ToList(),
            Evidence = block.Evidence.Select(x => new EvidenceRecord
            {
                Kind = (uint)x.Kind,
                Validator = x.Validator.Encoded,
                Height = x.Height
            }).ToList()
        };

    private static Block FromRecord(BlockRecord block)
        => new()
        {
            Version = block.Version,
            ChainId = block.ChainId,
            Height = block.Height,
            TimeStamp = block.TimeStamp,
            PrevHash = block.PrevHash,
            MerkleRoot = block.MerkleRoot,
            StateRoot = block.StateRoot,
            Validator = Address.Parse(block.Validator),
            Reward = new Money(block.Reward),
            Txs = block.Txs.Select(FromRecord).ToList(),
            Evidence = block.Evidence.Select(x => new ConsensusEvidence(
                (ConsensusEvidenceKind)x.Kind, Address.Parse(x.Validator), x.Height)).ToList()
        };

    private static TransactionRecord ToRecord(Transaction transaction)
        => new()
        {
            Version = transaction.Version,
            ChainId = transaction.ChainId,
            Kind = (uint)transaction.Kind,
            Nonce = transaction.Nonce,
            From = transaction.From.Encoded,
            To = transaction.To.Encoded,
            Amount = transaction.Amount.BaseUnits,
            Fee = transaction.Fee.BaseUnits,
            LockPeriod = transaction.LockPeriod,
            ValidFrom = transaction.ValidFrom,
            ValidUntil = transaction.ValidUntil,
                    PubKey = transaction.PubKey.ToArray(),
                    ValidatorPubKey = transaction.ValidatorPubKey.ToArray(),
            Signature = transaction.Signature.ToArray()
        };

    private static Transaction FromRecord(TransactionRecord transaction)
        => new()
        {
            Version = transaction.Version,
            ChainId = transaction.ChainId,
            Kind = (TransactionKind)transaction.Kind,
            Nonce = transaction.Nonce,
            From = Address.Parse(transaction.From),
            To = Address.Parse(transaction.To),
            Amount = new Money(transaction.Amount),
            Fee = new Money(transaction.Fee),
            LockPeriod = transaction.LockPeriod,
            ValidFrom = transaction.ValidFrom,
            ValidUntil = transaction.ValidUntil,
            PubKey = transaction.PubKey,
            ValidatorPubKey = transaction.ValidatorPubKey,
            Signature = transaction.Signature
        };

    private sealed class BlockRecord
    {
        public uint Version { get; set; }
        public uint ChainId { get; set; }
        public long Height { get; set; }
        public long TimeStamp { get; set; }
        public byte[] PrevHash { get; set; } = Array.Empty<byte>();
        public byte[] MerkleRoot { get; set; } = Array.Empty<byte>();
        public byte[] StateRoot { get; set; } = Array.Empty<byte>();
        public string Validator { get; set; } = string.Empty;
        public long Reward { get; set; }
        public List<TransactionRecord> Txs { get; set; } = new();
        public List<EvidenceRecord> Evidence { get; set; } = new();
    }

    private sealed class TransactionRecord
    {
        public uint Version { get; set; }
        public uint ChainId { get; set; }
        public uint Kind { get; set; }
        public ulong Nonce { get; set; }
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public long Amount { get; set; }
        public long Fee { get; set; }
        public long LockPeriod { get; set; }
        public long ValidFrom { get; set; }
        public long ValidUntil { get; set; }
        public byte[] PubKey { get; set; } = Array.Empty<byte>();
        public byte[] ValidatorPubKey { get; set; } = Array.Empty<byte>();
        public byte[] Signature { get; set; } = Array.Empty<byte>();
    }

    private sealed class EvidenceRecord
    {
        public uint Kind { get; set; }
        public string Validator { get; set; } = string.Empty;
        public long Height { get; set; }
    }
}
