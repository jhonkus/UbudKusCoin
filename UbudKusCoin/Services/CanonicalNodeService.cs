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
    private readonly ValidatorSet validatorSet;
    private CanonicalChain chain;

    public CanonicalNodeService(uint chainId, string snapshotPath, ValidatorSet validatorSet = null)
    {
        store = new CanonicalChainStore(snapshotPath);
        this.validatorSet = validatorSet;
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
        => Add(FromGrpc(request));

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
