using System;
using System.Threading.Tasks;
using Grpc.Core;
using System.Collections.Generic;
using System.Text.Json;
using UbudKusCoin.Models;
using UbudKusCoin.DB;

namespace UbudKusCoin.Facade
{
    public class ReportFacade
    {
      
        public ReportFacade()
        {
            Console.WriteLine("Report initilize ....");
        }

        //     BcInfoResponse bcInfo = new BcInfoResponse();
        //     public Task<BcInfoResponse> GetBchainInfo()
        //     {
        //         if (bcInfo.NumBloks == 0)
        //         {
        //             BuildReport();
        //         }

        //         return Task.FromResult(bcInfo);
        //     }
        public void BuildReport()
        {
            Console.WriteLine("== build report not implement yet==");
            // var localBcInfo = new BcInfoResponse();
            // Console.WriteLine("make report");
            // var firstBlock = Blockchain.GetGenesisBlock();
            // var lastBlock = Blockchain.GetLastBlock();
            // var blocks = ServicePool.DbService.BlockRepo.GetAll();

            // var amountTnxs = 0d;
            // var numTnxs = 0L;
            // var amountReward = 0d;
            // var transactions = new List<Transaction>();



            // if (bcInfo.NumBloks == 0)
            // {
            //     // calculate from begining
            //     foreach (var block in blocks.FindAll())
            //     {
            //         amountTnxs += block.TotalAmount;
            //         numTnxs += block.NumOfTx;
            //         amountReward += block.TotalReward;
            //     }
            //     localBcInfo.AmountTxns = amountTnxs;
            //     localBcInfo.NumTxns = numTnxs;
            //     localBcInfo.AmountReward = amountReward;
            //     localBcInfo.NumBloks = lastBlock.Height;
            //     localBcInfo.Tps = (double)lastBlock.NumOfTx / (double)30;

            //     // add 10 txns
            //     var trans1 = ServicePool.DbService.TxnRepo.GetSome(1, 10);
            //     if (trans1 is not null)
            //     {
            //         int i = 1;
            //         foreach (Transaction txn in trans1)
            //         {
            //             if (i <= 10)
            //             {
            //                 TxnModel mdl = Utils.ConvertTxnModel(txn);
            //                 localBcInfo.Txns.Add(mdl);
            //             }
            //             i++;
            //         }
            //     }

            //     // add 10 blocks
            //     var blocks1 = ServicePool.DbService.BlockRepo.GetSome(1, 10);
            //     if (blocks1 is not null)
            //     {
            //         int i = 1; // to prevent more than 10
            //         foreach (Block block in blocks1)
            //         {
            //             if (i <= 10)
            //             {
            //                 BlockModel mdl = Utils.ConvertBlockForList(block);
            //                 localBcInfo.Blocks.Add(mdl);
            //             }
            //             i++;
            //         }
            //     }

            //     bcInfo = localBcInfo;
            //     Console.WriteLine("== TPS {0}", localBcInfo.Tps);
            //     return;
            // }

            // amountTnxs = (bcInfo.AmountTxns + lastBlock.TotalAmount);
            // numTnxs = (bcInfo.NumTxns + lastBlock.NumOfTx);
            // amountReward = (bcInfo.AmountReward + lastBlock.TotalReward);
            // localBcInfo.AmountTxns = amountTnxs;
            // localBcInfo.NumTxns = numTnxs;
            // localBcInfo.Tps = (double)lastBlock.NumOfTx / (double)30;
            // localBcInfo.AmountReward = amountReward;
            // localBcInfo.NumBloks = lastBlock.Height;


            // // add 10 txns
            // var trans = ServicePool.DbService.TxnRepo.GetSome(1, 10);
            // if (trans is not null)
            // {
            //     foreach (Transaction txn in trans)
            //     {
            //         TxnModel mdl = Utils.ConvertTxnModel(txn);
            //         localBcInfo.Txns.Add(mdl);
            //     }
            // }

            // // add 10 blocks
            // var blcks = ServicePool.DbService.BlockRepo.GetSome(1, 10);
            // if (blocks is not null)
            // {
            //     foreach (Block block in blcks)
            //     {
            //         BlockModel mdl = Utils.ConvertBlockForList(block);
            //         localBcInfo.Blocks.Add(mdl);
            //     }
            // }
            // bcInfo = localBcInfo;
            // Console.WriteLine("== TPS {0}", localBcInfo.Tps);
        }

        // public override Task<AccountResponse> GetAccount(CommonRequest request, ServerCallContext context)
        // {
        //     AccountResponse response = new AccountResponse();


        //     // 1. get all transaction bellong this account
        //     var transactions = Transaction.GetAccountTransactions(request.Address);

