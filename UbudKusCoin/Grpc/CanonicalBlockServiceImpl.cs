using Grpc.Core;
using System.Threading.Tasks;
using UbudKusCoin.Services;

namespace UbudKusCoin.Grpc;

public sealed class CanonicalBlockServiceImpl : CanonicalBlockService.CanonicalBlockServiceBase
{
    public override Task<CanonicalBlockStatus> Add(CanonicalBlock request, ServerCallContext context)
    {
        var result = ServicePool.CanonicalNodeService.Add(request);
        return Task.FromResult(new CanonicalBlockStatus
        {
            Accepted = result.Accepted,
            Message = result.Message
        });
    }

    public override Task<CanonicalBlock> GetHead(CanonicalEmpty request, ServerCallContext context)
    {
        return Task.FromResult(CanonicalNodeService.ToGrpc(ServicePool.CanonicalNodeService.Chain.Head.Block));
    }
}
