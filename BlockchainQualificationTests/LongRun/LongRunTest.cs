using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Data.Sqlite;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Services;
using BlockchainQualificationTests.Utilities;
using Key = NBitcoin.Key;

namespace BlockchainQualificationTests.LongRun;

/// <summary>
/// LONG RUN TEST
/// Commits 10,000 blocks and collects per-interval telemetry:
///   CPU usage (wall-clock elapsed per batch), Managed memory (GC),
///   Working set (handle-level), LMDB snapshot file size, SQLite size,
///   Block time, AppHash (sampled at 1 k, 5 k, 10 k).
/// Detects: memory growth anomalies, handle leaks, database growth anomalies.
/// </summary>
public static class LongRunTest
{
    // ─── constants ───────────────────────────────────────────────────────────
    private const int TotalBlocks = 10_000;
    private const int SampleInterval = 500;   // collect telemetry every N blocks
    private const int TxsPerBlock = 2;        // alternating Transfer txs per block

    // anomaly thresholds
    // Memory: GC growth should not exceed chain (LMDB) growth × this factor.
    // A blockchain stores ALL blocks in memory; GC growing in lockstep with chain is expected.
    // Growing FASTER than chain data by >1.5× signals a genuine heap leak.
    private const double MemoryLeakGcToLmdbFactor = 1.5;
    // Working set: hard ceiling — WS must not grow more than chain data × this factor
    // (chain JSON in LMDB + process overhead + SQLite cache).
    private const double MaxWsToLmdbFactor        = 2.0;
    private const double DbGrowthAnomalyFactor     = 3.0;     // SQLite grows 3× over LMDB ⇒ anomaly
    private const long   MaxHandleThreshold        = 1024;    // OS handles hard ceiling

    // ─── entry point ─────────────────────────────────────────────────────────
    public static TestResult Run()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[LONG RUN TEST] Starting — target: 10,000 blocks");

