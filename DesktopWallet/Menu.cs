                using System;
                using System.Collections.Generic;
                using System.Globalization;
                using System.Linq;
            using System.Runtime.InteropServices;
            using Grpc.Net.Client;
            using grpcservice.Protos;

            namespace DesktopWallet
                {
                    public class Menu
                    {
                        static GrpcChannel channel;

                        public static void DisplayMenu(GrpcChannel cnl)
                        {
                            channel = cnl;
                            MenuScreen();
                            GetInputFromUser();

                        }

                        private static void MenuScreen()
                        {
                            Console.Clear();
                            Console.WriteLine("\n\n\n\n\n\n   UBUDKUS COIN MENU ");
                            Console.WriteLine("=========================");
                            Console.WriteLine("1. Genesis Block");
                            Console.WriteLine("2. Last Block");
                            Console.WriteLine("3. Send Coin");
                            Console.WriteLine("4. Create Block (mining)");
                            Console.WriteLine("5. Check Balance");
                            Console.WriteLine("6. Transaction History");
                            Console.WriteLine("7. Blockchain Explorer");
                            Console.WriteLine("8. Exit");
                            Console.WriteLine("=========================");
                        }

                        private static void GetInputFromUser()
                        {
                            int selection = 0;
                            while (selection != 20)
                            {
                                switch (selection)
                                {
                                    case 1:
                                        DoGenesisBlock();

                                        break;
                                    case 2:
                                        DoLastBlock();

                                        break;

                                    case 3:
                                        DoSendCoin();

                                        break;

                                    case 4:

                                        DoCreateBlock();

                                        break;

                                    case 5:
                                        DoGetBalance();

                                        break;
                                    case 6:
                                        DoGetTransactionHistory();


                                        break;
                                    case 7:
                                        DoShowBlockchain();

                                        break;

                                    case 8:
                                        DoExit();
                                        break;
                                }

                                if (selection != 0)
                                {
                                    Console.WriteLine("\n===== Press enter to continue! =====");
                                    string strKey = Console.ReadLine();
                                    if (strKey != null)
                                    {
                                        Console.Clear();
                                        MenuScreen();

                                    }
                                }

                                Console.WriteLine("\n**** Please select menu!!! *****");
                                string action = Console.ReadLine();
                                try
                                {
                                    selection = int.Parse(action);

                                }

                                catch
                                {
                                    selection = 0;
                                    Console.Clear();
                                    MenuScreen();
                                }
                            }

                        }

                        private static void DoExit()
                        {
                            Console.Clear();
                            Console.WriteLine("\n\nApplication closed!\n");
                            Environment.Exit(0);
                        }

                        private static void DoShowBlockchain()
                        {
                            Console.Clear();
                            Console.WriteLine("\n\n\nBlockchain Explorer");
                            Console.WriteLine("Time: {0}", DateTime.Now);
                            Console.WriteLine("======================");


                        }

                        private static void DoGetTransactionHistory()
                        {
                            Console.Clear();
                            Console.WriteLine("Get Transaction History");
                            Console.WriteLine("Please enter name:");
                            var name = Console.ReadLine();

                            if (string.IsNullOrEmpty(name))
                            {
                                Console.WriteLine("\n\nError, Please input name!\n");
                                return;
                            }

                            Console.Clear();
                            Console.WriteLine("Transaction History for {0}", name);
                            Console.WriteLine("Time: {0}", DateTime.Now);
                            Console.WriteLine("======================");
   
        
                        }

                        private static void DoGetBalance()
                        {
                            Console.Clear();
                            Console.WriteLine("Get Balance Account");
                            Console.WriteLine("Please enter name:");
                            string name = Console.ReadLine();

                            if (string.IsNullOrEmpty(name))
                            {
                                Console.WriteLine("\n\nError, Please input name!\n");
                                return;
                            }

                            Console.Clear();
                            Console.WriteLine("Balance for {0}", name);
                            Console.WriteLine("Time: {0}", DateTime.Now);
                            Console.WriteLine("======================");
                        }

                        private static void DoLastBlock()
                        {
                            Console.Clear();
                            Console.WriteLine("\n\n\nLast Block");
                            Console.WriteLine("Time: {0}", DateTime.Now);
                            Console.WriteLine("======================");


                            var block = new BlockSrv.BlockSrvClient(channel);

                            var response = block.LastBlock(new EmptyRequest());
                            Console.WriteLine(response.Message);


                        }

                        private static void DoGenesisBlock()
                        {
                            Console.Clear();
                            Console.WriteLine("\n\n\n\nGenesis Block");
                            Console.WriteLine("Time: {0}", DateTime.Now);
                            Console.WriteLine("======================");

           
                            //var hello = new Greeter.GreeterClient(channel);

                            var block = new BlockSrv.BlockSrvClient(channel);

                            var response = block.GetGenesis(new EmptyRequest());
                            Console.WriteLine(response.Message);


                        }

                        private static void DoSendCoin()
                        {
                            Console.Clear();
                            Console.WriteLine("\n\n\n\nSend Money");
                            Console.WriteLine("Time: {0}", DateTime.Now);
                            Console.WriteLine("======================");


                            Console.WriteLine("Please enter the sender name!:");
                            Console.WriteLine("(type 'ga1' or 'ga2' for first time)");
                            string sender = Console.ReadLine();

                            Console.WriteLine("Please enter the recipient name!:");
                            string recipient = Console.ReadLine();

                            Console.WriteLine("Please enter the amount (number)!:");
                            string strAmount = Console.ReadLine();

                            Console.WriteLine("Please enter fee (number)!:");
                            string strFee = Console.ReadLine();
                            double amount;

                            // validate input
                            if (string.IsNullOrEmpty(sender) ||
                                string.IsNullOrEmpty(recipient) ||
                                string.IsNullOrEmpty(strAmount) ||
                                string.IsNullOrEmpty(strFee))
                            {

                                Console.WriteLine("\n\nError, Please input all data: sender, recipient, amount and fee!\n");
                                return;
                            }

                            // validate amount
                            try
                            {
                                amount = double.Parse(strAmount);
                            }
                            catch
                            {
                                Console.WriteLine("\nError! You have inputted the wrong value for  the amount!");
                                return;
                            }

                            double fee;
                            // validate fee
                            try
                            {
                                fee = float.Parse(strFee);
                            }
                            catch
                            {
                                Console.WriteLine("\nError! You have inputted the wrong value for the fee!");
                                return;
                            }

                            // validating fee
                            // asume max fee is 50% of amount
                            if (fee > (0.5 * amount))
                            {
                                Console.WriteLine("\nError! You have inputted the fee to high, max fee 50% of amount!");
                                return;
                            }



                        }

                        private static void DoCreateBlock()
                        {
                            Console.Clear();
                            Console.WriteLine("\n\n\nCreate Block");
                            Console.WriteLine("======================");
                        }


                    }
                }
