using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace UbudKusCoin.Services;

public sealed class ConsensusReadinessMonitor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ServicePool.ConsensusEngine?.Mode != ConsensusEngineMode.CometBft)
        {
            return;
        }

        var options = ConsensusEngineOptions.FromEnvironment();
        var deadline = DateTime.UtcNow.AddSeconds(options.StartupTimeoutSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            var status = await ServicePool.ConsensusEngine.GetStatusAsync(stoppingToken);
            if (status.Healthy)
            {
                Console.WriteLine(".... External consensus engine is ready.");
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Console.WriteLine($".... External consensus engine is not ready: {status.Message}");
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
