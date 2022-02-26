// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Grpc.Core;
using System.Threading.Tasks;
using UbudKusCoin.Services;

namespace UbudKusCoin.Grpc
{
    public class StakeServiceImpl : StakeService.StakeServiceBase
    {

        public override Task<AddStakeStatus> Add(Stake req, ServerCallContext context)
        {

            ServicePool.DbService.stakeDb.AddOrUpdate(req);
            return Task.FromResult(new AddStakeStatus
            {
                Message = "success add stake",
                Status = Others.Constants.TXN_STATUS_SUCCESS,
            });
        }

        public override Task<StakeList> GetRange(StakeParams req, ServerCallContext context)
        {
            var response = new StakeList();
            var stakes = ServicePool.DbService.stakeDb.GetAll();
            response.Stakes.AddRange(stakes.FindAll());

            return Task.FromResult(response);
        }

    }
}
