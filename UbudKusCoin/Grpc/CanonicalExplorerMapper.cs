using System;
using System.Linq;
using System.Text.Json;
using CoreBlock = UbudKusCoin.Core.Types.Block;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;

namespace UbudKusCoin.Grpc;

/// <summary>
/// Keeps the legacy read-only gRPC facade compatible with the canonical chain.
/// Older explorer clients can continue using the established response shape
/// without reading the retired LiteDB chain.
/// </summary>
internal static class CanonicalExplorerMapper
{
    public static Block ToBlock(CoreBlock block)
    {
        var transactions = block.Txs
            .Select(transaction => ToTransaction(transaction, block.Height, block.TimeStamp))
            .ToArray();

        return new Block
        {
            Version = checked((int)block.Version),
            Height = block.Height,
            TimeStamp = block.TimeStamp,
            PrevHash = Hex(block.PrevHash),
            Hash = block.ComputeHeaderHashHex(),
            Transactions = JsonSerializer.Serialize(transactions),
            Validator = block.Validator.Encoded,
            ValidatorBalance = 0,
            MerkleRoot = Hex(block.MerkleRoot),
            NumOfTx = transactions.Length,
            TotalAmount = block.Txs.Sum(transaction => transaction.Amount.BaseUnits),
            TotalReward = block.Reward.BaseUnits,
            Difficulty = 0,
            Nonce = 0,
            Size = 0,
            BuildTime = 0,
            Signature = Convert.ToBase64String(block.ValidatorSignature)
        };
    }

    public static Transaction ToTransaction(CoreTransaction transaction, long height, long timeStamp)
        => new()
        {
            Hash = transaction.ComputeIdHex(),
            TimeStamp = timeStamp,
            Sender = transaction.From.Encoded,
            Recipient = transaction.To.Encoded,
            Amount = transaction.Amount.BaseUnits,
            Fee = transaction.Fee.BaseUnits,
            Height = height,
            Signature = Convert.ToBase64String(transaction.Signature),
            PubKey = Hex(transaction.PubKey),
            TxType = transaction.Kind.ToString()
        };

    private static string Hex(byte[] value)
        => Convert.ToHexString(value ?? Array.Empty<byte>()).ToLowerInvariant();
}
