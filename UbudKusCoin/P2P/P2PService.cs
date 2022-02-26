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
                if (!nodeAddress.Equals(peer.Address))
                {
                    Console.WriteLine("-- BroadcastBlock to {0}", peer.Address);
                    GrpcChannel channel = GrpcChannel.ForAddress(peer.Address);
                    var blockService = new BlockServiceClient(channel);
                    try
                    {
                        var response = blockService.Add(block);
                        Console.WriteLine("--- Done ");
                    }
                    catch
                    {
                        Console.WriteLine("--- Fail ");
                    }
                }
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
                if (!nodeAddress.Equals(peer.Address))
                {
                    Console.WriteLine("-- BroadcastStake to {0}", peer.Address);
                    GrpcChannel channel = GrpcChannel.ForAddress(peer.Address);
                    var stakeService = new StakeServiceClient(channel);
                    try
                    {
                        var response = stakeService.Add(stake);
                        Console.WriteLine("--- Done");
                    }
                    catch
                    {
                        Console.WriteLine("--- Fail");
                    }
                }
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
                if (!nodeAddress.Equals(peer.Address))
                {

                    Console.WriteLine("-- BroadcastTransaction to {0}", peer.Address);
                    GrpcChannel channel = GrpcChannel.ForAddress(peer.Address);
                    var txnService = new TransactionServiceClient(channel);
                    try
                    {
                        var response = txnService.Receive(new TransactionPost
                        {
                            SendingFrom = nodeAddress,
                            Transaction = tx
                        });
                        if (response.Status == Others.Constants.TXN_STATUS_SUCCESS)
                        {
                            Console.WriteLine(".. Done");
                        }
                        else
                        {
                            Console.WriteLine(".. Fail");
                        }
                    }
                    catch
                    {
                        Console.WriteLine(".. Fail");
                    }

                }
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

            var response = blockService.GetRemains(new StartingParam { Height = lastBlockHeight });
            List<Block> blocks = response.Blocks.ToList();
            blocks.Reverse();

            var lastHeight = 0L;
            foreach (var block in blocks)
            {
                try
                {
                    Console.WriteLine("==== Download block: {0}", block.Height);
                    var status = ServicePool.DbService.blockDb.Add(block);
                    lastHeight = block.Height;
                    Console.WriteLine("==== Done");
                }
                catch
                {
                    Console.WriteLine("==== Fail");
                }
            }

            if (lastHeight < peerHeight)
            {
                DownloadBlocks(blockService, lastHeight, peerHeight);
            }

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
                    Console.WriteLine("Sync state to {0}", peer.Address);
                    try
                    {
                        GrpcChannel channel = GrpcChannel.ForAddress(peer.Address);
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
                        GrpcChannel channel = GrpcChannel.ForAddress(peer.Address);
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
