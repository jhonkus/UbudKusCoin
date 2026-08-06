using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class ConsensusEngineTests
{
    [Fact]
    public void CometBftMode_RequiresAbsoluteHttpRpcUrl()
    {
        Assert.Throws<InvalidOperationException>(() => ConsensusEngineOptions.Parse("cometbft", ""));
        Assert.Throws<InvalidOperationException>(() => ConsensusEngineOptions.Parse("cometbft", "not-a-url"));
    }

    [Fact]
    public void UnknownMode_IsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => ConsensusEngineOptions.Parse("fake", ""));
    }

    [Fact]
    public async Task CometBftAdapter_ReportsRpcHealth()
    {
        var handler = new StubHandler(HttpStatusCode.OK);
        var adapter = new CometBftConsensusEngineAdapter(
            new Uri("http://localhost:26657"),
            new HttpClient(handler));

        var status = await adapter.GetStatusAsync();

        Assert.True(status.Healthy);
        Assert.Equal("cometbft", status.Engine);
        Assert.Equal("/status", handler.RequestedPath);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        public string? RequestedPath { get; private set; }

        public StubHandler(HttpStatusCode statusCode) => _statusCode = statusCode;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedPath = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
