using Grpc.Core;
using System.Linq;
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

    public override Task<CanonicalBlockList> GetRange(CanonicalStartingPoint request, ServerCallContext context)
    {
        var response = new CanonicalBlockList();
        response.Blocks.AddRange(ServicePool.CanonicalNodeService.GetRange(request.Height)
            .Select(CanonicalNodeService.ToGrpc));
        return Task.FromResult(response);
    }

    public override Task<CanonicalVoteStatus> SubmitVote(CanonicalVote request, ServerCallContext context)
    {
        var result = ServicePool.CanonicalNodeService.SubmitVote(request);
        return Task.FromResult(new CanonicalVoteStatus
        {
            Accepted = result.Accepted,
            Finalized = result.Finalized,
            Message = result.Message
        });
    }
}
