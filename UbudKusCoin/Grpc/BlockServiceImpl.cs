// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Threading.Tasks;

using Grpc.Core;

using UbudKusCoin.Services;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace UbudKusCoin.Grpc
{

    public class BlockServiceImpl : BlockService.BlockServiceBase
    {
        public override Task<AddBlockStatus> Add(Block block, ServerCallContext context)
        {

            var lastBlock = ServicePool.DbService.blockDb.GetLast();

            // validate block hash
            if (block.PrevHash != lastBlock.Hash)
            {
                return Task.FromResult(new AddBlockStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "hash not valid"
                });
            }

            // validate block height
            if (block.Height != lastBlock.Height + 1)
            {
                return Task.FromResult(new AddBlockStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "height not valid"
                });
            }

            // Console.WriteLine("\n- - - - >> Receiving block , height: {0} \n- - - - >> from: {1}\n", block.Height, block.Validator);
            var addStatus = ServicePool.DbService.blockDb.Add(block);
            //Console.WriteLine("- - - - >> Block added to db.");

            //extract transaction
            var transactions = JsonConvert.DeserializeObject<List<Transaction>>(block.Transactions);


            // update balances
            ServicePool.FacadeService.Account.UpdateBalance(transactions);

            // move pool to to transactions db
            ServicePool.FacadeService.Transaction.AddBulk(transactions);

            // clear mempool
            ServicePool.DbService.transactionsPooldb.DeleteAll();


            return Task.FromResult(addStatus);
        }

        public override Task<Block> GetFirst(EmptyRequest request, ServerCallContext context)
        {
            var block = ServicePool.DbService.blockDb.GetFirst();
            return Task.FromResult(block);
        }
        public override Task<Block> GetLast(EmptyRequest request, ServerCallContext context)
        {
            var block = ServicePool.DbService.blockDb.GetLast();
            return Task.FromResult(block);
        }
        public override Task<Block> GetByHash(Block request, ServerCallContext context)
        {
            var block = ServicePool.DbService.blockDb.GetByHash(request.Hash);
            return Task.FromResult(block);
        }
        public override Task<Block> GetByHeight(Block request, ServerCallContext context)
        {
            var block = ServicePool.DbService.blockDb.GetByHeight(request.Height);
            return Task.FromResult(block);
        }
        public override Task<BlockList> GetRange(BlockParams request, ServerCallContext context)
        {
            var blocks = ServicePool.DbService.blockDb.GetRange(request.PageNumber, request.ResultPerPage);
            var list = new BlockList();
            list.Blocks.AddRange(blocks);
            return Task.FromResult(list);
        }

        public override Task<BlockList> GetRemains(StartingParam request, ServerCallContext context)
        {
            var blocks = ServicePool.DbService.blockDb.GetRemains(request.Height);
            var list = new BlockList();
            list.Blocks.AddRange(blocks);
            return Task.FromResult(list);
        }



    }
}
