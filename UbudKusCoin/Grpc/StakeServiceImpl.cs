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
            if (ServicePool.ConsensusEngine?.Mode == ConsensusEngineMode.CometBft)
            {
                return Task.FromResult(new AddStakeStatus
                {
                    Message = "Direct stake writes are disabled under CometBFT; submit a signed consensus transaction.",
                    Status = Others.Constants.TXN_STATUS_FAIL,
                });
            }

            ServicePool.DbService.StakeDb.AddOrUpdate(req);
            return Task.FromResult(new AddStakeStatus
            {
                Message = "Stake successfully added",
                Status = Others.Constants.TXN_STATUS_SUCCESS,
            });
        }

        public override Task<StakeList> GetRange(StakeParams req, ServerCallContext context)
        {
            var response = new StakeList();
            var stakes = ServicePool.DbService.StakeDb.GetAll();
            response.Stakes.AddRange(stakes);
            return Task.FromResult(response);
        }
    }
}
