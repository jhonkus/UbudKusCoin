// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace UbudKusCoin.Services
{
    public class MintingService
    {
        private CancellationTokenSource cancelTask;
        private bool isMakingBlock;

        public MintingService()
        {
            isMakingBlock = true;
        }

        public void Start()
        {
            // sync state with others
            Console.WriteLine(".... Synchronizing state other peer(s) ");
            ServicePool.P2PService.SyncState();
            Console.WriteLine(".... Node is Ready.");

            Console.WriteLine("\n.... Minting Service is starting");
            cancelTask = new CancellationTokenSource();

            // run minting process
            Task.Run(MintingBlock, cancelTask.Token);
        }

        public void Stop()
        {
            cancelTask.Cancel();
            Console.WriteLine("Minter has been stopped");
        }

        public void MintingBlock()
        {
            isMakingBlock = true;
            Console.WriteLine("\n\n= = = = = = = = = = = = NODE IS RUNNING = = = = = = = = = = = =\n");
            Console.WriteLine(". Account Address: {0}", ServicePool.WalletService.GetAddress());
            Console.WriteLine(". Network Address: {0} ", ServicePool.FacadeService.Peer.NodeAddress);
            var lastBlock = ServicePool.CanonicalNodeService.Chain.Head.Block;
            Console.WriteLine("- Last Canonical Block: {0}", lastBlock.Height);
            Console.WriteLine("\n................ I am ready to validate blocks ..................\n");

            while (true)
            {
                var timeMinting = DateTime.UtcNow;
                if (timeMinting.Second < 3)
                {
                    isMakingBlock = false;
                }

                if (!isMakingBlock && timeMinting.Second >= 45)
                {
                    isMakingBlock = true;

                    Console.WriteLine("\n\n= = = = TIME TO MINTING = = = =\n");
                    Console.WriteLine("- Time: {0}", timeMinting.Second);
                    lastBlock = ServicePool.CanonicalNodeService.Chain.Head.Block;
                    Console.WriteLine("- Last Canonical Block: {0}", lastBlock.Height);

                    Console.WriteLine("\n-- Attempting canonical proposal\n");
                    var result = ServicePool.CanonicalNodeService.CreateAndCommitBlock(ServicePool.WalletService);
                        if (result.Accepted)
                        {
                            ServicePool.P2PService.BroadcastCanonicalBlock(result.Block);
                            var vote = ServicePool.CanonicalNodeService.CreateVote(result.Block, ServicePool.WalletService);
                            var voteResult = ServicePool.CanonicalNodeService.SubmitVote(vote);
                            Console.WriteLine("-- Local consensus vote: {0}", voteResult.Message);
                            Task.Run(() => ServicePool.P2PService.BroadcastCanonicalVote(vote));
                        }
                    else
                    {
                        Console.WriteLine("-- Proposal not accepted: {0}", result.Message);
                    }

                    Console.WriteLine("= = = = Minting finish = = = \n\n\n");
                }

                // sleep 1 second
                Thread.Sleep(1000);
            }
        }

    }
}
