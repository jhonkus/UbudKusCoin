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
                Console.WriteLine(" ..Waiting P2P Ready ...");
            }

            // sync state with others
            Console.WriteLine(".... Synchronizing state other peer(s) ");
            ServicePool.P2PService.SyncState();
            Console.WriteLine("... Node is Ready.");


            Console.WriteLine("\n... Minting Service is starting");
            cancelTask = new CancellationTokenSource();

            Task.Run(() => AutoStake(), cancelTask.Token);


            Task.Run(() => MintingBlock(), cancelTask.Token);

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
                var mintingTime = DateTime.UtcNow;

                if (mintingTime.Second % 30 == 0)
                {
                    Console.WriteLine("\n\n= = = = TIME TO MINTING = = = =");
                    Console.WriteLine("- Time: {0}", mintingTime);

                    ServicePool.FacadeService.Block.CreateNew();

                    Console.WriteLine("= = = = Minting Done = = = \n\n\n");
                }

            }
        }


        public void AutoStake()
        {

            Random rnd = new Random();
            while (true)
            {
                var stakingTime = DateTime.UtcNow;

                // Asume staker updating staking frequently
                var delayStaking = rnd.Next(10000, 20000);
                Thread.Sleep(delayStaking);
                //end of

                // staking amount        
                var amount = rnd.Next(100, 500);
                // Console.WriteLine(".... Send Staking, amount: {0}", amount);
                ServicePool.P2PService.Stake(amount);
                // Console.WriteLine("..... Staking send");


            }
        }



    }
}