        var tempDir      = Path.Combine(Path.GetTempPath(), "ukc-longrun-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var snapshotFile = Path.Combine(tempDir, "canonical.json");
        var sqliteFile   = Path.Combine(tempDir, "indexer.db");

        var chainId = ChainInfo.ChainIdTestnet;

        // Build key pool:  sender → receivers (10 pairs)
        var keys     = Enumerable.Range(1, 20).Select(i => MakeKey((byte)i)).ToArray();
        var addrs    = keys.Select(k => Address.FromPublicKey(ChainInfo.AddressVersion(chainId), k.PubKey.ToBytes())).ToArray();

        // Manifest: fund every key with 100 M coins so we never run out
        var manifest = Genesis.CreateDefaultManifest(chainId);
        foreach (var k in keys)
        {
            manifest.Accounts.Add(new GenesisAccount(
                Convert.ToHexString(k.PubKey.ToBytes()),
                Money.FromCoins(100_000_000m).BaseUnits));
        }

        // Nonce tracking per sender
        var nonces = new ulong[keys.Length];

        // ── set up node + indexer ─────────────────────────────────────────────
        var node    = new CanonicalNodeService(chainId, snapshotFile, null, manifest);
        var indexer = new IndexerStore(sqliteFile);

        // ── telemetry arrays ──────────────────────────────────────────────────
        var sampleHeights     = new List<long>();
        var gcManagedMb       = new List<double>();   // GC.GetTotalMemory (MB)
        var wsMb              = new List<double>();   // working set (MB)
        var handleCount       = new List<long>();
        var lmdbKb            = new List<long>();     // snapshot JSON size (kB)
        var sqliteKb          = new List<long>();
        var blockTimesMs      = new List<double>();   // wall time per batch / SampleInterval

        // baseline telemetry
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        var baseGcMb  = GC.GetTotalMemory(false) / 1_048_576.0;
        var baseWsMb  = Process.GetCurrentProcess().WorkingSet64 / 1_048_576.0;

        string? failReason    = null;
        byte[]? appHashAt1k   = null;
        byte[]? appHashAt5k   = null;
        byte[]? appHashAt10k  = null;
        var batchSw           = Stopwatch.StartNew();

        // ── commit 10,000 blocks ──────────────────────────────────────────────
        for (int b = 1; b <= TotalBlocks; b++)
        {
            // Build 2 transfer transactions (alternating sender pairs)
            var txs = new List<Transaction>();
            for (int t = 0; t < TxsPerBlock; t++)
            {
                int senderIdx    = (b * TxsPerBlock + t) % (keys.Length / 2);       // 0..9
                int recipientIdx = senderIdx + keys.Length / 2;                     // 10..19

                nonces[senderIdx]++;
                var tx = new Transaction
                {
                    Version  = ChainInfo.TxVersion,
                    ChainId  = chainId,
                    Kind     = TransactionKind.Transfer,
                    From     = addrs[senderIdx],
                    To       = addrs[recipientIdx],
                    Amount   = Money.FromCoins(0.01m),
                    Fee      = Money.FromCoins(0.001m),
                    Nonce    = nonces[senderIdx],
                    PubKey   = keys[senderIdx].PubKey.ToBytes()
                };
                tx.Signature = TransactionSigner.Sign(tx, keys[senderIdx].ToBytes());
                txs.Add(tx);
            }

            var timestamp = manifest.GenesisTime + b * 5L; // deterministic 5s per block
            var (ok, block, msg) = node.AcceptExternalCommit(
                txs,
                timestamp,
                addrs[0],   // constant validator
                node.Chain.State.Height);

            if (!ok)
            {
                failReason = $"AcceptExternalCommit failed at block {b}: {msg}";
                break;
            }

            // Sync indexer (synchronously for determinism)
            try { indexer.IndexBlock(block, node.Chain.State); }
            catch (Exception ex)
            {
                // indexer failure is anomalous but should not crash application
                failReason = $"IndexerStore.IndexBlock threw at block {b}: {ex.Message}";
                break;
            }

            // ── snapshot AppHash at checkpoints ──────────────────────────────
            if (b == 1_000) appHashAt1k  = node.Chain.State.ComputeStateRoot().ToArray();
            if (b == 5_000) appHashAt5k  = node.Chain.State.ComputeStateRoot().ToArray();
            if (b == TotalBlocks) appHashAt10k = node.Chain.State.ComputeStateRoot().ToArray();

            // ── periodic telemetry ───────────────────────────────────────────
            if (b % SampleInterval == 0)
            {
                GC.Collect(2, GCCollectionMode.Forced, blocking: true);

                var proc       = Process.GetCurrentProcess();
                proc.Refresh();

                sampleHeights.Add(b);
                gcManagedMb.Add(GC.GetTotalMemory(false) / 1_048_576.0);
                wsMb.Add(proc.WorkingSet64 / 1_048_576.0);
                handleCount.Add(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? proc.HandleCount
                    : 0);
                lmdbKb.Add(FileKb(snapshotFile));
                sqliteKb.Add(FileKb(sqliteFile));
                blockTimesMs.Add(batchSw.Elapsed.TotalMilliseconds / SampleInterval);

                batchSw.Restart();

                Console.WriteLine(
                    $"  Block {b,6}/{TotalBlocks}  " +
                    $"GC={gcManagedMb[^1]:F1} MB  WS={wsMb[^1]:F1} MB  " +
                    $"Handles={handleCount[^1]}  " +
                    $"LMDB={lmdbKb[^1]} kB  SQLite={sqliteKb[^1]} kB  " +
                    $"BlkTime={blockTimesMs[^1]:F2} ms/blk");
            }
        }

        // cleanup
        try { indexer.Dispose(); } catch { /* best-effort */ }
        try { Directory.Delete(tempDir, true); } catch { /* best-effort */ }

        if (failReason is not null)
        {
            return Fail(stopwatch, failReason);
        }

        // ── anomaly detection ─────────────────────────────────────────────────
        var anomalies = new List<string>();

        // Compute the chain-data growth ratio (LMDB) as the reference baseline.
        // All memory checks are expressed relative to this so that expected
        // in-memory growth (blocks accumulating) does not trigger false positives.
        double lmdbGrowthRatio = (lmdbKb.Count >= 2 && lmdbKb[0] > 0)
            ? (double)lmdbKb[^1] / lmdbKb[0]
            : 1.0;
        double lmdbGrowthKb = lmdbKb.Count >= 2 ? lmdbKb[^1] - lmdbKb[0] : 0;

        // 1. Memory leak: GC should not grow faster than chain data × 1.5.
        //    A blockchain that keeps all blocks in memory will see GC grow
        //    proportionally to chain size — that is expected, not a leak.
        if (gcManagedMb.Count >= 2)
        {
            var gcGrowthRatio  = gcManagedMb[^1] / Math.Max(gcManagedMb[0], 0.1);
            var allowedGcRatio = lmdbGrowthRatio * MemoryLeakGcToLmdbFactor;
            if (gcGrowthRatio > allowedGcRatio)
            {
                anomalies.Add(
                    $"MEMORY LEAK: GC managed memory grew {gcGrowthRatio:F2}× " +
                    $"({gcManagedMb[0]:F1} MB → {gcManagedMb[^1]:F1} MB) " +
                    $"which exceeds the allowed ratio of chain growth ({lmdbGrowthRatio:F2}×) × {MemoryLeakGcToLmdbFactor:F1} = {allowedGcRatio:F2}×");
            }
        }

        // 2. Working set: WS growth must not exceed LMDB growth (in kB, scaled) × MaxWsToLmdbFactor.
        //    Convert LMDB growth to MB for comparison.
        if (wsMb.Count >= 2)
        {
            var wsGrowthMb      = wsMb[^1] - baseWsMb;
            var allowedWsGrowth = (lmdbGrowthKb / 1024.0) * MaxWsToLmdbFactor;
            if (wsGrowthMb > allowedWsGrowth && wsGrowthMb > 200)
            {
                anomalies.Add(
                    $"WORKING SET ANOMALY: WS grew {wsGrowthMb:F1} MB above baseline " +
                    $"(allowed ≤ {allowedWsGrowth:F1} MB based on LMDB growth {lmdbGrowthKb:F0} kB × {MaxWsToLmdbFactor}×).");
            }
        }

        // 3. Handle leak (Windows only)
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            && handleCount.Count >= 2)
        {
            var handleDelta = handleCount[^1] - handleCount[0];
            if (handleDelta > MaxHandleThreshold)
            {
                anomalies.Add(
                    $"HANDLE LEAK: OS handle count grew by {handleDelta} " +
                    $"({handleCount[0]} → {handleCount[^1]}) over 10,000 blocks.");
            }
        }

