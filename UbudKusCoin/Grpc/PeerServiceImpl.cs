// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Grpc.Core;
using UbudKusCoin.Services;
using System.Threading.Tasks;

namespace UbudKusCoin.Grpc
{

    public class PeerServiceImpl : PeerService.PeerServiceBase
    {
        public override Task<PeerStatus> Add(Peer request, ServerCallContext context)
        {
            var response = new PeerStatus();
            return Task.FromResult(response);
        }

        public override Task<PeerList> GetRange(PeerPaging request, ServerCallContext context)
        {
            var peers = ServicePool.DbService.peerDb.GetRange(request.PageNumber, request.ResultPerPage);
            var list = new PeerList();
            list.Peers.AddRange(peers);
            return Task.FromResult(list);
        }



    }
}
