// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

using UbudKusCoin.Grpc;
using UbudKusCoin.Services;

namespace UbudKusCoin.Facade
{
    public class StakeFacade
    {
        public StakeFacade()
        {
            Console.WriteLine("...... Stake initilized.");
        }

        public Stake GetMaxStake()
        {
            var validator = ServicePool.DbService.stakeDb.GetMax();
            return validator;
        }
        public void AddOrUpdate(Stake stake)
        {
            ServicePool.DbService.stakeDb.AddOrUpdate(stake);
        }

    }
}
