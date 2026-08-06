#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UbudKusCoin.CometBft.Abci;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Services;
using CoreBlock = UbudKusCoin.Core.Types.Block;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;

namespace UbudKusCoin.Grpc;

public sealed class AbciServiceImpl : ABCI.ABCIBase
{
    private const uint Success = 0;
    private const uint InvalidTransaction = 1;

    private static ConsensusApplicationStateMachine Application
        => ServicePool.ApplicationStateMachine
            ?? throw new RpcException(new Status(StatusCode.FailedPrecondition, "ABCI application is not initialized."));

    public override Task<ResponseEcho> Echo(RequestEcho request, ServerCallContext context)
        => Task.FromResult(new ResponseEcho { Message = request.Message });

    public override Task<ResponseFlush> Flush(RequestFlush request, ServerCallContext context)
        => Task.FromResult(new ResponseFlush());

    public override Task<ResponseInfo> Info(RequestInfo request, ServerCallContext context)
    {
        var state = Application.State;
        return Task.FromResult(new ResponseInfo
        {
            Version = "UbudKusCoin",
            LastBlockHeight = AbciHeight(state.Height),
            LastBlockAppHash = ByteString.CopyFrom(state.ComputeStateRoot())
        });
    }

    public override Task<ResponseCheckTx> CheckTx(RequestCheckTx request, ServerCallContext context)
    {
        if (!TransactionCodec.TryDecode(request.Tx.Span, out var transaction, out var decodeError))
        {
            return Task.FromResult(new ResponseCheckTx { Code = InvalidTransaction, Log = decodeError });
        }

        var result = Application.CheckTx(transaction!);
        return Task.FromResult(new ResponseCheckTx
        {
            Code = result.Accepted ? Success : InvalidTransaction,
            Log = result.Message,
            Data = ByteString.CopyFrom(transaction!.ComputeId())
        });
    }

    public override Task<ResponseQuery> Query(RequestQuery request, ServerCallContext context)
    {
        if (request.Path == "/app_hash")
        {
            return Task.FromResult(new ResponseQuery
            {
                Value = ByteString.CopyFrom(Application.State.ComputeStateRoot()),
                Height = AbciHeight(Application.State.Height)
            });
        }

        return Task.FromResult(new ResponseQuery { Code = InvalidTransaction, Log = "Unknown query path." });
    }

    public override Task<ResponsePrepareProposal> PrepareProposal(RequestPrepareProposal request, ServerCallContext context)
    {
        var transactions = DecodeTransactions(request.Txs, out _);
        var maxBytes = request.MaxTxBytes > int.MaxValue ? int.MaxValue : (int)request.MaxTxBytes;
        var result = Application.PrepareProposal(transactions, maxBytes);
        var response = new ResponsePrepareProposal();
        response.Txs.AddRange(result.Transactions.Select(x => ByteString.CopyFrom(TransactionCodec.Encode(x))));
        return Task.FromResult(response);
    }

    public override Task<ResponseProcessProposal> ProcessProposal(RequestProcessProposal request, ServerCallContext context)
    {
        var transactions = DecodeTransactions(request.Txs, out var decodeError);
        var result = decodeError is null
            ? Application.ProcessProposal(transactions, TimestampSeconds(request.Time))
            : new ApplicationCheckResult(false, decodeError);
        return Task.FromResult(new ResponseProcessProposal
        {
            Status = result.Accepted
                ? ResponseProcessProposal.Types.ProposalStatus.Accept
                : ResponseProcessProposal.Types.ProposalStatus.Reject
        });
    }

    public override Task<ResponseFinalizeBlock> FinalizeBlock(RequestFinalizeBlock request, ServerCallContext context)
    {
        var proposer = ResolveProposer(request.Height, request.ProposerAddress);
        if (proposer is null)
        {
            return Task.FromResult(new ResponseFinalizeBlock
            {
                TxResults = { new ExecTxResult { Code = InvalidTransaction, Log = "Invalid CometBFT chain, height, or proposer." } }
            });
        }

        var transactions = DecodeTransactions(request.Txs, out var decodeError);
        var validation = decodeError is null
            ? Application.ProcessProposal(transactions, TimestampSeconds(request.Time))
            : new ApplicationCheckResult(false, decodeError);
        var commit = validation.Accepted
            ? ServicePool.CanonicalNodeService.AcceptExternalCommit(
                transactions,
                TimestampSeconds(request.Time),
                proposer.Value)
            : (false, new CoreBlock(), validation.Message);
        if (commit.Item1)
        {
            Application.Synchronize(ServicePool.CanonicalNodeService.Chain.State);
        }

        var appHash = commit.Item1
            ? ServicePool.CanonicalNodeService.Chain.State.ComputeStateRoot()
            : Array.Empty<byte>();
        var response = new ResponseFinalizeBlock { AppHash = ByteString.CopyFrom(appHash) };
        response.TxResults.AddRange(transactions.Select(_ => new ExecTxResult
        {
            Code = commit.Item1 ? Success : InvalidTransaction,
            Log = commit.Item3
        }));
        if (commit.Item1)
        {
            response.ValidatorUpdates.AddRange(BuildValidatorUpdates(Application.State));
        }
        return Task.FromResult(response);
    }

