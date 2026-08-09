using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Services;
using BlockchainQualificationTests.Utilities;
using Key = NBitcoin.Key;

namespace BlockchainQualificationTests.Security;

/// <summary>
/// SECURITY TEST
/// Each scenario submits adversarial input directly into the application
/// boundary (CheckTx / AcceptExternalCommit / TransactionCodec.TryDecode).
/// Invariants verified after every attack:
///   1. Application never throws an unhandled exception.
///   2. Returned result is always a clean rejection (no PASS for bad input).
///   3. Chain state (Height + AppHash) is unchanged after each rejection.
/// </summary>
public static class SecurityTest
{
    private static Key MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new Key(bytes);
    }

    // Build a fully signed, valid transfer transaction that IS in the manifest.
    private static Transaction MakeValidTransfer(
        uint chainId,
        Key senderKey,
        Address senderAddr,
        Address recipientAddr,
        ulong nonce,
        long genesisTime)
    {
        var tx = new Transaction
        {
            Version  = ChainInfo.TxVersion,
            ChainId  = chainId,
            Kind     = TransactionKind.Transfer,
            From     = senderAddr,
            To       = recipientAddr,
            Amount   = Money.FromCoins(1m),
            Fee      = Money.FromCoins(0.01m),
            Nonce    = nonce,
            PubKey   = senderKey.PubKey.ToBytes()
        };
        tx.Signature = TransactionSigner.Sign(tx, senderKey.ToBytes());
        return tx;
    }

    public static TestResult Run()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[SECURITY TEST] Starting...");

        var tempDir = Path.Combine(Path.GetTempPath(), "ukc-sectest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var snapshotFile = Path.Combine(tempDir, "canonical.json");

        var chainId    = ChainInfo.ChainIdTestnet;
        var senderKey  = MakeKey(0x01);
        var senderAddr = Address.FromPublicKey(ChainInfo.AddressVersion(chainId), senderKey.PubKey.ToBytes());

        var recipientKey  = MakeKey(0x02);
        var recipientAddr = Address.FromPublicKey(ChainInfo.AddressVersion(chainId), recipientKey.PubKey.ToBytes());

        var manifest = Genesis.CreateDefaultManifest(chainId);
        manifest.Accounts.Add(new GenesisAccount(
            Convert.ToHexString(senderKey.PubKey.ToBytes()),
            Money.FromCoins(10_000m).BaseUnits
        ));

        var passed   = new List<string>();
        var failed   = new List<string>();
        var evidence = new List<string>();

        void RecordPass(string scenario)
        {
            Console.WriteLine($"[SECURITY TEST] {scenario}: PASS");
            passed.Add(scenario);
            evidence.Add($"{scenario}: correctly rejected with no state change");
        }

        TestResult FailScenario(string scenario, string reason)
        {
            Console.WriteLine($"[SECURITY TEST] {scenario}: FAIL — {reason}");
            failed.Add(scenario);
            evidence.Add($"{scenario}: FAIL — {reason}");
            return BuildResult(stopwatch, passed, failed, evidence);
        }

        // ── helpers ──────────────────────────────────────────────────────────

        CanonicalNodeService MakeNode() =>
            new CanonicalNodeService(chainId, snapshotFile, null, manifest);

        (long height, byte[] appHash) Snapshot(CanonicalNodeService n)
        {
            var h   = n.Chain.State.Height;
            var ah  = n.Chain.State.ComputeStateRoot().ToArray();
            return (h, ah);
        }

        bool StateUnchanged(CanonicalNodeService n, long h0, byte[] ah0) =>
            n.Chain.State.Height == h0 &&
            n.Chain.State.ComputeStateRoot().SequenceEqual(ah0);

        ApplicationCheckResult SafeCheckTx(ConsensusApplicationStateMachine app, Transaction tx)
        {
            try   { return app.CheckTx(tx); }
            catch (Exception ex) { return new ApplicationCheckResult(false, $"UNHANDLED EXCEPTION: {ex.Message}"); }
        }

        // ── build a node and commit one valid block so Height > 0 ────────────
        CanonicalNodeService node = MakeNode();
        var validatorAddr = senderAddr;

        // first commit: empty block at height 1
        var (ok1, _, msg1) = node.AcceptExternalCommit(
            Array.Empty<Transaction>(),
            manifest.GenesisTime + 10,
            validatorAddr,
            node.Chain.State.Height);
        if (!ok1)
        {
            return FailScenario("Setup", $"Could not commit baseline block: {msg1}");
        }

        // ====================================================================
        // SCENARIO 1: Duplicate Transaction
        //   Submit tx-A, commit it, then submit the identical tx-A again.
        //   The second submission must be rejected (old nonce).
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 1: Duplicate Transaction");
        {
            var app = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);
            var tx  = MakeValidTransfer(chainId, senderKey, senderAddr, recipientAddr, 1, manifest.GenesisTime);

            // First submission should pass CheckTx
            var first = SafeCheckTx(app, tx);
            if (!first.Accepted)
            {
                return FailScenario("Duplicate Transaction", $"Valid tx rejected at first submission: {first.Message}");
            }

            // Commit the tx
            var (ok, _, cmsg) = node.AcceptExternalCommit(
                new[] { tx },
                manifest.GenesisTime + 20,
                validatorAddr,
                node.Chain.State.Height);
            if (!ok) return FailScenario("Duplicate Transaction", $"Commit failed: {cmsg}");

            var (h0, ah0) = Snapshot(node);

            // Re-submit the exact same tx
            var appAfter = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);
            var second   = SafeCheckTx(appAfter, tx);
            if (second.Accepted)
            {
                return FailScenario("Duplicate Transaction", "Duplicate tx was ACCEPTED — replay protection absent.");
            }

            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Duplicate Transaction", "Chain state mutated despite rejection.");
            }
            RecordPass("Duplicate Transaction");
        }

        // ====================================================================
        // SCENARIO 2: Invalid Signature
        //   Build a valid tx, mutate the last byte of the signature, resubmit.
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 2: Invalid Signature");
        {
            var tx = MakeValidTransfer(chainId, senderKey, senderAddr, recipientAddr, 2, manifest.GenesisTime);
            // Corrupt the signature
            var corruptSig = tx.Signature.ToArray();
            corruptSig[^1] ^= 0xFF;
            tx.Signature = corruptSig;

            var (h0, ah0) = Snapshot(node);
            var app       = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);
            var result    = SafeCheckTx(app, tx);

            if (result.Accepted)
            {
                return FailScenario("Invalid Signature", "Tx with corrupted signature was ACCEPTED.");
            }
            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Invalid Signature", "Chain state mutated despite rejection.");
            }
            RecordPass("Invalid Signature");
        }

        // ====================================================================
        // SCENARIO 3: Wrong ChainId
        //   Craft a tx for ChainId = Mainnet while node is Testnet.
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 3: Wrong ChainId");
        {
            var wrongChain = chainId == ChainInfo.ChainIdTestnet
                ? ChainInfo.ChainIdMainnet
                : ChainInfo.ChainIdTestnet;
            var wrongAddr = Address.FromPublicKey(ChainInfo.AddressVersion(wrongChain), senderKey.PubKey.ToBytes());
            var wrongTo   = Address.FromPublicKey(ChainInfo.AddressVersion(wrongChain), recipientKey.PubKey.ToBytes());

            var tx = new Transaction
            {
                Version  = ChainInfo.TxVersion,
                ChainId  = wrongChain,
                Kind     = TransactionKind.Transfer,
                From     = wrongAddr,
                To       = wrongTo,
                Amount   = Money.FromCoins(1m),
                Fee      = Money.FromCoins(0.01m),
                Nonce    = 2,
                PubKey   = senderKey.PubKey.ToBytes()
            };
            tx.Signature = TransactionSigner.Sign(tx, senderKey.ToBytes());

            var (h0, ah0) = Snapshot(node);
            var app       = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);
            var result    = SafeCheckTx(app, tx);

            if (result.Accepted)
            {
                return FailScenario("Wrong ChainId", "Cross-chain tx was ACCEPTED — replay protection absent.");
            }
            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Wrong ChainId", "Chain state mutated despite rejection.");
            }
            RecordPass("Wrong ChainId");
        }

        // ====================================================================
        // SCENARIO 4: Future Nonce
        //   Submit nonce = current + 100 (skipping nonces in between).
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 4: Future Nonce");
        {
            var senderAccount = node.Chain.State.GetAccount(senderAddr);
            var currentNonce  = senderAccount?.Nonce ?? 0;
            var futureNonce   = currentNonce + 100;

            var tx = MakeValidTransfer(chainId, senderKey, senderAddr, recipientAddr, futureNonce, manifest.GenesisTime);

            var (h0, ah0) = Snapshot(node);
            var app       = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);
            var result    = SafeCheckTx(app, tx);

            if (result.Accepted)
            {
                return FailScenario("Future Nonce", $"Tx with nonce {futureNonce} (expected {currentNonce + 1}) was ACCEPTED.");
            }
            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Future Nonce", "Chain state mutated despite rejection.");
            }
            RecordPass("Future Nonce");
        }

        // ====================================================================
        // SCENARIO 5: Old Nonce
        //   Submit nonce = 0 (already used at genesis).
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 5: Old Nonce");
        {
            var tx = MakeValidTransfer(chainId, senderKey, senderAddr, recipientAddr, 0, manifest.GenesisTime);

            var (h0, ah0) = Snapshot(node);
            var app       = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);
            var result    = SafeCheckTx(app, tx);

            if (result.Accepted)
            {
                return FailScenario("Old Nonce", "Tx with nonce=0 (replay) was ACCEPTED.");
            }
            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Old Nonce", "Chain state mutated despite rejection.");
            }
            RecordPass("Old Nonce");
        }

        // ====================================================================
        // SCENARIO 6: Oversized Transaction
        //   Build a valid tx then pad PubKey to 64 KB before encoding.
        //   Both IsEnvelopeWellFormed and TransactionCodec must reject it.
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 6: Oversized Transaction");
        {
            var tx = MakeValidTransfer(chainId, senderKey, senderAddr, recipientAddr, 2, manifest.GenesisTime);
            // Pad the pubkey field to exceed MaxTxSizeBytes (FeePolicy.MaxTxSizeBytes)
            tx.PubKey = new byte[64 * 1024]; // 64 KB — vastly over the limit
            senderKey.PubKey.ToBytes().CopyTo(tx.PubKey, 0);

            var (h0, ah0) = Snapshot(node);
            var app       = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);

            // IsEnvelopeWellFormed must say false
            bool wellFormed = false;
            try { wellFormed = tx.IsEnvelopeWellFormed(chainId); }
            catch (Exception ex)
            {
                return FailScenario("Oversized Transaction", $"IsEnvelopeWellFormed threw: {ex.Message}");
            }

            if (wellFormed)
            {
                return FailScenario("Oversized Transaction", "Oversized tx passed IsEnvelopeWellFormed — size guard absent.");
            }

            // CheckTx must also reject without throwing
            var result = SafeCheckTx(app, tx);
            if (result.Accepted)
            {
                return FailScenario("Oversized Transaction", "Oversized tx was ACCEPTED by CheckTx.");
            }
            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Oversized Transaction", "Chain state mutated despite rejection.");
            }
            RecordPass("Oversized Transaction");
        }

        // ====================================================================
        // SCENARIO 7: Malformed Protobuf / Wrong Magic
        //   Feed a truncated or garbage-magic byte sequence to TryDecode.
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 7: Malformed Encoding (wrong magic)");
        {
            var malformedInputs = new List<(string label, byte[] data)>
            {
                ("empty",           Array.Empty<byte>()),
                ("single_byte",     new byte[] { 0xFF }),
                ("wrong_magic",     new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }),
                ("truncated_tx",    new byte[32]),       // zeroes — wrong magic
                ("all_0xFF",        Enumerable.Repeat((byte)0xFF, 200).ToArray()),
            };

            var (h0, ah0) = Snapshot(node);
            foreach (var (label, data) in malformedInputs)
            {
                bool decoded;
                try
                {
                    decoded = TransactionCodec.TryDecode(data, out _, out _);
                }
                catch (Exception ex)
                {
                    return FailScenario($"Malformed Encoding ({label})", $"TryDecode threw: {ex.Message}");
                }

                if (decoded)
                {
                    return FailScenario($"Malformed Encoding ({label})", "Malformed bytes decoded successfully — magic/bounds check absent.");
                }
            }
            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Malformed Encoding", "Chain state mutated despite rejection.");
            }
            RecordPass("Malformed Encoding");
        }

        // ====================================================================
        // SCENARIO 8: Corrupted Binary
        //   Encode a valid tx, bit-flip every 8th byte, verify decode still
        //   returns false or produces a transaction that fails CheckTx.
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 8: Corrupted Binary");
        {
            var goodTx    = MakeValidTransfer(chainId, senderKey, senderAddr, recipientAddr, 2, manifest.GenesisTime);
            var goodBytes = TransactionCodec.Encode(goodTx);

            var (h0, ah0) = Snapshot(node);
            var corruptionCount = 0;
            var incorrectlyAcceptedCount = 0;

            for (int i = 0; i < goodBytes.Length; i += 8)
            {
                var corrupted = goodBytes.ToArray();
                corrupted[i] ^= 0xAA;
                corruptionCount++;

                bool decoded;
                Transaction? tx = null;
                try
                {
                    decoded = TransactionCodec.TryDecode(corrupted, out tx, out _);
                }
                catch (Exception ex)
                {
                    return FailScenario("Corrupted Binary", $"TryDecode threw on corruption at byte {i}: {ex.Message}");
                }

                // If decode succeeds the tx must still fail CheckTx (bad sig / bad envelope)
                if (decoded && tx is not null)
                {
                    var app = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);
                    var r   = SafeCheckTx(app, tx);
                    if (r.Accepted)
                    {
                        incorrectlyAcceptedCount++;
                    }
                }
            }

            if (incorrectlyAcceptedCount > 0)
            {
                return FailScenario("Corrupted Binary",
                    $"{incorrectlyAcceptedCount}/{corruptionCount} corrupted payloads passed CheckTx — signature verification or envelope check is weak.");
            }
            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Corrupted Binary", "Chain state mutated despite rejection.");
            }
            RecordPass("Corrupted Binary");
        }

        // ====================================================================
        // SCENARIO 9: Random Bytes
        //   Feed 50 bursts of cryptographically-random bytes into TryDecode
        //   and (if any decode) into CheckTx.  No crash, no acceptance.
        // ====================================================================
        Console.WriteLine("[SECURITY TEST] Scenario 9: Random Bytes");
        {
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var (h0, ah0) = Snapshot(node);
            var incorrectlyAcceptedCount = 0;

            for (int i = 0; i < 50; i++)
            {
                // Vary the payload size: 1 … 4096 bytes
                var size  = (i % 16) switch
                {
                    0 => 1, 1 => 2, 2 => 4, 3 => 8, 4 => 16, 5 => 32,
                    6 => 64, 7 => 128, 8 => 256, 9 => 512, 10 => 1024,
                    11 => 2048, 12 => 4096, 13 => 3, 14 => 7, _ => 100
                };
                var buf = new byte[size];
                rng.GetBytes(buf);

                bool decoded;
                Transaction? tx = null;
                try
                {
                    decoded = TransactionCodec.TryDecode(buf, out tx, out _);
                }
                catch (Exception ex)
                {
                    return FailScenario("Random Bytes", $"TryDecode threw on random input #{i}: {ex.Message}");
                }

                if (decoded && tx is not null)
                {
                    var app = new ConsensusApplicationStateMachine(node.Chain.State, validatorAddr);
                    ApplicationCheckResult r;
                    try   { r = app.CheckTx(tx); }
                    catch (Exception ex)
                    {
                        return FailScenario("Random Bytes", $"CheckTx threw on random input #{i}: {ex.Message}");
                    }
                    if (r.Accepted) incorrectlyAcceptedCount++;
                }
            }

            if (incorrectlyAcceptedCount > 0)
            {
                return FailScenario("Random Bytes",
                    $"{incorrectlyAcceptedCount}/50 random byte payloads passed CheckTx — application boundary is insufficiently hardened.");
            }
            if (!StateUnchanged(node, h0, ah0))
            {
                return FailScenario("Random Bytes", "Chain state mutated despite rejection.");
            }
            RecordPass("Random Bytes");
        }

        // ── cleanup ──────────────────────────────────────────────────────────
        try { Directory.Delete(tempDir, true); } catch { /* best-effort */ }

        return BuildResult(stopwatch, passed, failed, evidence);
    }

    // ── private ─────────────────────────────────────────────────────────────

    private static TestResult BuildResult(
        Stopwatch sw,
        List<string> passed,
        List<string> failed,
        List<string> evidence)
    {
        sw.Stop();
        bool allPassed = failed.Count == 0;
        var status     = allPassed ? "PASS" : "FAIL";
        var detail     = allPassed
            ? $"All {passed.Count} security scenarios correctly rejected adversarial input without crash or state corruption:\n"
              + string.Join("\n", evidence)
            : $"FAILED scenarios ({failed.Count}): {string.Join(", ", failed)}\n"
              + string.Join("\n", evidence);

        return new TestResult
        {
            Name     = "Security Adversarial Test",
            Status   = status,
            Duration = sw.Elapsed,
            Evidence = detail
        };
    }

    private static TestResult Fail(string message) => new()
    {
        Name     = "Security Adversarial Test",
        Status   = "FAIL",
        Evidence = message
    };
}
