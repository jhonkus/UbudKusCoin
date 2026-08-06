using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace UbudKusCoin.Services;

public static class ExternalSignerConnectivity
{
    public static async Task EnsureReachableAsync(
        Uri endpoint,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (!string.Equals(endpoint.Scheme, "tcp", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("External signer reachability checks require a tcp:// endpoint.");
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }

        using var client = new TcpClient();
        var connectTask = client.ConnectAsync(endpoint.Host, endpoint.Port);
        var completed = await Task.WhenAny(connectTask, Task.Delay(timeout, cancellationToken));
        if (completed != connectTask)
        {
            throw new InvalidOperationException(
                $"External signer endpoint '{endpoint}' was not reachable within {timeout.TotalSeconds:0.###} seconds.");
        }

        try
        {
            await connectTask;
        }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException)
        {
            throw new InvalidOperationException(
                $"External signer endpoint '{endpoint}' is not reachable.",
                exception);
        }
    }
}
