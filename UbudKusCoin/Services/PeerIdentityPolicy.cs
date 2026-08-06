using System;

namespace UbudKusCoin.Services;

public static class PeerIdentityPolicy
{
    public static bool TryNormalizeEndpoint(string value, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = "Peer address is required.";
            return false;
        }

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp
                && uri.Scheme != Uri.UriSchemeHttps
                && !string.Equals(uri.Scheme, "tcp", StringComparison.OrdinalIgnoreCase)))
        {
            error = "Peer address must be an absolute http(s):// or tcp:// URI.";
            return false;
        }

        if (uri.Port <= 0)
        {
            error = "Peer address must include a valid port.";
            return false;
        }

        normalized = $"{uri.Scheme.ToLowerInvariant()}://{uri.Host.ToLowerInvariant()}:{uri.Port}";
        return true;
    }

    public static bool AreSameEndpoint(string left, string right)
    {
        return TryNormalizeEndpoint(left, out var normalizedLeft, out _)
            && TryNormalizeEndpoint(right, out var normalizedRight, out _)
            && string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSelfEndpoint(string candidate, string selfAddress)
        => AreSameEndpoint(candidate, selfAddress);
}
