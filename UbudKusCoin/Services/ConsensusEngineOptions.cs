using System;

namespace UbudKusCoin.Services;

public enum ConsensusEngineMode
{
    Development,
    CometBft
}

public sealed class ConsensusEngineOptions
{
    public ConsensusEngineMode Mode { get; }
    public Uri RpcUrl { get; }

    private ConsensusEngineOptions(ConsensusEngineMode mode, Uri rpcUrl)
    {
        Mode = mode;
        RpcUrl = rpcUrl;
    }

    public static ConsensusEngineOptions FromEnvironment()
    {
        var mode = DotNetEnv.Env.GetString("CONSENSUS_ENGINE", "development");
        var rpcUrl = DotNetEnv.Env.GetString("COMETBFT_RPC_URL", string.Empty);
        return Parse(mode, rpcUrl);
    }

    public static ConsensusEngineOptions Parse(string mode, string rpcUrl)
    {
        switch ((mode ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "development":
            case "in-process":
                return new ConsensusEngineOptions(ConsensusEngineMode.Development, null);
            case "cometbft":
                if (!Uri.TryCreate(rpcUrl?.Trim(), UriKind.Absolute, out var endpoint)
                    || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
                {
                    throw new InvalidOperationException(
                        "COMETBFT_RPC_URL must be an absolute HTTP(S) URL when CONSENSUS_ENGINE=cometbft.");
                }

                return new ConsensusEngineOptions(ConsensusEngineMode.CometBft, endpoint);
            default:
                throw new InvalidOperationException(
                    $"Unsupported CONSENSUS_ENGINE '{mode}'. Use 'development' or 'cometbft'.");
        }
    }
}
