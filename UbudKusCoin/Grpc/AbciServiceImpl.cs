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
            LastBlockHeight = state.Height,
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
                Height = Application.State.Height
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
        var transactions = DecodeTransactions(request.Txs, out var decodeError);
        var result = decodeError is null
            ? Application.FinalizeBlock(transactions, TimestampSeconds(request.Time))
            : new ApplicationFinalizeResult(false, null, Array.Empty<byte>(), decodeError);
        var response = new ResponseFinalizeBlock { AppHash = ByteString.CopyFrom(result.AppHash) };
        response.TxResults.AddRange(transactions.Select(_ => new ExecTxResult
        {
            Code = result.Accepted ? Success : InvalidTransaction,
            Log = result.Message
        }));
        return Task.FromResult(response);
    }

    public override Task<ResponseCommit> Commit(RequestCommit request, ServerCallContext context)
        => Task.FromResult(new ResponseCommit());

    public override Task<ResponseInitChain> InitChain(RequestInitChain request, ServerCallContext context)
        => Task.FromResult(new ResponseInitChain
        {
            AppHash = ByteString.CopyFrom(Application.State.ComputeStateRoot())
        });

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

    private static long TimestampSeconds(Timestamp timestamp)
        => timestamp?.Seconds ?? 0;
}
