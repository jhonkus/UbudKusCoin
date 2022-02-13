// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;

using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;

namespace UbudKusCoin.Facade
{

    public class AccountFacade
    {

        public AccountFacade()
        {
            Console.WriteLine("...... Account initilized.");
        }

        public Account GetByAddress(string address)
        {
            return ServicePool.DbService.accountDb.GetByAddress(address);
        }

        public List<Account> GetGenesis()
        {
            var timestamp = Utils.GetTime();
            var list = new List<Account> {
                new Account{
                    // owner elevator visual public loyal actual outside trumpet sugar drama impact royal
                    Address = "NFrwmp2Wo6zvPBpzCmDeLH5PNu6pcAg1ZDAUyUTDRYVC",
                    PubKey = "03d8bf992ebda445a512ae687a0601a43d85e623b3df052c3b32a44a895d9b3abd",
                    Balance = 2000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // actual lucky tail message acquire alarm bomb finger route wool reward bike
                    Address = "nBnrTvVoNyx2qg3yeBg3HtaHMbEyFPGJ4EQ183BBsphM",
                    PubKey = "034ab41cd9592200344a8c170cd26510966be1920a60943ea883458719d9e916f9",
                    Balance = 3000000000,
                    TxnCount = 2,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // secreet nunber
                    // return lucky tribe pen swarm diagram buzz april beef fire antenna robot
                    Address = "w5z8FbxzGL7K6erUfYBXNzAiLQ6qLPdzDyjexH13WjCq",
                    PubKey = "0371a749c364123b474530309d3b2d69e7347f2e58cb20960d354afb9ba8d645cc",
                    Balance = 4000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // secreet nunber
                    // transfer divorce liberty laptop harbor cotton hollow drop record deal pill charge
                    Address = "E985YzzBYZJG1csRz34tR5P9UetG6cCuR2DvnkaUJYq4",
                    PubKey = "020fa69ad893523e58f95a460d08f7e8c2fd1a2c86445f900fdf465300dacaf165",
                    Balance = 5000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // secret number
                    // elite mosquito enrich remain story indoor mutual elite polar juice enter craft
                    Address = "hS7NaiubZRqsWWS7AHY1woVH1E6buL4Ferp9f6HgnnNK",
                    PubKey = "03107eca9f1d44f2a452f6de7ebddab474fc5f489d7da2627b7fdd836b32045e36",
                    Balance = 10000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                }
            };
            return list;
        }
    }

}