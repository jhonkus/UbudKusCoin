using Microsoft.VisualBasic.CompilerServices;
// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading;
using System.Threading.Tasks;
using UbudKusCoin.Grpc;
using Grpc.Net.Client;
using static UbudKusCoin.Grpc.StakeService;

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
            Console.WriteLine(".... Node is Ready.");


            Console.WriteLine("\n.... Minting Service is starting");
            cancelTask = new CancellationTokenSource();

            // Run Auto Stake, to act as real staking process
            // in real blockchain, user will stake through website or mobile app.
            // in this auto stake process, I am not do balance validation,
            // no signature validation
            Task.Run(() => AutoStake(), cancelTask.Token);


            // run minting process
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
                var currentTime = DateTime.UtcNow;

                if (currentTime.Second == 29 || currentTime.Second == 59)
                {
                    // Console.Clear();
                    Console.WriteLine("\n\n= = = = TIME TO MINTING = = = =");
                    Console.WriteLine("- Time: {0}", currentTime);

                    Console.WriteLine("\n-------------------------------\n Current Stakes:");
                    LeaderBoard();
                    // Thread.Sleep(2000);
                    Console.WriteLine("-------------------------------\n");

                    var myAddress = ServicePool.WalletService.GetAddress();
                    //Console.WriteLine("my Address {0}", myAddress);

                    var stakeWinner = ServicePool.DbService.stakeDb.GetMaxStake();
                    if (stakeWinner is not null && myAddress == stakeWinner.Address)
                    {
                        Console.WriteLine("-- Horee, I am validator for next Block \n");
                        ServicePool.FacadeService.Block.CreateNew();
                    }

                    // give some delay
                    Thread.Sleep(5000);

                    Console.WriteLine("= = = = Minting Done = = = \n\n\n");
                }

            }
        }


        public void AutoStake()
        {

            ServicePool.DbService.stakeDb.DeleteAll();

            Random rnd = new Random();

            while (true)
            {
                var currentTime = DateTime.UtcNow;

                // Clean the staker list before time for  block creation.
                // Staker list will clean on second: 2 and 34.

                if (currentTime.Second == 2 || currentTime.Second == 30)
                {
                    //Console.WriteLine("... Cleaning stakes");
                    ServicePool.DbService.stakeDb.DeleteAll();
                    //Console.WriteLine("... Done!");
                }

                // Each node will stake amount of coin to network
                // I make fake amount by generated it randomly.
                // staking will do in limited time starting from second: 4 to 32
                // and sedond 

                if (currentTime.Second == 4 || currentTime.Second == 32)
                {
                    //Console.WriteLine("Time to staking ");

                    // To make staker not do staking in the same time let make some delay
                    var delayStaking = rnd.Next(1000, 3000);
                    Thread.Sleep(delayStaking);
                    //end of

                    // Make stakeing with random amount        
                    var stake = new Stake
                    {
                        Address = ServicePool.WalletService.GetAddress(),
                        Amount = rnd.Next(10, 100),
                        TimeStamp = UbudKusCoin.Others.Utils.GetTime()
                    };
                    // Console.Clear();
                    Console.WriteLine("---- I Stake {0} coins at {1} \n\n", stake.Amount, DateTime.UtcNow);

                    // Console.WriteLine(".... Send Staking, amount: {0}", amount);
                    ServicePool.P2PService.BroadcastStake(stake);

                    // Console.WriteLine("..... Staking send");
                }

            }
        }


        private void LeaderBoard()
        {
            var satkes = ServicePool.DbService.stakeDb.GetAll().FindAll();
            foreach (var stake in satkes)
            {
                Console.WriteLine(" {0}, {1}", stake.Address, stake.Amount);
            }
        }


    }
}