    public override Task<ResponseCommit> Commit(RequestCommit request, ServerCallContext context)
        => Task.FromResult(new ResponseCommit());

    public override Task<ResponseInitChain> InitChain(RequestInitChain request, ServerCallContext context)
    {
        var expectedChainId = DotNetEnv.Env.GetString(
            "COMETBFT_CHAIN_ID",
            $"ukc-{Application.State.ChainId}");
        if (!string.Equals(request.ChainId, expectedChainId, StringComparison.Ordinal))
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"Unexpected CometBFT chain ID '{request.ChainId}'. Expected '{expectedChainId}'."));
        }

        var response = new ResponseInitChain
        {
            AppHash = ByteString.CopyFrom(Application.State.ComputeStateRoot())
        };
        var publicKey = CometBftValidatorKeyLoader.TryLoadPublicKey();
        if (request.Validators.Count == 0 && publicKey.Length > 0)
        {
            response.Validators.Add(new ValidatorUpdate
            {
                PubKey = new PublicKey { Ed25519 = ByteString.CopyFrom(publicKey) },
                Power = 10
            });
        }

        return Task.FromResult(response);
    }

    public override Task<ResponseListSnapshots> ListSnapshots(RequestListSnapshots request, ServerCallContext context)
        => Task.FromResult(new ResponseListSnapshots());

    public override Task<ResponseOfferSnapshot> OfferSnapshot(RequestOfferSnapshot request, ServerCallContext context)
        => Task.FromResult(new ResponseOfferSnapshot { Result = 3 });

    public override Task<ResponseLoadSnapshotChunk> LoadSnapshotChunk(RequestLoadSnapshotChunk request, ServerCallContext context)
        => Task.FromResult(new ResponseLoadSnapshotChunk());

    public override Task<ResponseApplySnapshotChunk> ApplySnapshotChunk(RequestApplySnapshotChunk request, ServerCallContext context)
        => Task.FromResult(new ResponseApplySnapshotChunk { Result = 2 });

    public override Task<ResponseExtendVote> ExtendVote(RequestExtendVote request, ServerCallContext context)
        => Task.FromResult(new ResponseExtendVote());

    public override Task<ResponseVerifyVoteExtension> VerifyVoteExtension(RequestVerifyVoteExtension request, ServerCallContext context)
        => Task.FromResult(new ResponseVerifyVoteExtension
        {
            Status = ResponseVerifyVoteExtension.Types.VerifyStatus.Reject
        });

    private static List<CoreTransaction> DecodeTransactions(IEnumerable<ByteString> encoded, out string? error)
    {
        var result = new List<CoreTransaction>();
        foreach (var bytes in encoded)
        {
            if (!TransactionCodec.TryDecode(bytes.Span, out var transaction, out error))
            {
                return result;
            }

            result.Add(transaction!);
        }

        error = null;
        return result;
    }

    // The application stores the genesis block at height 1; ABCI reports the
    // pre-genesis state as height 0 so CometBFT can execute InitChain.
    private static long AbciHeight(long applicationHeight)
        => Math.Max(0, applicationHeight - 1);

    private static Address? ResolveProposer(long requestedHeight, ByteString proposerAddress)
    {
        var state = Application.State;
        if (requestedHeight != AbciHeight(state.Height) + 1)
        {
            return null;
        }

        var resolved = CometBftValidatorKeyLoader.TryResolveApplicationAddress(
            proposerAddress.ToByteArray(),
            state.ChainId);
        if (resolved is not null)
            return resolved;

        foreach (var stake in state.Stakes)
        {
            if (!stake.Jailed && stake.UnlockHeight == 0
                && ComputeSecp256k1CometAddress(stake.PubKey).SequenceEqual(proposerAddress.ToByteArray()))
            {
                return stake.Address;
            }
        }

        return null;
    }

    private static IEnumerable<ValidatorUpdate> BuildValidatorUpdates(State state)
    {
        foreach (var stake in state.Stakes.OrderBy(x => x.Address.Encoded, StringComparer.Ordinal))
        {
            if (stake.PubKey.Length is < 33 or > 65)
                continue;

            var power = stake.Jailed || stake.UnlockHeight != 0
                ? 0
                : Math.Max(1, Math.Min(long.MaxValue, stake.Amount.BaseUnits));
            yield return new ValidatorUpdate
            {
                PubKey = new PublicKey { Secp256K1 = Google.Protobuf.ByteString.CopyFrom(stake.PubKey) },
                Power = power
            };
        }
    }

    private static byte[] ComputeSecp256k1CometAddress(byte[] publicKey)
        => new NBitcoin.PubKey(publicKey).Hash.ToBytes();

    private static long TimestampSeconds(Timestamp timestamp)
        => timestamp?.Seconds ?? 0;
}
