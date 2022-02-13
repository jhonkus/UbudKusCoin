using System.Linq;
// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

using UbudKusCoin.Others;
using UbudKusCoin.Services;
using UbudKusCoin.Grpc;
using Newtonsoft.Json;


namespace UbudKusCoin.Facade
{
    class Inventory
    {
        public string AddrFrom { set; get; }
        public string Type { set; get; }
        public IList<string> Items { set; get; }
    }


    public class PeerFacade
    {

        public List<Peer> peers { set; get; }
        public PeerFacade()
        {
            this.peers = new List<Peer>();

            Initialize();
            Console.WriteLine("...... Peer initilized.");
        }

        internal void Initialize()
        {

            if (this.peers.Count() < 1)
            {
                var bootstrapPeers = DotNetEnv.Env.GetString("BOOTSRTAP_PEERS");
                bootstrapPeers.Replace(" ", "");
                var tempPeers = bootstrapPeers.Split(",");
                for (int i = 0; i < tempPeers.Length; i++)
                {
                    this.peers.Add(new Grpc.Peer
                    {
                        Address = tempPeers[i],
                        IsBootstrap = true,
                        IsCanreach = false,
                        LastReach = Utils.GetTime()
                    });
                }
            }
        }

        public NodeState GetNodeState()
        {
            var lastBlock = ServicePool.DbService.blockDb.GetLast();
            Console.WriteLine(" lastBlock Hash {0}", lastBlock.Hash);


            Console.WriteLine(" peers {0}", this.peers.Count());

            var nodeState = new Grpc.NodeState
            {
                Version = Constants.VERSION,
                Height = lastBlock.Height,
                Address = ServicePool.P2PService.nodeAddress,
                Hash = lastBlock.Hash
            };
            nodeState.KnownPeers.AddRange(this.peers);
            
            return nodeState;
        }

    }
}
