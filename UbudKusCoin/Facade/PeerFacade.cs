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
            if (!PeerIdentityPolicy.TryNormalizeEndpoint(NodeAddress, out _, out var nodeError))
            {
                throw new InvalidOperationException($"NODE_ADDRESS is invalid: {nodeError}");
            }

            var KnowPeers = ServicePool.DbService.PeerDb.GetAll();
            if (KnowPeers.Count() < 1)
            {
                InitialPeers = new List<Peer>();
                var bootstrapPeers = DotNetEnv.Env.GetString("BOOTSRTAP_PEERS", string.Empty)
                    .Replace(" ", "", StringComparison.Ordinal);
                var tempPeers = bootstrapPeers.Length == 0
                    ? Array.Empty<string>()
                    : bootstrapPeers.Split(",", StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < tempPeers.Length; i++)
                {
                    var newPeer = new Peer
                    {
                        Address = tempPeers[i],
                        IsBootstrap = true,
                        IsCanreach = false,
                        LastReach = UkcUtils.GetTime()
                    };

                    ServicePool.DbService.PeerDb.Add(newPeer, out _);
                    InitialPeers.Add(newPeer);
                }
            }
        }

        public List<Peer> GetKnownPeers()
        {
            var now = UkcUtils.GetTime();
            var peers = ServicePool.DbService.PeerDb.GetAll().FindAll().ToList();
            return PeerAdmissionPolicy.OrderPeers(peers, now)
                .Take(PeerAdmissionPolicy.GetMaxKnownPeers())
                .ToList();
        }

        public NodeState GetNodeState()
        {
            var lastBlock = ServicePool.DbService.BlockDb.GetLast();
            if (lastBlock is null)
            {
                return new NodeState
                {
                    Version = Constants.VERSION,
                    Height = 0,
                    Address = NodeAddress,
                    Hash = string.Empty
                };
            }

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

        public bool Add(Peer peer, out string message)
        {
            if (!PeerIdentityPolicy.TryNormalizeEndpoint(peer?.Address, out _, out message))
            {
                return false;
            }

            if (PeerIdentityPolicy.IsSelfEndpoint(peer.Address, NodeAddress))
            {
                message = "Refusing to add the local node as a peer.";
                return false;
            }

            peer.LastReach = UkcUtils.GetTime();
            peer.TimeStamp = peer.TimeStamp == 0 ? peer.LastReach : peer.TimeStamp;
            return ServicePool.DbService.PeerDb.Add(peer, out message);
        }

        public bool TouchKnownPeer(string address, out string message)
        {
            message = string.Empty;
            if (!PeerIdentityPolicy.TryNormalizeEndpoint(address, out _, out message))
            {
                return false;
            }

            var knownPeer = ServicePool.DbService.PeerDb.GetByAddress(address);
            if (knownPeer is null)
            {
                message = "Peer is not registered.";
                return false;
            }

            knownPeer.LastReach = UkcUtils.GetTime();
            knownPeer.IsCanreach = true;
            knownPeer.TimeStamp = knownPeer.TimeStamp == 0 ? knownPeer.LastReach : knownPeer.TimeStamp;
            ServicePool.DbService.PeerDb.Add(knownPeer, out _);
            message = "Peer reach updated.";
            return true;
        }
    }
}
