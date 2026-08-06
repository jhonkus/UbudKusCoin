// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.
using Grpc.Core;
using UbudKusCoin.Services;
using System;
using System.Threading.Tasks;

namespace UbudKusCoin.Grpc
{
    public class PeerServiceImpl : PeerService.PeerServiceBase
    {
        public override Task<AddPeerReply> Add(Peer request, ServerCallContext context)
        {
            var response = new AddPeerReply();
            if (!ServicePool.FacadeService.Peer.Add(request, out var message))
            {
                response.Status = Others.Constants.TXN_STATUS_FAIL;
                response.Message = message;
                return Task.FromResult(response);
            }

            response.Status = Others.Constants.TXN_STATUS_SUCCESS;
            response.Message = message;
            return Task.FromResult(response);
        }

        public override Task<NodeState> GetNodeState(NodeParam request, ServerCallContext context)
        {
            if (!ServicePool.FacadeService.Peer.TouchKnownPeer(request.NodeIpAddress, out _))
            {
                // Discovery still works even when the caller is not registered yet.
            }

            var nodeState = ServicePool.FacadeService.Peer.GetNodeState();
            return Task.FromResult(nodeState);
        }
    }
}
