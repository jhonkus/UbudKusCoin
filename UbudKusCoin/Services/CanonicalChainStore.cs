using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Services;

/// <summary>
/// Crash-safe snapshot store for the canonical Core chain. The snapshot is
/// rebuilt through StateTransition on load; persisted data is never trusted.
/// </summary>
public sealed class CanonicalChainStore
{
    private readonly string path;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public CanonicalChainStore(string path)
    {
        this.path = path;
    }

    public void Save(CanonicalChain chain)
    {
        var snapshot = new ChainSnapshot
        {
            ChainId = chain.State.ChainId,
            Blocks = chain.Candidates
                .OrderBy(x => x.Block.Height)
                .ThenBy(x => x.Block.ComputeHeaderHashHex(), StringComparer.Ordinal)
                .Select(ToRecord)
                .ToList()
        };

        var directory = Path.GetDirectoryName(Path.GetFullPath(path))!;
        Directory.CreateDirectory(directory);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(snapshot, jsonOptions));
        File.Move(temporary, path, true);
    }

    public CanonicalChain Load()
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Canonical chain snapshot was not found.", path);
        }

        var snapshot = JsonSerializer.Deserialize<ChainSnapshot>(File.ReadAllText(path), jsonOptions)
            ?? throw new InvalidDataException("Canonical chain snapshot is empty.");
        var chain = new CanonicalChain(snapshot.ChainId);
        foreach (var record in snapshot.Blocks
                     .Where(x => x.Height > 1)
                     .OrderBy(x => x.Height)
                     .ThenBy(x => Convert.ToHexString(x.PrevHash), StringComparer.Ordinal))
        {
            var block = FromRecord(record);
            var accepted = block.ValidatorSignature.Length == 0
                ? chain.TryAcceptCommitted(block, out var error)
                : chain.TryAccept(block, out error);
            if (!accepted)
            {
                throw new InvalidDataException($"Canonical snapshot contains an invalid block: {error}");
            }
        }

        return chain;
    }

    private static BlockRecord ToRecord(ChainNode node)
    {
        var block = node.Block;
        return new BlockRecord
        {
            Version = block.Version,
            ChainId = block.ChainId,
            Height = block.Height,
            TimeStamp = block.TimeStamp,
            PrevHash = block.PrevHash,
            MerkleRoot = block.MerkleRoot,
            StateRoot = block.StateRoot,
            Validator = block.Validator.Encoded,
            Reward = block.Reward.BaseUnits,
            ValidatorPubKey = block.ValidatorPubKey,
            ValidatorSignature = block.ValidatorSignature,
            Transactions = block.Txs.Select(ToRecord).ToList(),
            Evidence = block.Evidence.Select(ToRecord).ToList()
        };
    }

    private static TransactionRecord ToRecord(Transaction tx)
        => new()
        {
            Version = tx.Version,
            ChainId = tx.ChainId,
            Nonce = tx.Nonce,
            From = tx.From.Encoded,
            To = tx.To.Encoded,
            Amount = tx.Amount.BaseUnits,
            Fee = tx.Fee.BaseUnits,
            ValidFrom = tx.ValidFrom,
            ValidUntil = tx.ValidUntil,
            Kind = (uint)tx.Kind,
            LockPeriod = tx.LockPeriod,
            PubKey = tx.PubKey,
            Signature = tx.Signature
        };

    private static Block FromRecord(BlockRecord record)
    {
        var block = new Block
        {
            Version = record.Version,
            ChainId = record.ChainId,
            Height = record.Height,
            TimeStamp = record.TimeStamp,
            PrevHash = record.PrevHash,
            MerkleRoot = record.MerkleRoot,
            StateRoot = record.StateRoot,
            Validator = Address.Parse(record.Validator),
            Reward = new Money(record.Reward),
            ValidatorPubKey = record.ValidatorPubKey,
            ValidatorSignature = record.ValidatorSignature,
            Txs = record.Transactions.Select(FromRecord).ToList(),
            Evidence = record.Evidence.Select(FromRecord).ToList()
        };
        return block;
    }

    private static Transaction FromRecord(TransactionRecord record)
        => new()
        {
            Version = record.Version,
            ChainId = record.ChainId,
            Nonce = record.Nonce,
            From = Address.Parse(record.From),
            To = Address.Parse(record.To),
            Amount = new Money(record.Amount),
            Fee = new Money(record.Fee),
            ValidFrom = record.ValidFrom,
            ValidUntil = record.ValidUntil,
            Kind = (TransactionKind)record.Kind,
            LockPeriod = record.LockPeriod,
            PubKey = record.PubKey,
            Signature = record.Signature
        };

    private static EvidenceRecord ToRecord(ConsensusEvidence evidence)
        => new()
        {
            Kind = (uint)evidence.Kind,
            Validator = evidence.Validator.Encoded,
            Height = evidence.Height
        };

    private static ConsensusEvidence FromRecord(EvidenceRecord record)
        => new((ConsensusEvidenceKind)record.Kind, Address.Parse(record.Validator), record.Height);

    private sealed class ChainSnapshot
    {
        public uint ChainId { get; set; }
        public List<BlockRecord> Blocks { get; set; } = new();
    }

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
        public byte[] ValidatorPubKey { get; set; } = Array.Empty<byte>();
        public byte[] ValidatorSignature { get; set; } = Array.Empty<byte>();
        public List<TransactionRecord> Transactions { get; set; } = new();
        public List<EvidenceRecord> Evidence { get; set; } = new();
    }

    private sealed class TransactionRecord
    {
        public uint Version { get; set; }
        public uint ChainId { get; set; }
        public ulong Nonce { get; set; }
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public long Amount { get; set; }
        public long Fee { get; set; }
        public long ValidFrom { get; set; }
        public long ValidUntil { get; set; }
        public uint Kind { get; set; }
        public long LockPeriod { get; set; }
        public byte[] PubKey { get; set; } = Array.Empty<byte>();
        public byte[] Signature { get; set; } = Array.Empty<byte>();
    }

    private sealed class EvidenceRecord
    {
        public uint Kind { get; set; }
        public string Validator { get; set; } = string.Empty;
        public long Height { get; set; }
    }
}
