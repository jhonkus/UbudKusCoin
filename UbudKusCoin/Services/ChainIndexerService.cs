#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Services;

public sealed class ChainIndexerService : BackgroundService
{
    private readonly IndexerStore _store;
    private readonly CanonicalNodeService _nodeService;
    private readonly TimeSpan _pollInterval;

    public ChainIndexerService(IndexerStore store, CanonicalNodeService nodeService, TimeSpan? pollInterval = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _nodeService = nodeService ?? throw new ArgumentNullException(nameof(nodeService));
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(500);
    }

    public IndexerStore Store => _store;

    public void CatchUp()
    {
        var chain = _nodeService.Chain;
        var lastIndexed = _store.GetLastIndexedHeight();
        var headHeight = chain.State.Height;

        if (lastIndexed >= headHeight)
        {
            return;
        }

        var blocks = chain.GetCanonicalBlocks(lastIndexed);
        foreach (var block in blocks)
        {
            if (block.Height > lastIndexed)
            {
                _store.IndexBlock(block, chain.State);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CatchUp();
            }
            catch (Exception)
            {
                // Background indexer retry loop
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
