using System;

namespace UbudKusCoin.Services;

public enum ConsensusEngineMode
{
    Development,
    CometBft
}

public enum ValidatorKeyCustodyMode
{
    LocalFile,
    ExternalSigner
}

public sealed class ConsensusEngineOptions
{
    public ConsensusEngineMode Mode { get; }
    public Uri RpcUrl { get; }
    public int StartupTimeoutSeconds { get; }
    public ValidatorKeyCustodyMode KeyCustodyMode { get; }
    public Uri ExternalSignerAddress { get; }

    private ConsensusEngineOptions(ConsensusEngineMode mode, Uri rpcUrl, int startupTimeoutSeconds,
        ValidatorKeyCustodyMode keyCustodyMode, Uri externalSignerAddress)
    {
        Mode = mode;
        RpcUrl = rpcUrl;
        StartupTimeoutSeconds = startupTimeoutSeconds;
        KeyCustodyMode = keyCustodyMode;
        ExternalSignerAddress = externalSignerAddress;
    }

    public static ConsensusEngineOptions FromEnvironment()
    {
        var mode = DotNetEnv.Env.GetString("CONSENSUS_ENGINE", "development");
        var rpcUrl = DotNetEnv.Env.GetString("COMETBFT_RPC_URL", string.Empty);
        var timeout = DotNetEnv.Env.GetInt("COMETBFT_STARTUP_TIMEOUT_SECONDS");
        var custody = DotNetEnv.Env.GetString("VALIDATOR_KEY_CUSTODY_MODE", "local-file");
        var signer = DotNetEnv.Env.GetString("COMETBFT_PRIV_VALIDATOR_LADDR", string.Empty);
        return Parse(mode, rpcUrl, timeout == 0 ? 60 : timeout, custody, signer);
    }

    public static ConsensusEngineOptions Parse(string mode, string rpcUrl)
        => Parse(mode, rpcUrl, 60);

    public static ConsensusEngineOptions Parse(string mode, string rpcUrl, int startupTimeoutSeconds)
        => Parse(mode, rpcUrl, startupTimeoutSeconds, "local-file", string.Empty);

    public static ConsensusEngineOptions Parse(string mode, string rpcUrl, int startupTimeoutSeconds,
        string custodyMode, string externalSignerAddress)
    {
        if (startupTimeoutSeconds < 0 || startupTimeoutSeconds > 600)
        {
            throw new InvalidOperationException("COMETBFT_STARTUP_TIMEOUT_SECONDS must be between 0 and 600.");
        }

        var keyCustodyMode = ParseCustodyMode(custodyMode);
        var signerAddress = ParseExternalSignerAddress(externalSignerAddress, keyCustodyMode);

        switch ((mode ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "development":
            case "in-process":
                if (keyCustodyMode == ValidatorKeyCustodyMode.ExternalSigner)
                    throw new InvalidOperationException("External validator signing requires CONSENSUS_ENGINE=cometbft.");
                return new ConsensusEngineOptions(ConsensusEngineMode.Development, null, startupTimeoutSeconds,
                    keyCustodyMode, null);
            case "cometbft":
                if (!Uri.TryCreate(rpcUrl?.Trim(), UriKind.Absolute, out var endpoint)
                    || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
                {
                    throw new InvalidOperationException(
                        "COMETBFT_RPC_URL must be an absolute HTTP(S) URL when CONSENSUS_ENGINE=cometbft.");
                }

                return new ConsensusEngineOptions(ConsensusEngineMode.CometBft, endpoint, startupTimeoutSeconds,
                    keyCustodyMode, signerAddress);
            default:
                throw new InvalidOperationException(
                    $"Unsupported CONSENSUS_ENGINE '{mode}'. Use 'development' or 'cometbft'.");
        }
    }

    private static ValidatorKeyCustodyMode ParseCustodyMode(string value)
        => (value ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "local-file" => ValidatorKeyCustodyMode.LocalFile,
            "external-signer" => ValidatorKeyCustodyMode.ExternalSigner,
            _ => throw new InvalidOperationException(
                "VALIDATOR_KEY_CUSTODY_MODE must be 'local-file' or 'external-signer'.")
        };

    private static Uri ParseExternalSignerAddress(string value, ValidatorKeyCustodyMode mode)
    {
        if (mode == ValidatorKeyCustodyMode.LocalFile)
            return null;

        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var endpoint)
            || !string.Equals(endpoint.Scheme, "tcp", StringComparison.OrdinalIgnoreCase)
            || endpoint.IsDefaultPort
            || endpoint.Port <= 0)
        {
            throw new InvalidOperationException(
                "COMETBFT_PRIV_VALIDATOR_LADDR must be an absolute tcp:// host:port URL for external-signer custody.");
        }

        return endpoint;
    }
}
