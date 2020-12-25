using System;
using Models;
using Newtonsoft.Json;

namespace Client
{
    public class Menu
    {
        private  Blockchain bc;

        public Menu(Blockchain blockchain)
        {
            this.bc = blockchain;
        }

        private void MenuScreen()
        {
            Console.WriteLine("=========================");
            Console.WriteLine("1. Get Genesis Block");
            Console.WriteLine("2. Get Last Block");
            Console.WriteLine("3. Send Money");
            Console.WriteLine("4. Create Block (mining)");
            Console.WriteLine("5. Get Balance");
            Console.WriteLine("6. Transaction History");
            Console.WriteLine("7. Show Blockchain");
            Console.WriteLine("8. Exit");
            Console.WriteLine("=========================");
        }

        private void GetInputFromUser()
        {
            int selection = 0;
            while (selection != 20)
            {
                switch (selection)
                {
                    case 1:
                        Console.Clear();
                        Console.WriteLine("Geting Genesis Block");
                        var genesisBlock = bc.GetGenesisBlock();
                        Console.WriteLine(JsonConvert.SerializeObject(genesisBlock, Formatting.Indented));

                        break;
                    case 2:
                        Console.Clear();
                        Console.WriteLine("Geting Last Block");
                        var lastBlock = bc.GetLastBlock();
                        Console.WriteLine(JsonConvert.SerializeObject(lastBlock, Formatting.Indented));

                        break;

                    case 3:
                        Console.Clear();
                        Console.WriteLine("Send Money");
                        Console.WriteLine("======================");


                        Console.WriteLine("Please enter the sender name");
                        string sender = Console.ReadLine();

                        Console.WriteLine("Please enter the recipient name");
                        string recipient = Console.ReadLine();

                        Console.WriteLine("Please enter the amount");
                        string amount = Console.ReadLine();

                        Console.WriteLine("Please enter fee");
                        string fee = Console.ReadLine();

                        //Create transaction
                        var newTrx = new Transaction()
                        {
                            Sender = sender,
                            Recipient = recipient,
                            Amount = Double.Parse(amount),
                            Fee = Double.Parse(fee)
                        };

                        // Add transaction to pool
                        bc.GetPool().Add(newTrx); ;

                        Console.WriteLine("Send money transaction added to Pool");


                        break;


                    case 4:
                        Console.Clear();

                        Console.WriteLine("Create Block");
                        var pool = bc.GetPool();
                        bc.AddBlock(pool.ToArray());
                        Console.WriteLine("Block created and added to Blockchain");

                        break;

                    case 5:
                        Console.WriteLine("Get Balance");
                        Console.WriteLine("Please enter name:");
                        string name = Console.ReadLine();
                        Console.Clear();
                        Console.WriteLine("Balance of {0}", name);

                        var balance = bc.GetBalance(name);
                        Console.WriteLine("Balance: {0}", balance);

                        break;
                    case 6:
                        Console.WriteLine("Transaction of");
                        break;
                    case 7:

                        // Get all blocks
                        var blocks = bc.GetBlocks();
                        Console.WriteLine("Show Blockcahin");

                        // Showing all blocks to console
                        Console.WriteLine(JsonConvert.SerializeObject(blocks, Formatting.Indented));

                        break;

                }

                if (selection != 0)
                {
                    Console.WriteLine("Please press any key to continue:");
                    string strKey = Console.ReadLine();
                    if (strKey != null)
                    {
                        Console.Clear();
                        MenuScreen();

                    }
                    Console.WriteLine("Please type number of menu item:");
                    string action = Console.ReadLine();
                    selection = int.Parse(action);
                }
                else
                {
                    Console.WriteLine("Please type number of menu item:");
                    string action = Console.ReadLine();
                    selection = int.Parse(action);
                }


            }

        }

        public void DisplayMenu(Blockchain bc)
        {

            MenuScreen();
            GetInputFromUser();

        }
    }
}
