using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class ExternalSignerConnectivityTests
{
    [Fact]
    public async Task EnsureReachableAsync_AllowsListeningTcpEndpoint()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = new Uri($"tcp://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}");

        await ExternalSignerConnectivity.EnsureReachableAsync(endpoint, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task EnsureReachableAsync_RejectsStoppedTcpEndpoint()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var endpoint = new Uri($"tcp://127.0.0.1:{port}");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ExternalSignerConnectivity.EnsureReachableAsync(endpoint, TimeSpan.FromMilliseconds(200)));
    }
}
