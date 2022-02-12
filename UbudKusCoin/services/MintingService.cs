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
            Console.WriteLine("Minting Service started...");
        }

        public void Start()
        {
            cancelTask = new CancellationTokenSource();
            Task.Run(() => MintingBlock(), cancelTask.Token);
            Console.WriteLine("Minter started started");
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

                Console.WriteLine("Oke ");

                ServicePool.FacadeService.Block.CreateNew();

                Console.WriteLine("Oke End");
                
                var endTime = DateTime.UtcNow.Second;
                var remainTime = Constants.BLOCK_GENERATION_INTERVAL - (endTime - startTime);
                Console.WriteLine("remain Time: {0}", remainTime);
                Thread.Sleep(remainTime < 0 ? 0 : remainTime * 1000);
            }
        }

    }
}