using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using UbudKusCoin.Services;

namespace UbudKusCoin.Grpc;

public sealed class ReadModelServiceImpl : ReadModelService.ReadModelServiceBase
{
    public override Task<StatsResponse> GetStats(StatsRequest request, ServerCallContext context)
    {
        var blocks = ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(0).ToList();
        var transactions = blocks.SelectMany(block => block.Txs).ToList();
        var pending = Array.Empty<UbudKusCoin.Core.Types.Transaction>();
        var accounts = ServicePool.CanonicalNodeService.Chain.State.Accounts.Count();

        return Task.FromResult(new StatsResponse
        {
            BlockCount = blocks.Count,
            TransactionCount = transactions.Count,
            AccountCount = accounts,
            TransactionAmount = transactions.Sum(x => x.Amount.BaseUnits),
            RewardAmount = blocks.Sum(x => x.Reward.BaseUnits),
            PendingCount = pending.Length,
            PendingAmount = pending.Sum(x => x.Amount.BaseUnits),
            LatestBlockHeight = blocks.Count == 0 ? 0 : blocks.Max(x => x.Height)
        });
    }

    public override Task<ChartResponse> GetTransactionChart(ChartRequest request, ServerCallContext context)
    {
        var limit = Math.Clamp(request.Limit == 0 ? 30 : request.Limit, 1, 365);
        var transactions = ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(0)
            .SelectMany(block => block.Txs.Select(transaction => new { transaction, block.TimeStamp }))
            .OrderByDescending(item => item.TimeStamp)
            .Take(limit * 100)
            .ToList();
        var response = new ChartResponse();
        response.Points.AddRange(transactions
            .GroupBy(x => x.TimeStamp / 3600 * 3600)
            .OrderBy(x => x.Key)
            .TakeLast(limit)
            .Select(group => new ChartPoint
            {
                Timestamp = group.Key,
                TransactionCount = group.LongCount(),
                Amount = group.Sum(x => x.transaction.Amount.BaseUnits)
            }));
        return Task.FromResult(response);
    }
}