        //     if (transactions is null)
        //     {
        //         response.Transactions.Add(new TxnModel()); //no txn
        //     }
        //     else
        //     {

        //         foreach (Transaction Txn in transactions)
        //         {
        //             TxnModel mdl = ConvertTxnModel(Txn);
        //             response.Transactions.Add(mdl);
        //         }
        //     }

        //     // get Blocks by validator
        //     var blocks = Blockchain.GetBlocksByValidator(request.Address);
        //     if (blocks is null)
        //     {
        //         response.Blocks.Add(new BlockModel()); //no txn
        //     }
        //     else
        //     {
        //         response.NumBlockValidate = 0;
        //         foreach (Block block in blocks)
        //         {
        //             BlockModel mdl = ConvertBlockForList(block);
        //             response.Blocks.Add(mdl);
        //             // count number of block validated by this account
        //             response.NumBlockValidate += 1;
        //         }


        //     }

        //     // Get Account Balance
        //     var balance = Transaction.GetBalance(request.Address);
        //     response.Balance = balance;

        //     return Task.FromResult(response);
        // }


        //        public override Task<BcInfoResponse> GetBchainInfo(CommonRequest request, ServerCallContext context)
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


        //  public static void BuildReport()
        //         {
        //             var localBcInfo = new BcInfoResponse();
        //             Console.WriteLine("make report");
        //             var lastBlock = Blockchain.GetLastBlock();
        //             var blocks = Blockchain.GetBlocks();

        //             var amountTnxs = 0d;
        //             var numTnxs = 0L;
        //             var amountReward = 0d;
        //             var transactions = new List<Transaction>();



        //             if (bcInfo.NumBloks == 0)
        //             {
        //                 // calculate from begining
        //                 foreach (var block in blocks.FindAll())
        //                 {
        //                     amountTnxs += block.TotalAmount;
        //                     numTnxs += block.NumOfTx;
        //                     amountReward += block.TotalReward;
        //                 }
        //                 localBcInfo.AmountTxns = amountTnxs;
        //                 localBcInfo.NumTxns = numTnxs;
        //                 localBcInfo.AmountReward = amountReward;
        //                 localBcInfo.NumBloks = lastBlock.Height;
        //                 localBcInfo.Tps = lastBlock.NumOfTx / 30;

        //                 // add 10 txns
        //                 var trans1 = Transaction.GetTransactions(1, 10);
        //                 if (trans1 is not null)
        //                 {
        //                     foreach (Transaction txn in trans1)
        //                     {
        //                         TxnModel mdl = ConvertTxnModel(txn);
        //                         localBcInfo.Txns.Add(mdl);
        //                     }
        //                 }

        //                 // add 10 blocks
        //                 var blocks1 = Blockchain.GetBlocks(1, 10);
        //                 if (blocks1 is not null)
        //                 {
        //                     foreach (Block block in blocks1)
        //                     {
        //                         BlockModel mdl = ConvertBlockForList(block);
        //                         localBcInfo.Blocks.Add(mdl);
        //                     }
        //                 }

        //                 bcInfo = localBcInfo;
        //                 Console.WriteLine("== TPS {0}", localBcInfo.Tps);
        //                 return;
        //             }

        //             amountTnxs = (bcInfo.AmountTxns + lastBlock.TotalAmount);
        //             numTnxs = (bcInfo.NumTxns + lastBlock.NumOfTx);
        //             amountReward = (bcInfo.AmountReward + lastBlock.TotalReward);
        //             localBcInfo.AmountTxns = amountTnxs;
        //             localBcInfo.NumTxns = numTnxs;
        //             localBcInfo.Tps = lastBlock.NumOfTx / 30;
        //             localBcInfo.AmountReward = amountReward;
        //             localBcInfo.NumBloks = lastBlock.Height;


        //             // add 10 txns
        //             var trans = Transaction.GetTransactions(1, 10);
        //             if (trans is not null)
        //             {
        //                 foreach (Transaction txn in trans)
        //                 {
        //                     TxnModel mdl = ConvertTxnModel(txn);
        //                     localBcInfo.Txns.Add(mdl);
        //                 }
        //             }

        //             // add 10 blocks
        //             var blcks = Blockchain.GetBlocks(1, 10);
        //             if (blocks is not null)
        //             {
        //                 foreach (Block block in blcks)
        //                 {
        //                     BlockModel mdl = ConvertBlockForList(block);
        //                     localBcInfo.Blocks.Add(mdl);
        //                 }
        //             }
        //             bcInfo = localBcInfo;
        //             Console.WriteLine("== TPS {0}", localBcInfo.Tps);
        //         }

    }
}
