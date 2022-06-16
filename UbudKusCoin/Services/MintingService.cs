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

namespace UbudKusCoin.Services
{
    public class MintingService
    {
        private CancellationTokenSource cancelTask;
        private bool isAlreadyStaking;
        private bool isMakingBlock;
        private readonly Random rnd;

        public MintingService()
        {
            rnd = new Random();
            isAlreadyStaking = true;
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

            // Run Auto Stake, to act as real staking process
            // in real blockchain, user will stake through website or mobile app.
            // in this auto stake process, I am not do balance validation,
            // no signature validation
            Task.Run(AutoStake, cancelTask.Token);

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
            var lastBlock = ServicePool.DbService.BlockDb.GetLast();
            Console.WriteLine("- Last Block: {0}", lastBlock.Height);
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
                    lastBlock = ServicePool.DbService.BlockDb.GetLast();
                    Console.WriteLine("- Last Block: {0}", lastBlock.Height);

                    Console.WriteLine("\n---------------------------------------------\n Stakes Leaderboard:");
                    Task.Run(LeaderBoard);

                    var myAddress = ServicePool.WalletService.GetAddress();
                    var maxStake = ServicePool.DbService.StakeDb.GetMax();
                    if (maxStake is not null && myAddress == maxStake.Address)
                    {
                        Console.WriteLine("\n-- Horee, I am the validator of the next block \n");
                        ServicePool.FacadeService.Block.New(maxStake);
                    }
                    else
                    {
                        Console.WriteLine("\n-- Damn, I was not lucky this time. \n");
                    }

                    Console.WriteLine("= = = = Minting finish = = = \n\n\n");
                }

                // sleep 1 second
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// To send staking one time, before time to create block.
        /// </summary>
        public void AutoStake()
        {
            ServicePool.DbService.StakeDb.DeleteAll();
            while (true)
            {
                var timeStaking = DateTime.UtcNow;
                // Clean the stakes before create a block.
                if (timeStaking.Second < 3)
                {
                    ServicePool.DbService.StakeDb.DeleteAll();
                    Console.WriteLine("... I've cleaned my stakes list.");
                    isAlreadyStaking = false;
                    Thread.Sleep(4000);
                    timeStaking = DateTime.UtcNow;
                }

                // staking will do in limited time starting from second: 4 to 30
                if (!isAlreadyStaking && timeStaking.Second < 35)
                {
                    // Make stakeing with random amount        
                    var stake = new Stake
                    {
                        Address = ServicePool.WalletService.GetAddress(),
                        Amount = rnd.Next(10, 100),
                        TimeStamp = Others.UkcUtils.GetTime()
                    };
                    Console.WriteLine("... Now I wil stake {0} coins at: {1}\n", stake.Amount, DateTime.UtcNow);

                    ServicePool.DbService.StakeDb.AddOrUpdate(stake);

                    Task.Run(() => ServicePool.P2PService.BroadcastStake(stake));

                    isAlreadyStaking = true;
                }

                // sleep 1 second
                Thread.Sleep(1000);
            }
        }

        private void LeaderBoard()
        {
            var stakeList = ServicePool.DbService.StakeDb.GetAll().FindAll();
            foreach (var stake in stakeList)
            {
                Console.WriteLine(" {0}, {1}", stake.Address, stake.Amount);
            }

            Console.WriteLine("-----------------------------------------------\n");
        }
    }
}