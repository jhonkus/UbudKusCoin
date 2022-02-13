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

            }

            Console.WriteLine("... Minting Service is starting");
            Console.WriteLine("...... Sync state with peer");
            //TODO this.BroadcastState();
            Console.WriteLine("...... Sync block is symced");
            Console.WriteLine("... Minting Service is Ready.");

            Console.WriteLine("... Node is Ready.");

            if (ServicePool.StateService.IsNodeStateReady())
            {
                cancelTask = new CancellationTokenSource();
                Task.Run(() => MintingBlock(), cancelTask.Token);
            }
        }


        public void Stop()
        {
            cancelTask.Cancel();
            Console.WriteLine("Minter Stoped");
        }

        public void MintingBlock()
        {
            while (true)
            {
                var startTime = DateTime.UtcNow.Second;
                ServicePool.FacadeService.Block.CreateNew();
                var endTime = DateTime.UtcNow.Second;
                var remainTime = Constants.BLOCK_GENERATION_INTERVAL - (endTime - startTime);
                Console.WriteLine("remain Time: {0}", remainTime);
                Thread.Sleep(remainTime < 0 ? 0 : remainTime * 1000);
            }
        }

    }
}