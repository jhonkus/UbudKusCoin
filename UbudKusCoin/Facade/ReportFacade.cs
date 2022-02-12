// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UbudKusCoin.Services;
using UbudKusCoin.Grpc;

namespace UbudKusCoin.Facade
{
    public class ReportFacade
    {

        private InfoBlockTxn _BlockInfo { set; get; }

        private List<Block> _Blocks { set; get; }

        private List<Transaction> _Txns { set; get; }

        public ReportFacade()
        {
            Console.WriteLine("Report initilize ....");

            // BuildReport();

        }



        public void BuildReport()
        {

            var localBcInfo = new InfoBlockTxn();
            var firstBlock = ServicePool.DbService.blockDb.GetFirst();
            var lastBlock = ServicePool.DbService.blockDb.GetLast();
            var blocks = ServicePool.DbService.blockDb.GetAll();

            var amountTnxs = 0d;
            var numTnxs = 0L;
            var amountReward = 0d;
            var transactions = new List<Transaction>();


            if (_BlockInfo.NumBlock == 0)
            {

                foreach (var block in blocks.FindAll())
                {
                    amountTnxs += block.TotalAmount;
                    numTnxs += block.NumOfTx;
                    amountReward += block.TotalReward;
                }
                localBcInfo.AmountTxn = amountTnxs;
                localBcInfo.NumTxn = numTnxs;
                localBcInfo.AmountReward = amountReward;
                localBcInfo.NumBlock = lastBlock.Height;
                localBcInfo.Tps = (long)lastBlock.NumOfTx / (long)30;

                // add 10 txns
                var trans1 = ServicePool.DbService.transactionDb.GetLasts(10);
                _Txns.AddRange(trans1);


                // add 10 blocks
                var blocks1 = ServicePool.DbService.blockDb.GetRange(1, 10);
                _Blocks.AddRange(blocks1);


                _BlockInfo = localBcInfo;
                Console.WriteLine("== TPS {0}", localBcInfo.Tps);
                return;
            }

            amountTnxs = (_BlockInfo.AmountTxn + lastBlock.TotalAmount);
            numTnxs = (_BlockInfo.NumTxn + lastBlock.NumOfTx);
            amountReward = (_BlockInfo.AmountReward + lastBlock.TotalReward);

            localBcInfo.AmountTxn = amountTnxs;
            localBcInfo.NumTxn = numTnxs;
            localBcInfo.Tps = (long)lastBlock.NumOfTx / (long)30;
            localBcInfo.AmountReward = amountReward;
            localBcInfo.NumBlock = lastBlock.Height;


            // add 10 txns
            var trans = ServicePool.DbService.transactionDb.GetLasts(10);
            if (trans is not null)
            {
                _Txns = new List<Transaction>();
                _Txns.AddRange(trans);
            }

            // add 10 blocks
            var blcks = ServicePool.DbService.blockDb.GetRange(1, 10);
            _Blocks = blcks;

            _BlockInfo = localBcInfo;
        }






        // public override Task<BcInfoResponse> GetBchainInfo(CommonRequest request, ServerCallContext context)
        // {
        //     if (bcInfo.NumBloks == 0)
        //     {
        //         BuildReport();
        //     }

        //     return Task.FromResult(bcInfo);
        // }

        // public override Task<TxnPoolResponse> GetPoolInfo(CommonRequest request, ServerCallContext context)
        // {

        //     var pools = Transaction.GetPool();

        //     var amountPool = 0d;
        //     var numPool = 0;
        //     foreach (var tx in pools)
        //     {
        //         amountPool += tx.Amount;
        //         numPool += 1;
        //     }

        //     return Task.FromResult(new TxnPoolResponse
        //     {
        //         NumPool = numPool,
        //         AmountPool = amountPool
        //     });
        // }





        // public override Task<SearchResponse> Search(CommonRequest request, ServerCallContext context)
        // {
        //     var searchText = request.SearchText.Trim();

        //     var response = new SearchResponse
        //     {
        //         Id = 0,
        //         Title = "no data found",
        //         SarchText = searchText,
        //         Url = "/notfound",
        //         Status = "no"
        //     };
        //     //search block by hash
        //     try
        //     {
        //         var block = Blockchain.GetBlockByHash(searchText);
        //         if (block is not null && block.Hash is not null)
        //         {
        //             response = new SearchResponse
        //             {
        //                 Id = 1,
        //                 Title = "search found block by hash",
        //                 SarchText = searchText,
        //                 Url = "/block/hash/" + searchText,
        //                 Status = "ok"
        //             };
        //             return Task.FromResult(response);
        //         }
        //     }
        //     catch
        //     {
        //         Console.WriteLine("no block by hash");
        //     }

