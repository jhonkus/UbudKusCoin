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
    public void StartupTimeout_IsBounded()
    {
        Assert.Equal(15, ConsensusEngineOptions.Parse("development", "", 15).StartupTimeoutSeconds);
        Assert.Throws<InvalidOperationException>(() => ConsensusEngineOptions.Parse("development", "", 601));
    }

    [Fact]
    public async Task CometBftAdapter_ReportsRpcHealth()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            "{\"result\":{\"node_info\":{},\"sync_info\":{\"latest_block_height\":\"42\"}}}");
        var adapter = new CometBftConsensusEngineAdapter(
            new Uri("http://localhost:26657"),
            new HttpClient(handler));

        var status = await adapter.GetStatusAsync();

        Assert.True(status.Healthy);
        Assert.Equal("cometbft", status.Engine);
        Assert.Contains("block 42", status.Message);
        Assert.Equal("/status", handler.RequestedPath);
    }

    [Fact]
    public async Task CometBftAdapter_RejectsInvalidStatusPayload()
    {
        var adapter = new CometBftConsensusEngineAdapter(
            new Uri("http://localhost:26657"),
            new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")));

        var status = await adapter.GetStatusAsync();

        Assert.False(status.Healthy);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _body;
        public string? RequestedPath { get; private set; }

        public StubHandler(HttpStatusCode statusCode, string body = "")
        {
            _statusCode = statusCode;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestedPath = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body)
            });
        }
    }
}
