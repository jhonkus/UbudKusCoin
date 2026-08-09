using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UbudKusCoin.Core.Types;
using BlockchainQualificationTests.Utilities;

namespace BlockchainQualificationTests.Upgrade;

public static class UpgradeTest
{
    public static TestResult Run()
    {
        var stopwatch = Stopwatch.StartNew();
        Console.WriteLine("[UPGRADE TEST] Starting...");

        try
        {
            var chainId = ChainInfo.ChainIdTestnet;
            var state = Genesis.CreateState(chainId);
            var genesisBlock = Genesis.CreateBlock(chainId);

            // Apply genesis
            var genesisResult = StateTransition.ApplyCommittedBlock(state, genesisBlock);
            if (!genesisResult.Success)
            {
                return new TestResult
                {
                    Name = "Upgrade Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Failed to apply genesis block: {genesisResult.Error}"
                };
            }

            var nextState = genesisResult.NewState!;

            // 1. Test block with version N+1 (which is 2)
            var validatorKeyBytes = Convert.FromHexString(Genesis.CreateDefaultManifest(chainId).ValidatorPublicKeyHex);
            var validatorAddress = Address.FromPublicKey(ChainInfo.AddressVersion(chainId), validatorKeyBytes);

            var invalidBlock = new Block
            {
                Version = ChainInfo.TxVersion + 1, // Version 2 (Incompatible)
                ChainId = chainId,
                Height = nextState.Height + 1,
                TimeStamp = nextState.TimeStamp + 10,
                PrevHash = nextState.Head,
                MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>()),
                Validator = validatorAddress,
                Txs = new List<Transaction>()
            };
            invalidBlock.StateRoot = nextState.ComputeStateRoot();

            var invalidResult = StateTransition.ApplyCommittedBlock(nextState, invalidBlock);
            if (invalidResult.Success)
            {
                return new TestResult
                {
                    Name = "Upgrade Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = "Expected state machine to reject block version N+1 (Version 2) but it succeeded."
                };
            }

            Console.WriteLine($"[UPGRADE TEST] Block version 2 rejected as expected. Error: {invalidResult.Error}");

            // 2. Test block with compatible version N (which is 1)
            var validBlock = new Block
            {
                Version = ChainInfo.TxVersion, // Version 1 (Compatible)
                ChainId = chainId,
                Height = nextState.Height + 1,
                TimeStamp = nextState.TimeStamp + 10,
                PrevHash = nextState.Head,
                MerkleRoot = Merkle.ComputeRoot(Array.Empty<byte[]>()),
                Validator = validatorAddress,
                Txs = new List<Transaction>()
            };
            validBlock.StateRoot = nextState.ComputeStateRoot();

            var validResult = StateTransition.ApplyCommittedBlock(nextState, validBlock);
            if (!validResult.Success)
            {
                return new TestResult
                {
                    Name = "Upgrade Test",
                    Status = "FAIL",
                    Duration = stopwatch.Elapsed,
                    Evidence = $"Expected block version 1 to succeed but failed: {validResult.Error}"
                };
            }

            Console.WriteLine("[UPGRADE TEST] Block version 1 accepted successfully.");

            return new TestResult
            {
                Name = "Upgrade Test",
                Status = "PASS",
                Duration = stopwatch.Elapsed,
                Evidence = $"Protocol and application versions correctly enforced by state transition engine.\n- Block version {ChainInfo.TxVersion} accepted.\n- Block version {ChainInfo.TxVersion + 1} correctly rejected with: '{invalidResult.Error}'"
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = "Upgrade Test",
                Status = "FAIL",
                Duration = stopwatch.Elapsed,
                Evidence = $"Upgrade test threw exception: {ex.Message}"
            };
        }
    }
}
