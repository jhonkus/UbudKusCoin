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
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;
using Grpc.Net.Client;
using static UbudKusCoin.Grpc.PeerService;
using static UbudKusCoin.Grpc.BlockService;
using static UbudKusCoin.Grpc.TransactionService;
using static UbudKusCoin.Grpc.StakeService;
using CoreBlock = UbudKusCoin.Core.Types.Block;

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
                    GrpcChannel channel = CreateChannel(peer.Address);
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

        public void BroadcastCanonicalBlock(CoreBlock block)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;
            Parallel.ForEach(knownPeers, peer =>
            {
                if (nodeAddress.Equals(peer.Address))
                {
                    return;
                }

                try
                {
                    using var channel = CreateChannel(peer.Address);
                    var client = new CanonicalBlockService.CanonicalBlockServiceClient(channel);
                    var response = client.Add(CanonicalNodeService.ToGrpc(block));
                    Console.WriteLine("-- Canonical block {0}: {1}", peer.Address, response.Message);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("-- Canonical block broadcast failed: {0}", exception.Message);
                }
            });
        }

        public void BroadcastCanonicalVote(CanonicalVote vote)
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;
            Parallel.ForEach(knownPeers, peer =>
            {
                if (nodeAddress.Equals(peer.Address))
                {
                    return;
                }

                try
                {
                    using var channel = CreateChannel(peer.Address);
                    var client = new CanonicalBlockService.CanonicalBlockServiceClient(channel);
                    var response = client.SubmitVote(vote);
                    Console.WriteLine("-- Consensus vote {0}: {1}", peer.Address, response.Message);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("-- Consensus vote failed: {0}", exception.Message);
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
                    GrpcChannel channel = CreateChannel(peer.Address);
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
                    GrpcChannel channel = CreateChannel(peer.Address);
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
                    var status = ServicePool.BlockCommitService.ValidateAndCommit(block);
                    if (!status.Success)
                    {
                        Console.WriteLine("==== Rejected: {0}", status.Message);
                        break;
                    }

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

        private void DownloadCanonicalBlocks(CanonicalBlockService.CanonicalBlockServiceClient blockService, long lastBlockHeight, long peerHeight)
        {
            var response = blockService.GetRange(new CanonicalStartingPoint { Height = lastBlockHeight });
            if (response.Blocks.Count == 0)
            {
                return;
            }

            foreach (var block in response.Blocks)
            {
                var status = ServicePool.CanonicalNodeService.Add(block);
                if (!status.Accepted)
                {
                    Console.WriteLine("==== Canonical sync rejected: {0}", status.Message);
                    return;
                }
            }

            var newHeight = ServicePool.CanonicalNodeService.Chain.State.Height;
            if (newHeight < peerHeight)
            {
                DownloadCanonicalBlocks(blockService, newHeight, peerHeight);
            }
        }

        public void SyncCanonicalState()
        {
            var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;
            foreach (var peer in knownPeers)
            {
                if (nodeAddress.Equals(peer.Address))
                {
                    continue;
                }

                try
                {
                    using var channel = CreateChannel(peer.Address);
                    var blockService = new CanonicalBlockService.CanonicalBlockServiceClient(channel);
                    var peerHead = blockService.GetHead(new CanonicalEmpty());
                    var localHeight = ServicePool.CanonicalNodeService.Chain.State.Height;
                    if (peerHead.Height > localHeight)
                    {
                        DownloadCanonicalBlocks(blockService, localHeight, peerHead.Height);
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine("-- Canonical sync failed: {0}", exception.Message);
                }
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
        private static GrpcChannel CreateChannel(string address)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"Peer address is not an absolute URI: {address}");
            }

            var handler = new HttpClientHandler();
            var clientCertificatePath = DotNetEnv.Env.GetString(
                "P2P_TLS_CLIENT_CERT_PATH",
                DotNetEnv.Env.GetString("P2P_TLS_CERT_PATH", string.Empty));
            var clientCertificatePassword = DotNetEnv.Env.GetString(
                "P2P_TLS_CLIENT_CERT_PASSWORD",
                DotNetEnv.Env.GetString("P2P_TLS_CERT_PASSWORD", string.Empty));

            if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(clientCertificatePath))
            {
                handler.ClientCertificates.Add(new X509Certificate2(clientCertificatePath, clientCertificatePassword));
            }

            return GrpcChannel.ForAddress(uri, new GrpcChannelOptions
            {
                HttpHandler = handler
            });
        }

        public void SyncState()
        {
            SyncCanonicalState();
            Console.WriteLine("---- Sync Done~");
        }
    }
}
