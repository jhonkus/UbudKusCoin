// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            var block = ServicePool.DbService.BlockDb.GetFirst();
            return Task.FromResult(block);
        }

        public override Task<Block> GetLast(EmptyRequest request, ServerCallContext context)
        {
            var block = ServicePool.DbService.BlockDb.GetLast();
            return Task.FromResult(block);
        }

        public override Task<Block> GetByHash(Block request, ServerCallContext context)
        {
            var block = ServicePool.DbService.BlockDb.GetByHash(request.Hash);
            return Task.FromResult(block);
        }

        public override Task<Block> GetByHeight(Block request, ServerCallContext context)
        {
            var block = ServicePool.DbService.BlockDb.GetByHeight(request.Height);
            return Task.FromResult(block);
        }

        public override Task<BlockList> GetRange(BlockParams request, ServerCallContext context)
        {
            var blocks = ServicePool.DbService.BlockDb.GetRange(request.PageNumber, request.ResultPerPage);
            var list = new BlockList();
            list.Blocks.AddRange(blocks);
            return Task.FromResult(list);
        }

        public override Task<BlockList> GetRemains(StartingParam request, ServerCallContext context)
        {
            var blocks = ServicePool.DbService.BlockDb.GetRemaining(request.Height);
            var list = new BlockList();
            list.Blocks.AddRange(blocks);
            return Task.FromResult(list);
        }
    }
}
