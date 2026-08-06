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
        var blocks = ServicePool.DbService.BlockDb.GetAll().FindAll().ToList();
        var transactions = ServicePool.DbService.TransactionDb.GetAll().FindAll().ToList();
        var pending = ServicePool.DbService.PoolTransactionsDb.GetAll().FindAll().ToList();
        var accounts = ServicePool.DbService.AccountDb.GetRange(1, int.MaxValue).Count();

        return Task.FromResult(new StatsResponse
        {
            BlockCount = blocks.Count,
            TransactionCount = transactions.Count,
            AccountCount = accounts,
            TransactionAmount = transactions.Sum(x => x.Amount),
            RewardAmount = blocks.Sum(x => x.TotalReward),
            PendingCount = pending.Count,
            PendingAmount = pending.Sum(x => x.Amount),
            LatestBlockHeight = blocks.Count == 0 ? 0 : blocks.Max(x => x.Height)
        });
    }

    public override Task<ChartResponse> GetTransactionChart(ChartRequest request, ServerCallContext context)
    {
        var limit = Math.Clamp(request.Limit == 0 ? 30 : request.Limit, 1, 365);
        var transactions = ServicePool.DbService.TransactionDb.GetAll()
            .FindAll()
            .OrderByDescending(x => x.TimeStamp)
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
                Amount = group.Sum(x => x.Amount)
            }));
        return Task.FromResult(response);
    }
}
