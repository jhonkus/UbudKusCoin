using Microsoft.VisualBasic.CompilerServices;
// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Grpc.Core;
using UbudKusCoin.Services;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace UbudKusCoin.Grpc
{

    public class PeerServiceImpl : PeerService.PeerServiceBase
    {
        public override Task<AddPeerReply> Add(Peer request, ServerCallContext context)
        {
            var response = new AddPeerReply();
            return Task.FromResult(response);
        }

        public override Task<NodeState> GetNodeState(NodeParam request, ServerCallContext context)
        {
            ServicePool.FacadeService.Peer.Add(new Peer{
                Address = request.NodeIpAddress,
                IsBootstrap = false,
                IsCanreach = true,
                LastReach = Others.UkcUtils.GetTime(),
                TimeStamp = Others.UkcUtils.GetTime()
            });

            var nodeState = ServicePool.FacadeService.Peer.GetNodeState();
            return Task.FromResult(nodeState);
        }



    }
}
