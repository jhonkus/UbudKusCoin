// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Security.Cryptography;
using UbudKusCoin.P2P;
using UbudKusCoin.Core.Consensus;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Services
{
    public static class ServicePool
    {
        public static MintingService MintingService { set; get; }
        public static DbService DbService { set; get; }
        public static FacadeService FacadeService { set; get; }
        public static WalletService WalletService { set; get; }
        public static P2PService P2PService { set; get; }
        public static CanonicalNodeService CanonicalNodeService { get; private set; }
        public static IConsensusEngineAdapter ConsensusEngine { get; private set; }
        public static ConsensusApplicationStateMachine ApplicationStateMachine { get; private set; }
        public static BlockCommitService BlockCommitService { get; } = new();

        public static void Add(
            WalletService wallet,
            DbService db,
            FacadeService facade,
            MintingService minter,
            P2PService p2p)
        {
            WalletService = wallet;
            DbService = db;
            FacadeService = facade;
            MintingService = minter;
            P2PService = p2p;
        }

        public static void Start()
        {
            WalletService.Start();
            var chainId = DotNetEnv.Env.GetInt("CHAIN_ID");
            if (chainId == 0)
            {
                chainId = (int)ChainInfo.ChainIdTestnet;
            }

            var genesisPath = DotNetEnv.Env.GetString("GENESIS_MANIFEST_PATH", string.Empty);
            GenesisManifest genesisManifest = null;
            if (!string.IsNullOrWhiteSpace(genesisPath))
            {
                var expectedManifestHash = DotNetEnv.Env.GetString("GENESIS_MANIFEST_SHA256", string.Empty);
                if (!string.IsNullOrWhiteSpace(expectedManifestHash))
                {
                    var actualManifestHash = Convert.ToHexString(
                        SHA256.HashData(File.ReadAllBytes(genesisPath)));
                    if (!actualManifestHash.Equals(expectedManifestHash.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException("Genesis manifest SHA-256 does not match the configured pin.");
                    }
                }

                genesisManifest = GenesisManifest.Load(genesisPath, (uint)chainId);
            }

            var consensusOptions = ConsensusEngineOptions.FromEnvironment();
            ConsensusEngine = ConsensusEngineFactory.Create(consensusOptions);
            if (consensusOptions.Mode == ConsensusEngineMode.CometBft
                && consensusOptions.KeyCustodyMode == ValidatorKeyCustodyMode.ExternalSigner)
            {
                var reachabilityTimeout = TimeSpan.FromSeconds(
                    Math.Min(Math.Max(consensusOptions.StartupTimeoutSeconds, 1), 5));
                ExternalSignerConnectivity.EnsureReachableAsync(
                        consensusOptions.ExternalSignerAddress,
                        reachabilityTimeout)
                    .GetAwaiter()
                    .GetResult();
            }
            var validatorKey = consensusOptions.Mode == ConsensusEngineMode.CometBft
                ? CometBftValidatorKeyLoader.LoadConfiguredPublicKey(
                    consensusOptions.KeyCustodyMode == ValidatorKeyCustodyMode.ExternalSigner)
                : WalletService.GetPublicKey().PubKey.ToBytes();
            if (validatorKey.Length == 0)
            {
                throw new InvalidOperationException(
                    "A CometBFT validator public key is required when CONSENSUS_ENGINE=cometbft.");
            }

            var validatorSet = ConsensusValidatorConfig.Load((uint)chainId, WalletService, validatorKey);
            CanonicalNodeService = new CanonicalNodeService((uint)chainId, @"DbFiles/canonical-chain.json", validatorSet,
                genesisManifest);
            if (consensusOptions.Mode == ConsensusEngineMode.CometBft
                && !CometBftValidatorKeyLoader.IsGenesisOrActiveConsensusKey(
                    validatorKey, CanonicalNodeService.Chain.State))
            {
                throw new InvalidDataException(
                    "The CometBFT validator key is neither a genesis identity nor active in persisted staking state.");
            }
            var validator = Address.FromPublicKey(ChainInfo.AddressVersion((uint)chainId), validatorKey);
            ApplicationStateMachine = new ConsensusApplicationStateMachine(
                CanonicalNodeService.Chain.State,
                validator,
                validatorPublicKey: validatorKey);
            DbService.Start();
            FacadeService.start();
            P2PService.Start();
            MintingService.Start();
        }

        public static void Stop()
        {
            //stop when application exit
            //WalletService.Stop();
            DbService.Stop();
            //FacadeService.Stop();
            //P2PService.Stop();
            MintingService.Stop();
        }
    }
}
