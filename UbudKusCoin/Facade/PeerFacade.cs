// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Linq;
using System;
using System.Collections.Generic;
using UbudKusCoin.Others;
using UbudKusCoin.Services;
using UbudKusCoin.Grpc;

namespace UbudKusCoin.Facade
{
    public class Inventory
    {
        public string Type { set; get; }
        public IList<string> Items { set; get; }
    }

    public class PeerFacade
    {
        public string NodeAddress { get; set; }
        public List<Peer> InitialPeers { get; set; }

        public PeerFacade()
        {
            Initialize();
            Console.WriteLine("...... Peer innitialized.");
        }

        internal void Initialize()
        {
            NodeAddress = DotNetEnv.Env.GetString("NODE_ADDRESS");
            var KnowPeers = ServicePool.DbService.PeerDb.GetAll();
            if (KnowPeers.Count() < 1)
            {
                InitialPeers = new List<Peer>();
                var bootstrapPeers = DotNetEnv.Env.GetString("BOOTSRTAP_PEERS").Replace(" ", "");
                var tempPeers = bootstrapPeers.Split(",");
                
                for (int i = 0; i < tempPeers.Length; i++)
                {
                    var newPeer = new Peer
                    {
                        Address = tempPeers[i],
                        IsBootstrap = true,
                        IsCanreach = false,
                        LastReach = UkcUtils.GetTime()
                    };
                    
                    ServicePool.DbService.PeerDb.Add(newPeer);
                    InitialPeers.Add(newPeer);
                }
            }
        }

        public List<Peer> GetKnownPeers()
        {
            return ServicePool.DbService.PeerDb.GetAll().FindAll().ToList();
        }

        public NodeState GetNodeState()
        {
            var lastBlock = ServicePool.DbService.BlockDb.GetLast();
            var nodeState = new NodeState
            {
                Version = Constants.VERSION,
                Height = lastBlock.Height,
                Address = NodeAddress,
                Hash = lastBlock.Hash
            };
            
            nodeState.KnownPeers.AddRange(GetKnownPeers());
            return nodeState;
        }

        public void Add(Peer peer)
        {
            ServicePool.DbService.PeerDb.Add(peer);
        }
    }
}