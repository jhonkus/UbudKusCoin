using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Grpc;
using CoreBlock = UbudKusCoin.Core.Types.Block;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;

namespace UbudKusCoin.Services;

public sealed class CanonicalNodeService
{
    private readonly object writeLock = new();
    private readonly CanonicalChainStore store;
    private readonly FinalityStore finalityStore;
    private readonly ValidatorSet validatorSet;
    private readonly Dictionary<string, DeterministicBftDriver> voteDrivers = new(StringComparer.Ordinal);
    private readonly FinalityTracker finality = new();
    private CanonicalChain chain;

    public CanonicalNodeService(uint chainId, string snapshotPath, ValidatorSet validatorSet = null,
        GenesisManifest genesisManifest = null)
    {
        store = new CanonicalChainStore(snapshotPath, genesisManifest);
        finalityStore = new FinalityStore(snapshotPath + ".finality");
        this.validatorSet = validatorSet;
        chain = File.Exists(snapshotPath) ? store.Load() : new CanonicalChain(chainId, genesisManifest);
        if (!File.Exists(snapshotPath))
        {
            store.Save(chain);
        }

        var persistedFinality = finalityStore.Load();
        if (persistedFinality is null)
        {
            var genesis = chain.Candidates.Single(x => x.Block.Height == 1);
            var genesisHash = genesis.Block.ComputeHeaderHashHex();
            finality.Restore(1, genesisHash, chain, out _);
            finalityStore.Save(1, genesisHash);
        }
        else if (!finality.Restore(persistedFinality.Value.Height, persistedFinality.Value.Hash, chain, out var finalityError))
        {
            throw new InvalidDataException(finalityError);
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

    public FinalityTracker Finality => finality;

    public bool RestoreSnapshot(State state, CoreBlock head, out string error)
    {
        lock (writeLock)
        {
            if (!CanonicalChain.TryRestoreSnapshot(head, state, out var restored, out error))
                return false;

            chain = restored!;
            store.Save(chain);
            if (!finality.Restore(head.Height, head.ComputeHeaderHashHex(), chain, out error))
                return false;
            finalityStore.Save(finality.FinalizedHeight, finality.FinalizedHash);
            return true;
        }
    }

    public (bool Accepted, string Message) Add(CanonicalBlock request)
    {
        try
        {
            return Add(FromGrpc(request));
        }
        catch (Exception exception)
        {
            return (false, $"Malformed canonical block: {exception.Message}");
        }
    }

    public (bool Accepted, string Message) Add(CoreBlock block)
    {
        lock (writeLock)
        {
            if (validatorSet is not null)
            {
                var driver = new DeterministicBftDriver(chain.State, validatorSet);
                if (!driver.ValidateProposal(block, 0, out var consensusError))
                {
                    chain.AddQuarantine(block, consensusError);
                    return (false, consensusError);
                }
            }

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

    public IReadOnlyList<CoreBlock> GetRange(long startHeight)
    {
        lock (writeLock)
        {
            return chain.GetCanonicalBlocks(startHeight);
        }
    }

    public bool IsExternalCommitReplay(
        IEnumerable<CoreTransaction> transactions,
        long externalHeight,
        Address validator,
        IEnumerable<ConsensusEvidence> evidence = null)
    {
        lock (writeLock)
        {
            var expectedHeight = checked(externalHeight + 1);
            var list = transactions.ToList();
            var evidenceList = evidence?.ToList() ?? new List<ConsensusEvidence>();
            return chain.State.Height == expectedHeight
                && chain.Head.Block.Validator.Encoded == validator.Encoded
                && chain.Head.Block.Txs.Select(x => x.ComputeIdHex())
                    .SequenceEqual(list.Select(x => x.ComputeIdHex()), StringComparer.Ordinal)
                && chain.Head.Block.Evidence.SequenceEqual(evidenceList);
        }
    }

    public (bool Accepted, CoreBlock Block, string Message) AcceptExternalCommit(
        IEnumerable<CoreTransaction> transactions,
        long timeStamp,
        Address validator,
        long? externalHeight = null,
        IEnumerable<ConsensusEvidence> evidence = null)
    {
        lock (writeLock)
        {
            var previousFinalityHeight = finality.FinalizedHeight;
            var previousFinalityHash = finality.FinalizedHash;
            var list = transactions.ToList();
            var evidenceList = evidence?.ToList() ?? new List<ConsensusEvidence>();
            var expectedHeight = externalHeight is null
                ? chain.State.Height + 1
                : checked(externalHeight.Value + 1);

            if (chain.State.Height == expectedHeight)
            {
                var sameTransactions = chain.Head.Block.Txs.Count == list.Count
                    && chain.Head.Block.Txs.Select(x => x.ComputeIdHex())
                        .SequenceEqual(list.Select(x => x.ComputeIdHex()), StringComparer.Ordinal);
                if (sameTransactions && chain.Head.Block.Validator.Encoded == validator.Encoded)
                {
                    return (true, chain.Head.Block, "External commit was already accepted.");
                }

                return (false, chain.Head.Block, "External commit replay does not match the canonical block.");
            }

            if (expectedHeight != chain.State.Height + 1)
            {
                return (false, new CoreBlock(), "External commit height is not sequential.");
            }

            var block = new CoreBlock
            {
                ChainId = chain.State.ChainId,
                Height = expectedHeight,
                TimeStamp = Math.Max(timeStamp, chain.State.TimeStamp + 1),
                PrevHash = chain.State.Head,
                MerkleRoot = Merkle.ComputeRoot(list.Select(x => x.ComputeId()).ToArray()),
                Validator = validator,
                Reward = Money.Zero,
                Txs = list,
                Evidence = evidenceList
            };
            var resulting = StateTransition.ComputeResultingState(chain.State, block);
            if (!resulting.Success)
            {
                return (false, block, resulting.Error ?? "External commit rejected.");
            }

            block.StateRoot = resulting.NewState!.ComputeStateRoot();
            if (!chain.TryAcceptCommitted(block, out var error))
            {
                return (false, block, error);
            }

            try
            {
                store.Save(chain);
                if (!finality.TryFinalizeExternal(block, out var finalityError))
                {
                    chain = store.Load();
                    return (false, block, finalityError);
                }

                finalityStore.Save(finality.FinalizedHeight, finality.FinalizedHash);
                return (true, block, "External commit accepted and persisted.");
            }
            catch (Exception exception)
            {
                chain = store.Load();
                var persistedFinality = finalityStore.Load();
                if (persistedFinality is not null)
                {
                    finality.Restore(persistedFinality.Value.Height, persistedFinality.Value.Hash, chain, out _);
                }
                else
                {
                    finality.Restore(previousFinalityHeight, previousFinalityHash, chain, out _);
                }
                return (false, block, $"Persistence failed; state restored: {exception.Message}");
            }
        }
    }

    public (bool Accepted, bool Finalized, string Message) SubmitVote(CanonicalVote request)
    {
        try
        {
            if (validatorSet is null)
            {
                return (false, false, "Consensus validator set is not configured");
            }

            var vote = FromGrpc(request);
            var key = $"{vote.Height}:{vote.Round}";
            if (!voteDrivers.TryGetValue(key, out var driver))
            {
                driver = new DeterministicBftDriver(chain.State, validatorSet);
                voteDrivers[key] = driver;
            }

            if (!driver.AddVote(vote, out var certificate, out var error))
            {
                return (false, false, error);
            }

            if (certificate is null)
            {
                return (true, false, "Vote accepted; quorum pending");
            }

            var candidate = chain.Candidates.FirstOrDefault(x => x.Block.Height == vote.Height
                && x.Block.ComputeHeaderHashHex() == vote.BlockHash);
            if (candidate is null || !chain.IsCanonical(candidate.Block)
                || !finality.TryFinalize(candidate.Block, certificate, validatorSet, out error))
            {
                return (false, false, error ?? "Block candidate not found");
            }

            finalityStore.Save(finality.FinalizedHeight, finality.FinalizedHash);
            return (true, true, "Vote accepted and block finalized");
        }
        catch (Exception exception)
        {
            return (false, false, $"Malformed consensus vote: {exception.Message}");
        }
    }

    public CanonicalVote CreateVote(CoreBlock block, WalletService wallet, uint round = 0)
    {
        var pubKey = wallet.GetPublicKey().PubKey.ToBytes();
        var vote = new ConsensusVote
        {
            ChainId = block.ChainId,
            Height = block.Height,
            Round = round,
            BlockHash = block.ComputeHeaderHashHex(),
            Validator = Address.FromPublicKey(ChainInfo.AddressVersion(block.ChainId), pubKey),
            PubKey = pubKey
        };
        vote.Signature = wallet.GetKeyPair().PrivateKey.PrivateKey
            .Sign(new NBitcoin.uint256(vote.ComputeDigest())).ToDER();
        return ToGrpc(vote);
    }

    public (bool Accepted, string Message, CoreBlock Block) CreateAndCommitBlock(WalletService wallet)
    {
        lock (writeLock)
        {
            var key = wallet.GetKeyPair().PrivateKey.PrivateKey;
            var validatorKey = wallet.GetKeyPair().PublicKey.PubKey.ToBytes();
            var block = new CoreBlock
            {
                ChainId = chain.State.ChainId,
                Height = chain.State.Height + 1,
                TimeStamp = Math.Max(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), chain.State.TimeStamp + 1),
                PrevHash = chain.State.Head,
                MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>()),
                Validator = Address.FromPublicKey(ChainInfo.AddressVersion(chain.State.ChainId), validatorKey),
                Reward = Money.Zero,
                ValidatorPubKey = validatorKey,
                Txs = new()
            };
            if (validatorSet is not null
                && validatorSet.SelectProposer(block.ChainId, block.Height, 0).Address.Encoded != block.Validator.Encoded)
            {
                return (false, "This node is not the selected proposer for the current height", block);
            }
            var resulting = StateTransition.ComputeResultingState(chain.State, block);
            if (!resulting.Success)
            {
                return (false, resulting.Error ?? "Unable to build block", block);
            }

            block.StateRoot = resulting.NewState!.ComputeStateRoot();
            block.ValidatorSignature = key.Sign(new NBitcoin.uint256(block.ComputeHeaderHash())).ToDER();
            var result = Add(block);
            return (result.Accepted, result.Message, block);
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
        result.Evidence.AddRange(block.Evidence.Select(x => new CanonicalEvidence
        {
            Kind = (uint)x.Kind,
            Validator = x.Validator.Encoded,
            Height = x.Height
        }));
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
            Txs = request.Transactions.Select(FromGrpc).ToList(),
            Evidence = request.Evidence.Select(x => new ConsensusEvidence(
                (ConsensusEvidenceKind)x.Kind, Address.Parse(x.Validator), x.Height)).ToList()
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
            Kind = (uint)transaction.Kind,
            LockPeriod = transaction.LockPeriod,
            PubKey = Google.Protobuf.ByteString.CopyFrom(transaction.PubKey),
            ValidatorPubKey = Google.Protobuf.ByteString.CopyFrom(transaction.ValidatorPubKey),
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
            Kind = (TransactionKind)transaction.Kind,
            LockPeriod = transaction.LockPeriod,
            PubKey = transaction.PubKey.ToByteArray(),
            ValidatorPubKey = transaction.ValidatorPubKey.ToByteArray(),
            Signature = transaction.Signature.ToByteArray()
        };

    private static ConsensusVote FromGrpc(CanonicalVote vote)
        => new()
        {
            ChainId = vote.ChainId,
            Height = vote.Height,
            Round = vote.Round,
            BlockHash = vote.BlockHash,
            Validator = Address.Parse(vote.Validator),
            PubKey = vote.PubKey.ToByteArray(),
            Signature = vote.Signature.ToByteArray()
        };

    private static CanonicalVote ToGrpc(ConsensusVote vote)
        => new()
        {
            ChainId = vote.ChainId,
            Height = vote.Height,
            Round = vote.Round,
            BlockHash = vote.BlockHash,
            Validator = vote.Validator.Encoded,
            PubKey = Google.Protobuf.ByteString.CopyFrom(vote.PubKey),
            Signature = Google.Protobuf.ByteString.CopyFrom(vote.Signature)
        };
}