        // 4. Database growth anomaly: SQLite should not grow disproportionately
        if (sqliteKb.Count >= 2 && lmdbKb.Count >= 2)
        {
            var sqliteGrowth = (double)(sqliteKb[^1] - sqliteKb[0]);
            var lmdbGrowth   = (double)(lmdbKb[^1]   - lmdbKb[0]);
            if (lmdbGrowth > 0 && sqliteGrowth / lmdbGrowth > DbGrowthAnomalyFactor)
            {
                anomalies.Add(
                    $"DB GROWTH ANOMALY: SQLite grew {sqliteGrowth:F0} kB " +
                    $"vs LMDB {lmdbGrowth:F0} kB (ratio {sqliteGrowth / lmdbGrowth:F1}×)");
            }
        }

        // 5. AppHash must be deterministic across checkpoints (spot check: 1k, 5k, 10k all non-null)
        if (appHashAt1k is null || appHashAt5k is null || appHashAt10k is null)
        {
            anomalies.Add("APPHASH SAMPLING FAILED: one or more checkpoint AppHash values were not captured.");
        }

        stopwatch.Stop();

        // ── build evidence ────────────────────────────────────────────────────
        var sb = new StringBuilder();
        sb.AppendLine($"Committed {TotalBlocks:N0} blocks with {TxsPerBlock} txs each ({TotalBlocks * TxsPerBlock:N0} total txs).");
        sb.AppendLine($"Total wall time: {stopwatch.Elapsed.TotalSeconds:F1}s");
        sb.AppendLine($"Average block time: {stopwatch.Elapsed.TotalMilliseconds / TotalBlocks:F2} ms/block");
        sb.AppendLine();
        sb.AppendLine("AppHash Checkpoints:");
        sb.AppendLine($"  Height  1,000: {(appHashAt1k  is null ? "N/A" : Convert.ToHexString(appHashAt1k))}");
        sb.AppendLine($"  Height  5,000: {(appHashAt5k  is null ? "N/A" : Convert.ToHexString(appHashAt5k))}");
        sb.AppendLine($"  Height 10,000: {(appHashAt10k is null ? "N/A" : Convert.ToHexString(appHashAt10k))}");
        sb.AppendLine();
        sb.AppendLine($"GC Managed Memory: {baseGcMb:F1} MB → {(gcManagedMb.Count > 0 ? gcManagedMb[^1] : 0):F1} MB");
        sb.AppendLine($"Working Set:       {baseWsMb:F1} MB → {(wsMb.Count > 0 ? wsMb[^1] : 0):F1} MB");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && handleCount.Count >= 2)
            sb.AppendLine($"OS Handles:        {handleCount[0]} → {handleCount[^1]} (delta {handleCount[^1] - handleCount[0]})");
        sb.AppendLine($"LMDB snapshot:     {(lmdbKb.Count > 0 ? lmdbKb[0] : 0):N0} kB → {(lmdbKb.Count > 0 ? lmdbKb[^1] : 0):N0} kB");
        sb.AppendLine($"SQLite indexer:    {(sqliteKb.Count > 0 ? sqliteKb[0] : 0):N0} kB → {(sqliteKb.Count > 0 ? sqliteKb[^1] : 0):N0} kB");
        sb.AppendLine();
        sb.AppendLine(anomalies.Count == 0
            ? "Anomaly detection: CLEAN — no memory leak, handle leak, or DB growth anomaly detected."
            : "Anomaly detection: ANOMALIES FOUND:\n  " + string.Join("\n  ", anomalies));

        var status = anomalies.Count == 0 ? "PASS" : "FAIL";

        Console.WriteLine($"[LONG RUN TEST] {status} — {stopwatch.Elapsed.TotalSeconds:F1}s elapsed");

        return new TestResult
        {
            Name     = "Long Run Test (10,000 Blocks)",
            Status   = status,
            Duration = stopwatch.Elapsed,
            Evidence = sb.ToString().Replace("\r\n", "<br>").Replace("\n", "<br>")
        };
    }

    // ─── helpers ─────────────────────────────────────────────────────────────
    private static Key MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new Key(bytes);
    }

    private static long FileKb(string path)
    {
        try { return new FileInfo(path).Length / 1024; }
        catch { return 0; }
    }

    private static TestResult Fail(Stopwatch sw, string reason)
    {
        sw.Stop();
        Console.WriteLine($"[LONG RUN TEST] FAIL — {reason}");
        return new TestResult
        {
            Name     = "Long Run Test (10,000 Blocks)",
            Status   = "FAIL",
            Duration = sw.Elapsed,
            Evidence = reason
        };
    }
}
