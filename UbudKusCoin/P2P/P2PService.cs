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
using static UbudKusCoin.Grpc.TransactionService;

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
            ListenEvent();
            ServicePool.StateService.IsP2PServiceReady = true;
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

        void Evt_EventTransactionCreated(object sender, Transaction txn)
        {
            Task.Run(() =>
           {
               BroadcastTransaction(txn);
           });
        }


        private void BroadcastBlock(Block block)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;

            Console.WriteLine("Will broadcasting block to {0} peers", knownPeers.Count());
            foreach (var peer in knownPeers)
            {
                Console.WriteLine(". . . . Sending block to {0}", peer.Address);
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                var blockService = new BlockServiceClient(channel);
                var response = blockService.Add(block);
                Console.WriteLine(". . . . Sending block done.\n\n ");
            }
        }


        private void BroadcastTransaction(Transaction tx)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;

            Console.WriteLine("Will broadcasting transaction to {0} peers", knownPeers.Count());
            foreach (var peer in knownPeers)
            {
                Console.WriteLine("Sending Transaction to {0}", peer.Address);
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                var txnService = new TransactionServiceClient(channel);
                var response = txnService.Receive(new TransactionPost
                {
                    SendingFrom = nodeAddress,
                    Transaction = tx
                });
                Console.WriteLine("... Sending TX done. ");
            }
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


            Console.WriteLine("------ my Network Address {0}", nodeAddress);

            foreach (var peer in knownPeers)
            {
                if (!nodeAddress.Equals(peer.Address))
                {
                    Console.WriteLine("------ Syncronizing state with {0}", peer.Address);
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
                        Console.WriteLine("---- Sync Done~");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(" error when connecting to : {0}", peer.Address);
                    }
                }

                Thread.Sleep(2000); // give time to next peer
            }


        }





    }



}
