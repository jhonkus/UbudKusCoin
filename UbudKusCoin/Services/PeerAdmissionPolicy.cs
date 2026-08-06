using System;
using System.Collections.Generic;
using System.Linq;
using UbudKusCoin.Grpc;

namespace UbudKusCoin.Services;

public static class PeerAdmissionPolicy
{
    public static int GetMaxKnownPeers()
    {
        var configured = DotNetEnv.Env.GetInt("MAX_KNOWN_PEERS");
        if (configured <= 0)
        {
            return 64;
        }

        return Math.Clamp(configured, 1, 1024);
    }

    public static long Score(Peer peer, long nowSeconds)
    {
        if (peer is null)
        {
            return long.MinValue;
        }

        var score = 0L;
        if (peer.IsBootstrap)
        {
            score += 100;
        }

        if (peer.IsCanreach)
        {
            score += 60;
        }

        var lastContact = Math.Max(peer.LastReach, peer.TimeStamp);
        if (lastContact > 0)
        {
            var age = Math.Max(0, nowSeconds - lastContact);
            score += age switch
            {
                < 300 => 40,
                < 3_600 => 25,
                < 86_400 => 10,
                _ => 0
            };
        }

        return score;
    }

    public static IReadOnlyList<Peer> OrderPeers(IEnumerable<Peer> peers, long nowSeconds)
        => peers
            .OrderByDescending(peer => Score(peer, nowSeconds))
            .ThenByDescending(peer => peer.LastReach)
            .ThenByDescending(peer => peer.TimeStamp)
            .ThenBy(peer => peer.Address, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
