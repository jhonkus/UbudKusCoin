﻿using Grpc.Core;
using System.Threading.Tasks;
using UbudKusCoin.Services;

namespace UbudKusCoin.Grpc
{
    public class StakeServiceImpl : StakeService.StakeServiceBase
    {

        public override Task<StakeList> GetRange(StakeParams req, ServerCallContext context)
        {
            var response = new StakeList();
            var transactions = ServicePool.FacadeService.Stake.GetRange(req.PageNumber, req.ResultPerPage);
            response.Stakes.AddRange(transactions);
            return Task.FromResult(response);
        }


    }
}
