using System;
using System.Collections.Generic;
using System.Linq;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Grpc;
using CoreBlock = UbudKusCoin.Core.Types.Block;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;

namespace UbudKusCoin.Services;

public static class CanonicalSyncVerifier
{
    public static bool TryValidatePeerHead(
        CanonicalBlock peerHead,
        long localHeight,
        string localHeadHash,
        string peerHeadHash,
        out string error)
    {
        error = string.Empty;
        if (peerHead is null)
        {
            error = "Peer head is missing.";
            return false;
        }

        if (!string.Equals(ComputeBlockHash(peerHead), peerHeadHash, StringComparison.OrdinalIgnoreCase))
        {
            error = "Peer head hash does not match the computed canonical block.";
            return false;
        }

        if (peerHead.Height < localHeight)
        {
            error = "Peer head is behind the local canonical chain.";
            return false;
        }

        if (peerHead.Height == localHeight
            && !string.Equals(peerHeadHash, localHeadHash, StringComparison.OrdinalIgnoreCase))
        {
            error = "Peer head height matches the local chain but the hash diverges.";
            return false;
        }

        return true;
    }

    public static bool TryValidateCanonicalRange(
        IReadOnlyList<CanonicalBlock> blocks,
        long startHeight,
        string expectedParentHash,
        string expectedHeadHash,
        out string error)
    {
        error = string.Empty;
        if (blocks is null || blocks.Count == 0)
        {
            error = "Peer did not return any canonical blocks.";
            return false;
        }

        var expectedHeight = startHeight + 1;
        var previousHash = expectedParentHash;
        foreach (var block in blocks)
        {
            if (block.Height != expectedHeight)
            {
                error = $"Expected canonical block height {expectedHeight} but received {block.Height}.";
                return false;
            }

            var blockParentHash = Convert.ToHexStringLower(block.PrevHash.ToByteArray());
            if (!string.Equals(previousHash, blockParentHash, StringComparison.OrdinalIgnoreCase))
            {
                error = $"Canonical block {block.Height} does not link to the previous hash.";
                return false;
            }

            var computedHash = ComputeBlockHash(block);
            previousHash = computedHash;
            expectedHeight++;
        }

        if (!string.Equals(previousHash, expectedHeadHash, StringComparison.OrdinalIgnoreCase))
        {
            error = "Canonical range does not converge to the expected peer head hash.";
            return false;
        }

        return true;
    }

    public static string ComputeBlockHash(CanonicalBlock block)
    {
        ArgumentNullException.ThrowIfNull(block);
        return BuildCoreBlock(block).ComputeHeaderHashHex();
    }

    private static CoreBlock BuildCoreBlock(CanonicalBlock request)
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
            Txs = request.Transactions.Select(FromGrpc).ToList(),
            Evidence = request.Evidence.Select(x => new ConsensusEvidence(
                (ConsensusEvidenceKind)x.Kind, Address.Parse(x.Validator), x.Height)).ToList()
        };
    }

    private static CoreTransaction FromGrpc(CanonicalTransaction transaction)
        => new()
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
            Kind = (TransactionKind)transaction.Kind,
            LockPeriod = transaction.LockPeriod,
            PubKey = transaction.PubKey.ToByteArray(),
            ValidatorPubKey = transaction.ValidatorPubKey.ToByteArray(),
            Signature = transaction.Signature.ToByteArray()
        };
}
