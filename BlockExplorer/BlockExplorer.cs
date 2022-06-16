using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static UbudKusCoin.Grpc.TransactionService;
using static UbudKusCoin.Grpc.BlockService;
using static UbudKusCoin.Grpc.AccountService;
using Grpc.Net.Client;
using UbudKusCoin.Grpc;
using UbudKusCoin.BlockExplorer.Others;

namespace UbudKusCoin.BlockExplorer
{
    public class BlockExplorer
    {
        private readonly AccountServiceClient _accountService;
        private readonly BlockServiceClient _blockService;
        private readonly TransactionServiceClient _transactionService;

        public BlockExplorer(GrpcChannel channel)
        {
            _accountService = new AccountServiceClient(channel);
            _blockService = new BlockServiceClient(channel);
            _transactionService = new TransactionServiceClient(channel);
            MenuItem();
            GetInput();
        }

        private void MenuItem()
        {
            Console.Clear();
            Console.WriteLine("\n\n\n");
            Console.WriteLine("                    UBUDKUS COIN EXPLORER ");
            Console.WriteLine("============================================================");
            Console.WriteLine("                    1. First Block");
            Console.WriteLine("                    2. Last Block");
            Console.WriteLine("                    3. Show All Block");
            Console.WriteLine("                    9. Exit");
            Console.WriteLine("------------------------------------------------------------");
        }

        private void GetInput()
        {
            int selection = 0;
            while (selection != 20)
            {
                switch (selection)
                {
                    case 1:
                        ShowFirstBlock();

                        break;
                    case 2:
                        ShowLastBlock();

                        break;

                    case 3:
                        ShowAllBlocks();
                        break;

                    case 9:
                        Exit();
                        break;
                }

                if (selection != 0)
                {
                    Console.WriteLine("\n===== Press enter to continue! =====");
                    string strKey = Console.ReadLine();
                    if (strKey != null)
                    {
                        Console.Clear();
                        MenuItem();
                    }
                }

                Console.WriteLine("\n**** Please select menu!!! *****");
                
                string action = Console.ReadLine();
                if (!int.TryParse(action, out selection))
                {
                    selection = 0;
                    Console.Clear();
                    MenuItem();
                }
            }
        }

        private void ShowLastBlock()
        {
            try
            {
                Console.Clear();
                Console.WriteLine("\n\n\n\nLast Block");
                Console.WriteLine("- Time: {0}", DateTime.Now);
                Console.WriteLine("======================");
                
                var block = _blockService.GetLast(new EmptyRequest());
                PrintBlock(block);

                Console.WriteLine("--------------\n");
            }
            catch
            {
                Console.WriteLine(" error!, {0}", "Please check the UbudKusCoin node. It needs to be running!");
            }
        }

        private void ShowFirstBlock()
        {
            Console.Clear();
            Console.WriteLine("\n\n\n\nGenesis Block");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");

            try
            {
                var block = _blockService.GetFirst(new EmptyRequest());

                PrintBlock(block);

                Console.WriteLine("--------------\n");
            }
            catch
            {
                Console.WriteLine(" error!, {0}", "Please check the UbudKusCoin node. It needs to be running!");
            }
        }

        private static void PrintBlock(Block block)
        {
            Console.WriteLine("Height        : {0}", block.Height);
            Console.WriteLine("Version       : {0}", block.Version);
            Console.WriteLine("Timestamp     : {0}", Utils.ToDateTime(block.TimeStamp));
            Console.WriteLine("Hash          : {0}", block.Hash);
            Console.WriteLine("Merkle Hash   : {0}", block.MerkleRoot);
            Console.WriteLine("Prev. Hash    : {0}", block.PrevHash);
            Console.WriteLine("Validator     : {0}", block.Validator);
            Console.WriteLine("Difficulty    : {0}", block.Difficulty);
            Console.WriteLine("Num of Txs    : {0}", block.NumOfTx);
            Console.WriteLine("Total Amount  : {0}", block.TotalAmount);
            Console.WriteLine("Total Fee     : {0}", block.TotalReward);
            Console.WriteLine("Size          : {0}", block.Size);
            Console.WriteLine("Build Time    : {0}", block.BuildTime);
            
            var transactions = JsonConvert.DeserializeObject<List<Transaction>>(block.Transactions);
            Console.WriteLine("Transactions: ");
            foreach (var Txn in transactions)
            {
                Console.WriteLine("   ID          : {0}", Txn.Hash);
                Console.WriteLine("   Timestamp   : {0}", Utils.ToDateTime(Txn.TimeStamp));
                Console.WriteLine("   Sender      : {0}", Txn.Sender);
                Console.WriteLine("   Recipient   : {0}", Txn.Recipient);
                Console.WriteLine("   Amount      : {0}", Txn.Amount.ToString("N", CultureInfo.InvariantCulture));
                Console.WriteLine("   Fee         : {0}", Txn.Fee.ToString("N4", CultureInfo.InvariantCulture));
                Console.WriteLine("   - - - - - - ");
            }
        }

        private static async void Exit()
        {
            Console.Clear();
            Console.WriteLine("\n\nApplication closed!\n");
            await Task.Delay(2000);
            Environment.Exit(0);
        }
        
        private void ShowAllBlocks()
        {
            Console.Clear();
            Console.WriteLine("\n\n\nBlockchain Explorer");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");
            Console.WriteLine("\nPlease enter the page number!:");
            
            // validate input
            string strPageNumber = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(strPageNumber))
            {
                Console.WriteLine("\n\nError, Please input the page number!\n");
                return;
            }

            if (!int.TryParse(strPageNumber, out var pageNumber))
            {
                Console.WriteLine("\n\nError, page is not a number!\n");
                return;
            }

            try
            {
                var response = _blockService.GetRange(new BlockParams
                {
                    PageNumber = pageNumber,
                    ResultPerPage = 5
                });

                foreach (var block in response.Blocks)
                {
                    PrintBlock(block);
                    Console.WriteLine("--------------\n");
                }
            }
            catch
            {
                Console.WriteLine(" error!, {0}", "Please check the UbudKusCoin node, it needs to be running!");
            }
        }
    }
}