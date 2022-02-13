// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
namespace UbudKusCoin.Services
{

    public class StateService
    {
        public bool IsP2PServiceReady { set; get; }
        public bool IsDbServiceReady { set; get; }
        public bool IsFacadeServiceReady { set; get; }
        public bool IsWalletServiceReady { set; get; }
        public bool IsEventServiceReady { set; get; }
        public bool IsMintingServiceReady { set; get; }
        public StateService()
        {
            IsMintingServiceReady = false;
            IsP2PServiceReady = false;
            IsDbServiceReady = false;
            IsFacadeServiceReady = false;
            IsWalletServiceReady = false;
            IsEventServiceReady = false;
        }

        public void Start()
        {
            Console.WriteLine("Node will start all services ...");
        }

        public bool IsNodeStateReady(){
            return IsDbServiceReady && IsEventServiceReady && IsFacadeServiceReady && IsP2PServiceReady && IsWalletServiceReady;
        }

    }


}