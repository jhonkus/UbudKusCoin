// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
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

            var validatorSet = ConsensusValidatorConfig.Load((uint)chainId, WalletService);
            var consensusOptions = ConsensusEngineOptions.FromEnvironment();
            ConsensusEngine = ConsensusEngineFactory.Create(consensusOptions);
            CanonicalNodeService = new CanonicalNodeService((uint)chainId, @"DbFiles/canonical-chain.json", validatorSet);
            var validatorKey = WalletService.GetPublicKey().PubKey.ToBytes();
            var validator = Address.FromPublicKey(ChainInfo.AddressVersion((uint)chainId), validatorKey);
            ApplicationStateMachine = new ConsensusApplicationStateMachine(CanonicalNodeService.Chain.State, validator);
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
