using System;
using System.Collections.Generic;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;
using Xunit;

namespace UbudKusCoin.Tests;

public sealed class PeerPolicyTests
{
    [Fact]
    public void NormalizeEndpoint_RejectsInvalidAddresses()
    {
        Assert.False(PeerIdentityPolicy.TryNormalizeEndpoint("", out _, out var emptyError));
        Assert.Contains("required", emptyError, StringComparison.OrdinalIgnoreCase);

        Assert.False(PeerIdentityPolicy.TryNormalizeEndpoint("localhost:26657", out _, out var schemeError));
        Assert.Contains("absolute", schemeError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Score_FavorsReachableBootstrapPeers()
    {
        var now = 10_000L;
        var bootstrap = new Peer
        {
            Address = "http://bootstrap:26657",
            IsBootstrap = true,
            IsCanreach = true,
            LastReach = now - 30
        };

        var stale = new Peer
        {
            Address = "http://stale:26657",
            IsBootstrap = false,
            IsCanreach = false,
            LastReach = now - 86_500
        };

        Assert.True(PeerAdmissionPolicy.Score(bootstrap, now) > PeerAdmissionPolicy.Score(stale, now));
    }

    [Fact]
    public void OrderPeers_SortsByScoreAndRecency()
    {
        var now = 10_000L;
        var peers = new List<Peer>
        {
            new()
            {
                Address = "http://c:26657",
                IsBootstrap = false,
                IsCanreach = false,
                LastReach = now - 5000
            },
            new()
            {
                Address = "http://a:26657",
                IsBootstrap = true,
                IsCanreach = true,
                LastReach = now - 10
            },
            new()
            {
                Address = "http://b:26657",
                IsBootstrap = false,
                IsCanreach = true,
                LastReach = now - 60
            }
        };

        var ordered = PeerAdmissionPolicy.OrderPeers(peers, now);

        Assert.Equal("http://a:26657", ordered[0].Address);
        Assert.Equal("http://b:26657", ordered[1].Address);
        Assert.Equal("http://c:26657", ordered[2].Address);
    }
}
