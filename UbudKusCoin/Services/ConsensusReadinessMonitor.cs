using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UbudKusCoin.Services;

public sealed class ConsensusReadinessMonitor : BackgroundService
{
    private readonly ILogger<ConsensusReadinessMonitor> _logger;

    public ConsensusReadinessMonitor(ILogger<ConsensusReadinessMonitor> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (ServicePool.ConsensusEngine?.Mode != ConsensusEngineMode.CometBft)
        {
            NodeReadinessState.SetConsensusMode(ConsensusEngineMode.Development);
            NodeReadinessState.SetConsensusStatus(new ConsensusEngineStatus(true, "development", "Development consensus is active."));
            return;
        }

        NodeReadinessState.SetConsensusMode(ConsensusEngineMode.CometBft);
        var options = ConsensusEngineOptions.FromEnvironment();
        var deadline = DateTime.UtcNow.AddSeconds(options.StartupTimeoutSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            using var activity = NodeTelemetry.ActivitySource.StartActivity("consensus.readiness.poll");
            var status = await ServicePool.ConsensusEngine.GetStatusAsync(stoppingToken);
            NodeReadinessState.SetConsensusStatus(status);
            NodeTelemetry.RecordReadinessCheck(status.Healthy, "consensus");
            if (status.Healthy)
            {
                _logger.LogInformation("External consensus engine is ready: {Message}", status.Message);
                return;
            }

            if (DateTime.UtcNow >= deadline)
            {
                _logger.LogWarning("External consensus engine timed out during startup: {Message}", status.Message);
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
