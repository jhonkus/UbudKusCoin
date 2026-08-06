// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using UbudKusCoin.Services;
using UbudKusCoin.Others;

namespace UbudKusCoin.Grpc
{
    public class BlockServiceImpl : BlockService.BlockServiceBase
    {
        public override Task<AddBlockStatus> Add(Block block, ServerCallContext context)
        {
            var result = ServicePool.BlockCommitService.ValidateAndCommit(block);
            return Task.FromResult(new AddBlockStatus
            {
                Status = result.Success ? Constants.TXN_STATUS_SUCCESS : Constants.TXN_STATUS_FAIL,
                Message = result.Message
            });
        }

        public override Task<Block> GetFirst(EmptyRequest request, ServerCallContext context)
        {
            var block = ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(0).FirstOrDefault();
            return Task.FromResult(block is null ? new Block() : CanonicalExplorerMapper.ToBlock(block));
        }

        public override Task<Block> GetLast(EmptyRequest request, ServerCallContext context)
        {
            var block = ServicePool.CanonicalNodeService.Chain.Head.Block;
            return Task.FromResult(CanonicalExplorerMapper.ToBlock(block));
        }

        public override Task<Block> GetByHash(Block request, ServerCallContext context)
        {
            var block = ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(0)
                .FirstOrDefault(item => item.ComputeHeaderHashHex().Equals(request.Hash, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(block is null ? new Block() : CanonicalExplorerMapper.ToBlock(block));
        }

        public override Task<Block> GetByHeight(Block request, ServerCallContext context)
        {
            var block = ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(0)
                .FirstOrDefault(item => item.Height == request.Height);
            return Task.FromResult(block is null ? new Block() : CanonicalExplorerMapper.ToBlock(block));
        }

        public override Task<BlockList> GetRange(BlockParams request, ServerCallContext context)
        {
            var blocks = ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(0)
                .OrderByDescending(item => item.Height)
                .Skip(Math.Max(0, request.PageNumber - 1) * request.ResultPerPage)
                .Take(Math.Max(0, request.ResultPerPage))
                .Select(CanonicalExplorerMapper.ToBlock);
            var list = new BlockList();
            list.Blocks.AddRange(blocks);
            return Task.FromResult(list);
        }

        public override Task<BlockList> GetRemains(StartingParam request, ServerCallContext context)
        {
            var blocks = ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(request.Height)
                .Select(CanonicalExplorerMapper.ToBlock);
            var list = new BlockList();
            list.Blocks.AddRange(blocks);
            return Task.FromResult(list);
        }
    }
}
