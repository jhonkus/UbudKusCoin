// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;
using Grpc.Net.Client;
using static UbudKusCoin.Grpc.PeerService;
using static UbudKusCoin.Grpc.BlockService;
using static UbudKusCoin.Grpc.TransactionService;
using static UbudKusCoin.Grpc.StakeService;

namespace UbudKusCoin.P2P
{

    /// <summary>
    /// This class for communicating with other peer, such as to broadcasting block,
    /// broadcasting transaction, downloading block.
    /// </summary>
    public class P2PService
    {

        public P2PService()
        {

        }

        public void Start()
        {
            Console.WriteLine("... P2P service is starting");
            // do some task
            Console.WriteLine("...... P2P service is ready");
        }



        /// <summary>
        /// Do Braodcast a block to all peer in known peers
        /// </summary>
        /// <param name="block"></param>
        public void BroadcastBlock(Block block)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;

            Parallel.ForEach(knownPeers, peer =>
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                var blockService = new BlockServiceClient(channel);
                var response = blockService.Add(block);
            });
        }



        /// <summary>
        /// Do Broadcast a stake to all peer in known peers
        /// </summary>
        /// <param name="stake"></param>
        public void BroadcastStake(Stake stake)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;
            Parallel.ForEach(knownPeers, peer =>
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                var stakeService = new StakeServiceClient(channel);
                stakeService.Add(stake);
            });
        }

        /// <summary>
        /// Do broadcast a transaction to all peer in known peers
        /// </summary>
        /// <param name="tx"></param>
        public void BroadcastTransaction(Transaction tx)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;
            Parallel.ForEach(knownPeers, peer =>
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                var txnService = new TransactionServiceClient(channel);
                var response = txnService.Receive(new TransactionPost
                {
                    SendingFrom = nodeAddress,
                    Transaction = tx
                });
            });

        }


        /// <summary>
        /// Sincronizing blocks from all peer in known peers
        /// </summary>
        /// <param name="blockService"></param>
        /// <param name="lastBlockHeight"></param>
        /// <param name="peerHeight"></param>
        private void DownloadBlocks(BlockServiceClient blockService, long lastBlockHeight, long peerHeight)
        {
            try
            {
                var response = blockService.GetRemains(new StartingParam { Height = lastBlockHeight });
                List<Block> blocks = response.Blocks.ToList();
                blocks.Reverse();

                var lastHeight = 0L;
                foreach (var block in blocks)
                {
                    Console.WriteLine("==== Block" + block.Height);
                    ServicePool.DbService.blockDb.Add(block);
                    lastHeight = block.Height;
                }

                if (lastHeight < peerHeight)
                {
                    DownloadBlocks(blockService, lastHeight, peerHeight);
                }
            }
            catch { }
        }

        /// <summary>
        /// Checking in db if new peer already in DB
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private bool IsNewPeer(string address)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            foreach (var peer in knownPeers)
            {
                if (address == peer.Address)
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Sincronize blockchain states, make block height same with other peer
        /// </summary>
        public void SyncState()
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;

            //synchronizing peer
            foreach (var peer in knownPeers)
            {
                if (!nodeAddress.Equals(peer.Address))
                {
                    try
                    {
                        GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                        var peerService = new PeerServiceClient(channel);
                        var peerState = peerService.GetNodeState(new NodeParam { NodeIpAddress = nodeAddress });

                        // add peer to db
                        foreach (var newPeer in peerState.KnownPeers)
                        {
                            ServicePool.FacadeService.Peer.Add(newPeer);
                        }
                    }
                    catch { }
                }
            }

            // synchronizing blocks
            knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            foreach (var peer in knownPeers)
            {
                if (!nodeAddress.Equals(peer.Address))
                {
                    try
                    {
                        GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                        var peerService = new PeerServiceClient(channel);
                        var peerState = peerService.GetNodeState(new NodeParam { NodeIpAddress = nodeAddress });

                        // local block height
                        var lastBlockHeight = ServicePool.DbService.blockDb.GetLast().Height;
                        var blockService = new BlockServiceClient(channel);
                        if (lastBlockHeight < peerState.Height)
                        {
                            DownloadBlocks(blockService, lastBlockHeight, peerState.Height);
                        }
                    }
                    catch { }
                }
            }
            Console.WriteLine("---- Sync Done~");
        }

    }



}
