// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using UbudKusCoin.Facade;

namespace UbudKusCoin.Services
{
    public class FacadeService
    {
        public PeerFacade Peer { set; get; }
        public AccountFacade Account { set; get; }
        public BlockFacade Block { set; get; }
        public TransactionFacade Transaction { set; get; }
        public TransactionPoolFacade TransactionPool { set; get; }
        public StakeFacade Stake { set; get; }

        public FacadeService()
        {
        }

        public void start()
        {
            Console.WriteLine("... Facade service is starting");
            Peer = new PeerFacade();
            Stake = new StakeFacade();
            Account = new AccountFacade();
            TransactionPool = new TransactionPoolFacade();
            Transaction = new TransactionFacade();
            Block = new BlockFacade();
            Console.WriteLine("...... Facade service is ready");
        }
    }
}