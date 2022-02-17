// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading;
using System.Threading.Tasks;

using UbudKusCoin.Others;

namespace UbudKusCoin.Services
{
    public class MintingService
    {
        private CancellationTokenSource cancelTask;

        public MintingService()
        {

        }

        public void Start()
        {
            // Console.WriteLine("...... Waiiting P2P Ready");
            while (!ServicePool.StateService.IsNodeStateReady())
            {
                //waiting until P2P Ready       
            }
            Console.WriteLine("... Minting Service is starting");

            // sync state with others
            ServicePool.P2PService.SyncState();
            Console.WriteLine("... Node is Ready.");

            // if (ServicePool.StateService.IsNodeStateReady())
            // {

            // var knownPeers = ServicePool.FacadeService.Peer.GetKnownPeers();
            // var nodeAddress = ServicePool.FacadeService.Peer.NodeAddress;


            // var isBootstrap = false;
            // foreach (var peer in knownPeers)
            // {
            //     if (peer.IsBootstrap && nodeAddress.Equals(peer.Address))
            //     {

            //         isBootstrap = true;
            //         break;
            //     }
            // }

            // if (!isBootstrap)
            // {
            //      Console.WriteLine("... Waiting 10 Minutes to minting.");
            //     Thread.Sleep(2 * 60 * 1000);
            // }

            cancelTask = new CancellationTokenSource();
            Task.Run(() => MintingBlock(), cancelTask.Token);
            // }
        }


        public void Stop()
        {
            cancelTask.Cancel();
            Console.WriteLine("Minter Stoped");
        }

        public void MintingBlock()
        {
            Console.WriteLine("\n\n= = = = = = = = NODE IS RUNNING =  = = = = = = ");
            Console.WriteLine("= = = = ready to fight to create block = = = =\n\n");
            while (true)
            {
                var startTime = DateTime.UtcNow.Second;

                if (startTime % 20 == 0)
                {
                    Console.WriteLine("\n\n= = = = TIME TO MINTING = = = {0}", startTime);
                    ServicePool.FacadeService.Block.CreateNew();
                    Console.WriteLine("= = = = Minting Done = = = \n\n\n");
                }


                // var endTime = DateTime.UtcNow.Second;
                //   var remainTime = Constants.BLOCK_GENERATION_INTERVAL - (endTime - startTime);
                // Console.WriteLine("remain Time: {0}", remainTime);
                //Thread.Sleep(remainTime < 0 ? 0 : remainTime * 1000);
            }
        }

    }
}