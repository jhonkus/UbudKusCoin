#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace UbudKusCoin.Services;

public static class NodeTelemetry
{
    public const string MeterName = "UbudKusCoin.Node";
    public static readonly ActivitySource ActivitySource = new(MeterName);
    public static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> ApiRequestsBlocked = Meter.CreateCounter<long>(
        "ubudkuscoin_api_requests_blocked_total",
        description: "Total inbound API requests blocked by authentication or rate limits.");

    public static readonly Counter<long> PeerAdmissions = Meter.CreateCounter<long>(
        "ubudkuscoin_peer_admissions_total",
        description: "Total peer admission decisions.");

    public static readonly Counter<long> ReadinessChecks = Meter.CreateCounter<long>(
        "ubudkuscoin_readiness_checks_total",
        description: "Total readiness checks performed by the node.");

    public static void RecordApiBlocked(string reason)
    {
        ApiRequestsBlocked.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }

    public static void RecordPeerAdmission(bool accepted, string reason)
    {
        PeerAdmissions.Add(1,
            new KeyValuePair<string, object?>("accepted", accepted),
            new KeyValuePair<string, object?>("reason", reason));
    }

    public static void RecordReadinessCheck(bool ready, string stage)
    {
        ReadinessChecks.Add(1,
            new KeyValuePair<string, object?>("ready", ready),
            new KeyValuePair<string, object?>("stage", stage));
    }
}
