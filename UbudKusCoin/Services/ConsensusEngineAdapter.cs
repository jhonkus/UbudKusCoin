using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UbudKusCoin.Services;

public sealed record ConsensusEngineStatus(bool Healthy, string Engine, string Message);

public interface IConsensusEngineAdapter
{
    ConsensusEngineMode Mode { get; }
    Task<ConsensusEngineStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class DevelopmentConsensusEngineAdapter : IConsensusEngineAdapter
{
    public ConsensusEngineMode Mode => ConsensusEngineMode.Development;

    public Task<ConsensusEngineStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ConsensusEngineStatus(
            false,
            "development",
            "The in-process consensus driver is for local development and testing only."));
    }
}

public sealed class CometBftConsensusEngineAdapter : IConsensusEngineAdapter
{
    private readonly HttpClient _httpClient;

    public CometBftConsensusEngineAdapter(Uri rpcUrl, HttpClient httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = rpcUrl;
    }

    public ConsensusEngineMode Mode => ConsensusEngineMode.CometBft;

    public async Task<ConsensusEngineStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("status", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new ConsensusEngineStatus(false, "cometbft", $"RPC returned {(int)response.StatusCode}.");
            }

            return new ConsensusEngineStatus(true, "cometbft", "CometBFT RPC is reachable.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new ConsensusEngineStatus(false, "cometbft", $"CometBFT RPC is unavailable: {exception.Message}");
        }
    }
}

public static class ConsensusEngineFactory
{
    public static IConsensusEngineAdapter Create(ConsensusEngineOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return options.Mode switch
        {
            ConsensusEngineMode.Development => new DevelopmentConsensusEngineAdapter(),
            ConsensusEngineMode.CometBft => new CometBftConsensusEngineAdapter(options.RpcUrl),
            _ => throw new InvalidOperationException("Unsupported consensus engine mode.")
        };
    }
}
