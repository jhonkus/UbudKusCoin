// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using UbudKusCoin.P2P;

namespace UbudKusCoin.Services
{
    public static class ServicePool
    {
        public static MintingService MintingService { set; get; }

        public static DbService DbService { set; get; }

        public static FacadeService FacadeService { set; get; }

        public static WalletService WalletService { set; get; }
        public static P2PService P2PService { set; get; }

        public static void Add(
                  WalletService wallet,
                  DbService db,
                  FacadeService facade,
                  MintingService minter,
                  P2PService p2p)
        {
            WalletService = wallet;
            DbService = db;
            FacadeService = facade;
            MintingService = minter;
            P2PService = p2p;
        }
        public static void Start()
        {
            WalletService.Start();
            DbService.Start();
            FacadeService.start();
            P2PService.Start();
            MintingService.Start();
        }

        public static void Stop()
        {
            //stop when application exit
            DbService.Stop();
        }


    }
}
