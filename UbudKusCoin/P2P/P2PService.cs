using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic;
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
using UbudKusCoin.Others;
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
            ListenEvent();
            ServicePool.StateService.IsP2PServiceReady = true;
            Console.WriteLine("...... P2P service is ready");
        }


        /// <summary>
        /// This method to register event blockCreated, and EventTransaction created.
        /// </summary>
        private void ListenEvent()
        {
            ServicePool.EventService.EventBlockCreated += Evt_EventBlockCreated;
            ServicePool.EventService.EventTransactionCreated += Evt_EventTransactionCreated;
        }


        /// <summary>
        /// This method is listening, when there is event block created,
        ///  and call BroadcasBlock function.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="block"></param>
        void Evt_EventBlockCreated(object sender, Block block)
        {
            BroadcastBlock(block);
        }

        /// <summary>
        /// Event transaction listener, when transaction created, it will call
        /// function BroadcastTransaction
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="txn"></param>
        void Evt_EventTransactionCreated(object sender, Transaction txn)
        {
            BroadcastTransaction(txn);
        }

        /// <summary>
        /// For send block to all peer in known peers
        /// </summary>
        /// <param name="block"></param>
        private void BroadcastBlock(Block block)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;

            Console.WriteLine("\n- Will broadcasting block to {0} peers", knownPeers.Count());
            Parallel.ForEach(knownPeers, peer =>
            {

                Console.WriteLine(". . << Sending block to {0}", peer.Address);
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                var blockService = new BlockServiceClient(channel);
                var response = blockService.Add(block);
                Console.WriteLine(". . << Sending block done.\n\n ");

            });
        }


        public void Stake(double amount)
        {
            BroadcastStaking(amount);
        }

        private void BroadcastStaking(double amount)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;

            Console.WriteLine("\n- Will broadcasting stake to {0} peers", knownPeers.Count());
            Parallel.ForEach(knownPeers, peer =>
            {

                Console.WriteLine(". . << Sending stake to {0}", peer.Address);
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                var stakeService = new StakeServiceClient(channel);

                var response = stakeService.Add(
                    new Stake
                    {
                        Address = nodeAddress,
                        Amount = amount
                    }
                );

                Console.WriteLine(". . << Sending stake done. status: {0}\n\n ", response.Status);

                var list = stakeService.GetRange(new StakeParams
                {
                    PageNumber = 1,
                    ResultPerPage = 100
                });

                foreach (var stake in list.Stakes)
                {
                    Console.WriteLine(" Address {0}, amount {1}", stake.Address, stake.Amount);
                }

            });
        }

        /// <summary>
        /// For send transaction to all peer in known peers
        /// </summary>
        /// <param name="tx"></param>
        private void BroadcastTransaction(Transaction tx)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;

            Console.WriteLine("Will broadcasting transaction to {0} peers", knownPeers.Count());
            Parallel.ForEach(knownPeers, peer =>
            {
                Console.WriteLine("Sending Transaction to {0}", peer.Address);
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + peer.Address);
                var txnService = new TransactionServiceClient(channel);
                var response = txnService.Receive(new TransactionPost
                {
                    SendingFrom = nodeAddress,
                    Transaction = tx
                });
                Console.WriteLine("... Sending TX done. with status: {0}", response.Status);
            });

        }


        /// <summary>
        /// Download blocks from all peer in known peer
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
                Console.WriteLine("=== Downloading Block from Height: {0}, to {1}", lastBlockHeight, lastBlockHeight + 50);
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
            catch
            {
                throw;
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

                Thread.Sleep(100); // give time to next peer
            }


        }





    }



}
