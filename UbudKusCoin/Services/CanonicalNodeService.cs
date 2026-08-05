using System;
using System.IO;
using System.Linq;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Grpc;
using CoreBlock = UbudKusCoin.Core.Types.Block;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;

namespace UbudKusCoin.Services;

public sealed class CanonicalNodeService
{
    private readonly object writeLock = new();
    private readonly CanonicalChainStore store;
    private CanonicalChain chain;

    public CanonicalNodeService(uint chainId, string snapshotPath)
    {
        store = new CanonicalChainStore(snapshotPath);
        chain = File.Exists(snapshotPath) ? store.Load() : new CanonicalChain(chainId);
        if (!File.Exists(snapshotPath))
        {
            store.Save(chain);
        }
    }

    public CanonicalChain Chain
    {
        get
        {
            lock (writeLock)
            {
                return chain;
            }
        }
    }

    public (bool Accepted, string Message) Add(CanonicalBlock request)
    {
        lock (writeLock)
        {
            var block = FromGrpc(request);
            if (!chain.TryAccept(block, out var error))
            {
                return (false, error);
            }

            try
            {
                store.Save(chain);
                return (true, "Canonical block accepted");
            }
            catch (Exception exception)
            {
                chain = store.Load();
                return (false, $"Persistence failed; state restored: {exception.Message}");
            }
        }
    }

    public static CanonicalBlock ToGrpc(CoreBlock block)
    {
        var result = new CanonicalBlock
        {
            Version = block.Version,
            ChainId = block.ChainId,
            Height = block.Height,
            TimeStamp = block.TimeStamp,
            PrevHash = Google.Protobuf.ByteString.CopyFrom(block.PrevHash),
            MerkleRoot = Google.Protobuf.ByteString.CopyFrom(block.MerkleRoot),
            StateRoot = Google.Protobuf.ByteString.CopyFrom(block.StateRoot),
            Validator = block.Validator.Encoded,
            Reward = block.Reward.BaseUnits,
            ValidatorPubKey = Google.Protobuf.ByteString.CopyFrom(block.ValidatorPubKey),
            ValidatorSignature = Google.Protobuf.ByteString.CopyFrom(block.ValidatorSignature)
        };
        result.Transactions.AddRange(block.Txs.Select(ToGrpc));
        return result;
    }

    private static CoreBlock FromGrpc(CanonicalBlock request)
    {
        return new CoreBlock
        {
            Version = request.Version,
            ChainId = request.ChainId,
            Height = request.Height,
            TimeStamp = request.TimeStamp,
            PrevHash = request.PrevHash.ToByteArray(),
            MerkleRoot = request.MerkleRoot.ToByteArray(),
            StateRoot = request.StateRoot.ToByteArray(),
            Validator = Address.Parse(request.Validator),
            Reward = new Money(request.Reward),
            ValidatorPubKey = request.ValidatorPubKey.ToByteArray(),
            ValidatorSignature = request.ValidatorSignature.ToByteArray(),
            Txs = request.Transactions.Select(FromGrpc).ToList()
        };
    }

    private static CanonicalTransaction ToGrpc(CoreTransaction transaction)
        => new()
        {
            Version = transaction.Version,
            ChainId = transaction.ChainId,
            Nonce = transaction.Nonce,
            From = transaction.From.Encoded,
            To = transaction.To.Encoded,
            Amount = transaction.Amount.BaseUnits,
            Fee = transaction.Fee.BaseUnits,
            ValidFrom = transaction.ValidFrom,
            ValidUntil = transaction.ValidUntil,
            PubKey = Google.Protobuf.ByteString.CopyFrom(transaction.PubKey),
            Signature = Google.Protobuf.ByteString.CopyFrom(transaction.Signature)
        };

    private static CoreTransaction FromGrpc(CanonicalTransaction transaction)
        => new CoreTransaction
        {
            Version = transaction.Version,
            ChainId = transaction.ChainId,
            Nonce = transaction.Nonce,
            From = Address.Parse(transaction.From),
            To = Address.Parse(transaction.To),
            Amount = new Money(transaction.Amount),
            Fee = new Money(transaction.Fee),
            ValidFrom = transaction.ValidFrom,
            ValidUntil = transaction.ValidUntil,
            PubKey = transaction.PubKey.ToByteArray(),
            Signature = transaction.Signature.ToByteArray()
        };
}
