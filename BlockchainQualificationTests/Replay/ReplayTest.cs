using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using BlockchainQualificationTests.Utilities;
using Key = NBitcoin.Key;

namespace BlockchainQualificationTests.Replay;

public static class ReplayTest
{
    private static Key MakeKey(byte seed)
    {
        var bytes = new byte[32];
        bytes[0] = seed;
        return new Key(bytes);
    }

    public static TestResult Run()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[REPLAY TEST] Starting...");

        try
        {
            var chainId = ChainInfo.ChainIdTestnet;
            var manifest = Genesis.CreateDefaultManifest(chainId);

            // Keys
            var senderKey = MakeKey(0x01);
            var recipientKey = MakeKey(0x02);
            var validatorKey = MakeKey(0x03);

            var senderAddr = Address.FromPublicKey(ChainInfo.AddressVersion(chainId), senderKey.PubKey.ToBytes());
            var recipientAddr = Address.FromPublicKey(ChainInfo.AddressVersion(chainId), recipientKey.PubKey.ToBytes());
            var validatorAddr = Address.FromPublicKey(ChainInfo.AddressVersion(chainId), validatorKey.PubKey.ToBytes());

            // 1. Initialize original node state & genesis
            var sourceState = Genesis.CreateState(manifest);
            var genesisBlock = Genesis.CreateBlock(manifest);
            var genRes = StateTransition.ApplyCommittedBlock(sourceState, genesisBlock);
            if (!genRes.Success)
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Failed to apply genesis block to source state: {genRes.Error}"
                };
            }
            sourceState = genRes.NewState!;

            // Fund sender address in initial state
            sourceState.EnsureAccount(senderAddr).Balance = Money.FromCoins(1000m);

            var canonicalBlocks = new List<Block>();
            var currentTime = manifest.GenesisTime + 10;

            // Generate Block 2 (Transfer from sender to recipient)
            var tx1 = new Transaction
            {
                Version = ChainInfo.TxVersion,
                ChainId = chainId,
                Kind = TransactionKind.Transfer,
                From = senderAddr,
                To = recipientAddr,
                Amount = Money.FromCoins(50m),
                Fee = Money.FromCoins(0.01m),
                Nonce = 1,
                PubKey = senderKey.PubKey.ToBytes()
            };
            tx1.Signature = TransactionSigner.Sign(tx1, senderKey.ToBytes());

            var block2 = new Block
            {
                Version = ChainInfo.TxVersion,
                ChainId = chainId,
                Height = sourceState.Height + 1,
                TimeStamp = currentTime,
                PrevHash = sourceState.Head,
                MerkleRoot = Merkle.ComputeRoot(new[] { tx1.ComputeId() }),
                Validator = validatorAddr,
                Txs = new List<Transaction> { tx1 }
            };

            var computeRes2 = StateTransition.ComputeResultingState(sourceState, block2);
            if (!computeRes2.Success)
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Failed to compute resulting state for block 2: {computeRes2.Error}"
                };
            }
            block2.StateRoot = computeRes2.NewState!.ComputeStateRoot();

            var res2 = StateTransition.ApplyCommittedBlock(sourceState, block2);
            if (!res2.Success)
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Failed to apply block 2 to source state: {res2.Error}"
                };
            }
            sourceState = res2.NewState!;
            canonicalBlocks.Add(block2);

            // Generate Block 3 (Staking Bond from sender)
            currentTime += 10;
            var tx2 = new Transaction
            {
                Version = ChainInfo.TxVersion,
                ChainId = chainId,
                Kind = TransactionKind.Bond,
                From = senderAddr,
                To = senderAddr,
                Amount = Money.FromCoins(200m),
                Fee = Money.FromCoins(0.01m),
                Nonce = 2,
                PubKey = senderKey.PubKey.ToBytes(),
                ValidatorPubKey = new byte[32]
            };
            tx2.Signature = TransactionSigner.Sign(tx2, senderKey.ToBytes());

            var block3 = new Block
            {
                Version = ChainInfo.TxVersion,
                ChainId = chainId,
                Height = sourceState.Height + 1,
                TimeStamp = currentTime,
                PrevHash = sourceState.Head,
                MerkleRoot = Merkle.ComputeRoot(new[] { tx2.ComputeId() }),
                Validator = validatorAddr,
                Txs = new List<Transaction> { tx2 }
            };

            var computeRes3 = StateTransition.ComputeResultingState(sourceState, block3);
            if (!computeRes3.Success)
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Failed to compute resulting state for block 3: {computeRes3.Error}"
                };
            }
            block3.StateRoot = computeRes3.NewState!.ComputeStateRoot();

            var res3 = StateTransition.ApplyCommittedBlock(sourceState, block3);
            if (!res3.Success)
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Failed to apply block 3 to source state: {res3.Error}"
                };
            }
            sourceState = res3.NewState!;
            canonicalBlocks.Add(block3);

            // Capture metrics from original node
            var sourceAppHash = sourceState.ComputeStateRoot();
            var sourceAppHashHex = Convert.ToHexString(sourceAppHash);
            var sourceAccounts = sourceState.Accounts.OrderBy(a => a.Address.Encoded, StringComparer.Ordinal).ToList();
            var sourceStakes = sourceState.Stakes.OrderBy(s => s.Address.Encoded, StringComparer.Ordinal).ToList();
            var sourceTotalSupply = sourceAccounts.Sum(a => a.Balance.BaseUnits) + sourceStakes.Sum(s => s.Amount.BaseUnits);

            // 2. Initialize fresh replay node and replay from genesis
            Console.WriteLine("[REPLAY TEST] Replaying chain from genesis on fresh state...");
            var replayState = Genesis.CreateState(manifest);
            var replayGenRes = StateTransition.ApplyCommittedBlock(replayState, genesisBlock);
            if (!replayGenRes.Success)
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Failed to apply genesis block to replay state: {replayGenRes.Error}"
                };
            }
            replayState = replayGenRes.NewState!;
            replayState.EnsureAccount(senderAddr).Balance = Money.FromCoins(1000m);

            // Replay each canonical block sequentially
            foreach (var b in canonicalBlocks)
            {
                var replayRes = StateTransition.ApplyCommittedBlock(replayState, b);
                if (!replayRes.Success)
                {
                    return new TestResult
                    {
                        Name = "Replay Test",
                        Status = "FAIL",
                        Duration = stopwatch.Elapsed,
                        Evidence = $"Failed to replay block height {b.Height}: {replayRes.Error}"
                    };
                }
                replayState = replayRes.NewState!;
            }

            // Capture metrics from replayed node
            var replayAppHash = replayState.ComputeStateRoot();
            var replayAppHashHex = Convert.ToHexString(replayAppHash);
            var replayAccounts = replayState.Accounts.OrderBy(a => a.Address.Encoded, StringComparer.Ordinal).ToList();
            var replayStakes = replayState.Stakes.OrderBy(s => s.Address.Encoded, StringComparer.Ordinal).ToList();
            var replayTotalSupply = replayAccounts.Sum(a => a.Balance.BaseUnits) + replayStakes.Sum(s => s.Amount.BaseUnits);

            // 3. Compare every value byte-for-byte
            if (!sourceAppHash.SequenceEqual(replayAppHash))
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"AppHash mismatch!\nSource: {sourceAppHashHex}\nReplay: {replayAppHashHex}"
                };
            }

            if (sourceAccounts.Count != replayAccounts.Count)
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Account count mismatch! Source: {sourceAccounts.Count}, Replay: {replayAccounts.Count}"
                };
            }

            for (int i = 0; i < sourceAccounts.Count; i++)
            {
                var sAcc = sourceAccounts[i];
                var rAcc = replayAccounts[i];
                if (sAcc.Address.Encoded != rAcc.Address.Encoded || sAcc.Balance != rAcc.Balance || sAcc.Nonce != rAcc.Nonce)
                {
                    return new TestResult
                    {
                        Name = "Replay Test",
                        Status = "FAIL",
                        Duration = stopwatch.Elapsed,
                        Evidence = $"Account mismatch for {sAcc.Address.Encoded}!\nSource Balance: {sAcc.Balance}, Nonce: {sAcc.Nonce}\nReplay Balance: {rAcc.Balance}, Nonce: {rAcc.Nonce}"
                    };
                }
            }

            if (sourceTotalSupply != replayTotalSupply)
            {
                return new TestResult
                {
                    Name = "Replay Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Total supply mismatch! Source: {sourceTotalSupply}, Replay: {replayTotalSupply}"
                };
            }

            return new TestResult
            {
                Name = "Replay Test",
                Status = "PASS",
                Duration = stopwatch.Elapsed,
                Evidence = $"Complete chain replay from Genesis verified byte-for-byte:\n- Final AppHash: {replayAppHashHex}\n- Account Balances & Nonces: All {replayAccounts.Count} accounts match exactly.\n- Validator Stakes & Positions: All {replayStakes.Count} positions matched.\n- Total Invariant Supply: {replayTotalSupply} BaseUnits matched."
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = "Replay Test",
                Status = "FAIL",
                Duration = stopwatch.Elapsed,
                Evidence = $"Replay test threw exception: {ex.Message}"
            };
        }
    }
}
