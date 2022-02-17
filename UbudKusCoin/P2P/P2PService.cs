// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;
using Grpc.Net.Client;
using static UbudKusCoin.Grpc.PeerService;
using static UbudKusCoin.Grpc.BlockService;

namespace UbudKusCoin.P2P
{


    public class GetData
    {
        public string Type { set; get; }
        public string ID { set; get; }
    }

    public class P2PService
    {
        IList<string> BlocksInTransit { set; get; }

        public P2PService()
        {

        }

        public void Start()
        {
            Console.WriteLine("... P2P service is starting");
            Task.Run(() =>
            {
                ListenEvent();
                ServicePool.StateService.IsP2PServiceReady = true;
            });

        }

        private void ListenEvent()
        {
            ServicePool.EventService.EventBlockCreated += Evt_EventBlockCreated;
            ServicePool.EventService.EventTransactionCreated += Evt_EventTransactionCreated;
        }

        void Evt_EventBlockCreated(object sender, Block block)
        {
            Task.Run(() =>
            {
                BroadcastBlock(block);
            });
        }

        private void BroadcastBlock(Block block)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;

            foreach (var peer in knownPeers)
            {
                if (!peer.Equals(nodeAddress))
                {
                    Console.WriteLine("Sending block to {0}", peer.Address);
                    GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                    var blockService = new BlockServiceClient(channel);
                    var status = blockService.Add(block);
                }
            }
        }


        void Evt_EventTransactionCreated(object sender, Transaction txn)
        {

        }


        private void DownloadBlocks(BlockServiceClient blockService, long lastBlockHeight, long peerHeight)
        {
            try
            {
                var response = blockService.GetRemains(new StartingParam { Height = lastBlockHeight });
                List<Block> blocks = response.Blocks.ToList();
                blocks.Reverse();
                var lastHeight = lastBlockHeight;
                Console.WriteLine("=== Downloading Block from Height: {0}, to {1}", lastBlockHeight, lastBlockHeight + 50);
                foreach (var block in blocks)
                {
                    Console.WriteLine("==== Block" + block.Height);
                    // TODO, VALIDATE BLOCK
                    ServicePool.DbService.blockDb.Add(block);
                    lastHeight = block.Height;
                }

                if (lastHeight < peerHeight)
                {
                    DownloadBlocks(blockService, lastHeight, peerHeight);
                }
            }
            catch
            {
                throw;
            }

        }

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





        public void SyncState()
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;



            foreach (var peer in knownPeers)
            {
                Console.WriteLine("------ nodeAddress {0}", nodeAddress);
                Console.WriteLine("------ PEER address {0}", peer.Address);
                if (!nodeAddress.Equals(peer.Address))
                {

                    Console.WriteLine("------ Will sync state with {0}", peer.Address);
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

                        // checking known peers
                        foreach (var newPeer in peerState.KnownPeers)
                        {
                            if (!IsNewPeer(newPeer.Address))
                            {
                                ServicePool.FacadeService.Peer.Add(newPeer);
                            }

                        }

                    }
                    catch (Exception)
                    {
                        // Console.WriteLine(" error when connecting: {0}", e.Message);
                    }
                }

                Thread.Sleep(10000); // give time to next peer
            }


        }





    }



}
