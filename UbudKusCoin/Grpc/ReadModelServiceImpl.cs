using System;
using System.Collections.Generic;
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
        var pending = ServicePool.DbService.PoolTransactionsDb.GetAll().FindAll();
        var accounts = ServicePool.CanonicalNodeService.Chain.State.Accounts.Count();

        return Task.FromResult(new StatsResponse
        {
            BlockCount = blocks.Count,
            TransactionCount = transactions.Count,
            AccountCount = accounts,
            TransactionAmount = transactions.Sum(x => x.Amount.BaseUnits),
            RewardAmount = blocks.Sum(x => x.Reward.BaseUnits),
            PendingCount = pending.Count(),
            PendingAmount = pending.Sum(x => x.Amount),
            LatestBlockHeight = blocks.Count == 0 ? 0 : blocks.Max(x => x.Height)
        });
    }

    public override Task<ChartResponse> GetTransactionChart(ChartRequest request, ServerCallContext context)
    {
        var limit = Math.Clamp(request.Limit == 0 ? 30 : request.Limit, 1, 365);
        var latestBucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 3600 * 3600;
        var firstBucket = latestBucket - ((long)limit - 1) * 3600;
        var transactions = ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(0)
            .SelectMany(block => block.Txs.Select(transaction => new { transaction, block.TimeStamp }))
            .Where(item => item.TimeStamp >= firstBucket && item.TimeStamp <= latestBucket + 3599)
            .GroupBy(item => item.TimeStamp / 3600 * 3600)
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    Count = group.LongCount(),
                    Amount = group.Sum(x => x.transaction.Amount.BaseUnits)
                });
        var response = new ChartResponse();
        response.Points.AddRange(Enumerable.Range(0, limit)
            .Select(index => firstBucket + index * 3600L)
            .Select(bucket =>
            {
                var bucketData = transactions.GetValueOrDefault(bucket);
                return new ChartPoint
                {
                    Timestamp = bucket,
                    TransactionCount = bucketData?.Count ?? 0,
                    Amount = bucketData?.Amount ?? 0
                };
            }));
        return Task.FromResult(response);
    }
}