        //     // search block by height
        //     try
        //     {
        //         var height = int.Parse(searchText);
        //         var block = Blockchain.GetBlockByHeight(height);
        //         if (block is not null && block.Hash is not null)
        //         {
        //             response = new SearchResponse
        //             {
        //                 Id = 2,
        //                 Title = "search found block by height",
        //                 SarchText = searchText,
        //                 Url = "/block/height/" + searchText,
        //                 Status = "ok"
        //             };
        //             return Task.FromResult(response);
        //         }
        //     }
        //     catch
        //     {
        //         Console.WriteLine("no block by height");
        //     }


        //     //search tnxs by hash
        //     try
        //     {
        //         var txn = Transaction.GetTxnByHash(searchText);
        //         if (txn is not null && txn.Hash is not null)
        //         {
        //             response = new SearchResponse
        //             {
        //                 Id = 3,
        //                 Title = "Found transaction",
        //                 SarchText = searchText,
        //                 Url = "/txn/" + searchText,
        //                 Status = "ok"
        //             };
        //             return Task.FromResult(response);
        //         }

        //     }
        //     catch
        //     {
        //         Console.WriteLine("no txn by hash");
        //     }


        //     // get Account 
        //     try
        //     {
        //         // 1. get one transaction by address
        //         var txn = Transaction.GetOneTxnByAddress(searchText);
        //         if (txn is not null && txn.Hash is not null)
        //         {
        //             response = new SearchResponse
        //             {
        //                 Id = 4,
        //                 Title = "search found one address ",
        //                 SarchText = searchText,
        //                 Url = "/address/" + searchText,
        //                 Status = "ok"
        //             };
        //             return Task.FromResult(response);
        //         }

        //     }
        //     catch
        //     {
        //         Console.WriteLine("Error when search by account");
        //     }


        //     return Task.FromResult(response);
        // }


        // public static void BuildReport2()
        // {
        //     var localBcInfo = new BcInfoResponse();
        //     Console.WriteLine("make report");
        //     var lastBlock = Blockchain.GetLastBlock();
        //     var blocks = Blockchain.GetBlocks();

        //     var amountTnxs = 0d;
        //     var numTnxs = 0L;
        //     var amountReward = 0d;
        //     var transactions = new List<Transaction>();



        //     if (bcInfo.NumBloks == 0)
        //     {
        //         // calculate from begining
        //         foreach (var block in blocks.FindAll())
        //         {
        //             amountTnxs += block.TotalAmount;
        //             numTnxs += block.NumOfTx;
        //             amountReward += block.TotalReward;
        //         }
        //         localBcInfo.AmountTxns = amountTnxs;
        //         localBcInfo.NumTxns = numTnxs;
        //         localBcInfo.AmountReward = amountReward;
        //         localBcInfo.NumBloks = lastBlock.Height;
        //         localBcInfo.Tps = lastBlock.NumOfTx / 30;

        //         // add 10 txns
        //         var trans1 = Transaction.GetTransactions(1, 10);
        //         if (trans1 is not null)
        //         {
        //             foreach (Transaction txn in trans1)
        //             {
        //                 TxnModel mdl = ConvertTxnModel(txn);
        //                 localBcInfo.Txns.Add(mdl);
        //             }
        //         }

        //         // add 10 blocks
        //         var blocks1 = Blockchain.GetBlocks(1, 10);
        //         if (blocks1 is not null)
        //         {
        //             foreach (Block block in blocks1)
        //             {
        //                 BlockModel mdl = ConvertBlockForList(block);
        //                 localBcInfo.Blocks.Add(mdl);
        //             }
        //         }

        //         bcInfo = localBcInfo;
        //         Console.WriteLine("== TPS {0}", localBcInfo.Tps);
        //         return;
        //     }

        //     amountTnxs = (bcInfo.AmountTxns + lastBlock.TotalAmount);
        //     numTnxs = (bcInfo.NumTxns + lastBlock.NumOfTx);
        //     amountReward = (bcInfo.AmountReward + lastBlock.TotalReward);
        //     localBcInfo.AmountTxns = amountTnxs;
        //     localBcInfo.NumTxns = numTnxs;
        //     localBcInfo.Tps = lastBlock.NumOfTx / 30;
        //     localBcInfo.AmountReward = amountReward;
        //     localBcInfo.NumBloks = lastBlock.Height;


        //     // add 10 txns
        //     var trans = Transaction.GetTransactions(1, 10);
        //     if (trans is not null)
        //     {
        //         foreach (Transaction txn in trans)
        //         {
        //             TxnModel mdl = ConvertTxnModel(txn);
        //             localBcInfo.Txns.Add(mdl);
        //         }
        //     }

        //     // add 10 blocks
        //     var blcks = Blockchain.GetBlocks(1, 10);
        //     if (blocks is not null)
        //     {
        //         foreach (Block block in blcks)
        //         {
        //             BlockModel mdl = ConvertBlockForList(block);
        //             localBcInfo.Blocks.Add(mdl);
        //         }
        //     }
        //     bcInfo = localBcInfo;
        //     Console.WriteLine("== TPS {0}", localBcInfo.Tps);
        // }

    }
}

