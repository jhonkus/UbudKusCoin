using System;
using Main;
using Models;
using Newtonsoft.Json;
using Utils;

namespace Client
{
    public class Menu
    {
        private Blockchain bc;

        public Menu(Blockchain blockchain)
        {
            this.bc = blockchain;
        }

        private void MenuScreen()
        {
            Console.Clear();
            Console.WriteLine("\n\n    UBUDKUS COIN MENU ");
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
                        Console.WriteLine("\nGenesis Block");
                        Console.WriteLine("======================");
                        var genesisBlock = Blockchain.GetGenesisBlock();
                        Console.WriteLine(JsonConvert.SerializeObject(genesisBlock, Formatting.Indented));

                        break;
                    case 2:
                        Console.Clear();
                        Console.WriteLine("\nLast Block");
                        Console.WriteLine("======================");
                        var lastBlock = Blockchain.GetLastBlock();
                        Console.WriteLine(JsonConvert.SerializeObject(lastBlock, Formatting.Indented));

                        break;

                    case 3:
                        Console.Clear();
                        Console.WriteLine("\nSend Money");
                        Console.WriteLine("======================");
                        Console.WriteLine("Please input carefully, not validate yet!");

                        Console.WriteLine("Please enter the sender name!:");
                        string sender = Console.ReadLine();

                        Console.WriteLine("Please enter the recipient name!:");
                        string recipient = Console.ReadLine();

                        Console.WriteLine("Please enter the amount (number)!:");
                        string amount = Console.ReadLine();

                        Console.WriteLine("Please enter fee (number)!:");
                        string fee = Console.ReadLine();

                        //Create transaction
                        var newTrx = new Transaction()
                        {
                            TimeStamp = new DateTime().Ticks,
                            Sender = sender,
                            Recipient = recipient,
                            Amount = Double.Parse(amount),
                            Fee = Double.Parse(fee)
                        };

                        Transaction.AddToPool(newTrx);
                        Console.Clear();
                        Console.WriteLine("\nHoree, transaction added to transaction pool!.");
                        Console.WriteLine("Sender: {0}", sender);
                        Console.WriteLine("Recipient {0}", recipient);
                        Console.WriteLine("Amount: {0}", amount);
                        Console.WriteLine("Fee: {0}", fee);

                        break;


                    case 4:
                        Console.Clear();
                        Console.WriteLine("Create Block");
                        Console.WriteLine("======================");
                        var trxPool = Transaction.GetPool();
                        var numOfTrxInPool = trxPool.Count();
                        if (numOfTrxInPool <= 0)
                        {
                            Console.WriteLine("No transaction in pool, please create transaction first!");
                        }
                        else
                        {
                            var lastBlock2 = Blockchain.GetLastBlock();

                            // create block from transaction pool
                            string tempTransactions = JsonConvert.SerializeObject(trxPool.FindAll());

                            var block = new Models.Block(lastBlock2, tempTransactions);
                            Console.WriteLine("Block created and added to Blockchain");

                            Blockchain.AddBlock(block);

                            // clear mempool
                            trxPool.DeleteAll();
                        }

                        break;

                    case 5:
                        Console.Clear();
                        Console.WriteLine("Get Balance Account");
                        Console.WriteLine("Please enter name:");
                        string name = Console.ReadLine();
                        Console.Clear();
                        Console.WriteLine("Balance of {0}", name);
                        Console.WriteLine("======================");
                        var balance = Blockchain.GetBalance(name);
                        Console.WriteLine("Balance: {0}", balance);

                        break;
                    case 6:
                        Console.Clear();
                        Console.WriteLine("Get Transaction History");
                        Console.WriteLine("Please enter name:");
                        name = Console.ReadLine();
                        Console.Clear();
                        Console.WriteLine("Transaction History of {0}", name);
                        Console.WriteLine("======================");
                        var trxs = Blockchain.GetTransactionHistory(name);

                        foreach (Transaction trx in trxs)
                        {
                            Console.WriteLine("Timestamp:   {0}", trx.TimeStamp.ConvertToDateTime());
                            Console.WriteLine("Sender:      {0}", trx.Sender);
                            Console.WriteLine("Recipient:   {0}", trx.Recipient);
                            Console.WriteLine("Amount:      {0}", trx.Amount);
                            Console.WriteLine("Fee:         {0}", trx.Fee);
                            Console.WriteLine("--------------\n");

                        }


                        break;
                    case 7:
                        Console.Clear();
                        Console.WriteLine("Block in Blockchain");
                        Console.WriteLine("======================");
                        var blockchain = Blockchain.GetBlocks();
                        var results = blockchain.FindAll();
                        Console.WriteLine(JsonConvert.SerializeObject(results, Formatting.Indented));

                        break;

                    case 8:
                        Console.Clear();
                        Console.WriteLine("\n\nApplication closed!\n");
                        Environment.Exit(0);
                        break;
                }

                if (selection != 0)
                {
                    Console.WriteLine("\n===== Please press any key to continue! =====");
                    string strKey = Console.ReadLine();
                    if (strKey != null)
                    {
                        Console.Clear();
                        MenuScreen();

                    }
                }

                Console.WriteLine("\n**** Please select menu!!! *****");
                string action = Console.ReadLine();
                selection = int.Parse(action);


            }

        }

        public void DisplayMenu(Blockchain bc)
        {

            MenuScreen();
            GetInputFromUser();

        }
    }
}
