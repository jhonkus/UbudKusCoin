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
    public int StartupTimeoutSeconds { get; }

    private ConsensusEngineOptions(ConsensusEngineMode mode, Uri rpcUrl, int startupTimeoutSeconds)
    {
        Mode = mode;
        RpcUrl = rpcUrl;
        StartupTimeoutSeconds = startupTimeoutSeconds;
    }

    public static ConsensusEngineOptions FromEnvironment()
    {
        var mode = DotNetEnv.Env.GetString("CONSENSUS_ENGINE", "development");
        var rpcUrl = DotNetEnv.Env.GetString("COMETBFT_RPC_URL", string.Empty);
        var timeout = DotNetEnv.Env.GetInt("COMETBFT_STARTUP_TIMEOUT_SECONDS");
        return Parse(mode, rpcUrl, timeout == 0 ? 60 : timeout);
    }

    public static ConsensusEngineOptions Parse(string mode, string rpcUrl)
        => Parse(mode, rpcUrl, 60);

    public static ConsensusEngineOptions Parse(string mode, string rpcUrl, int startupTimeoutSeconds)
    {
        if (startupTimeoutSeconds < 0 || startupTimeoutSeconds > 600)
        {
            throw new InvalidOperationException("COMETBFT_STARTUP_TIMEOUT_SECONDS must be between 0 and 600.");
        }

        switch ((mode ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "development":
            case "in-process":
                return new ConsensusEngineOptions(ConsensusEngineMode.Development, null, startupTimeoutSeconds);
            case "cometbft":
                if (!Uri.TryCreate(rpcUrl?.Trim(), UriKind.Absolute, out var endpoint)
                    || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
                {
                    throw new InvalidOperationException(
                        "COMETBFT_RPC_URL must be an absolute HTTP(S) URL when CONSENSUS_ENGINE=cometbft.");
                }

                return new ConsensusEngineOptions(ConsensusEngineMode.CometBft, endpoint, startupTimeoutSeconds);
            default:
                throw new InvalidOperationException(
                    $"Unsupported CONSENSUS_ENGINE '{mode}'. Use 'development' or 'cometbft'.");
        }
    }
}
