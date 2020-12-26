using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Main;
using Models;
using Newtonsoft.Json;
using Utils;

namespace Client
{
    public class Menu
    {

        public static void DisplayMenu()
        {

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
            var blockchain = Blockchain.GetBlocks();
            var blocks = blockchain.FindAll();

            foreach (Block block in blocks)
            {
                //Console.WriteLine("ID          :{0}", block.ID);
                Console.WriteLine("Height      : {0}", block.Height);
                Console.WriteLine("Timestamp   : {0}", block.TimeStamp.ConvertToDateTime());
                Console.WriteLine("Prev. Hash  : {0}", block.PrevHash);
                Console.WriteLine("Hash        : {0}", block.Hash);
               

      
                if (block.Height == 1) {
                Console.WriteLine("Transactions : {0}", block.Transactions);
                }
                else
                {
                    var transactions = JsonConvert.DeserializeObject<List<Transaction>>(block.Transactions);
                    Console.WriteLine("Transactions:");
                    foreach (Transaction trx in transactions)
                    {
                        Console.WriteLine("   Timestamp   : {0}", trx.TimeStamp.ConvertToDateTime());
                        Console.WriteLine("   Sender      : {0}", trx.Sender);
                        Console.WriteLine("   Recipient   : {0}", trx.Recipient);
                        Console.WriteLine("   Amount      : {0}", trx.Amount.ToString("N", CultureInfo.InvariantCulture));
                        Console.WriteLine("   Fee         : {0}", trx.Fee.ToString("N4", CultureInfo.InvariantCulture));
                        Console.WriteLine("   - - - - - - ");

                    }
                }


                Console.WriteLine("--------------\n");

            }


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
            var trxs = Transaction.GetHistory(name);

            if (trxs != null && trxs.Any())
            {
                foreach (Transaction trx in trxs)
                {
                    Console.WriteLine("Timestamp   : {0}", trx.TimeStamp.ConvertToDateTime());
                    Console.WriteLine("Sender      : {0}", trx.Sender);
                    Console.WriteLine("Recipient   : {0}", trx.Recipient);
                    Console.WriteLine("Amount      : {0}", trx.Amount.ToString("N", CultureInfo.InvariantCulture));
                    Console.WriteLine("Fee         : {0}", trx.Fee.ToString("N4", CultureInfo.InvariantCulture));
                    Console.WriteLine("--------------\n");

                }
            } else
            {
                Console.WriteLine("\n---- no record found! ---");
            }

        
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
            var balance = Transaction.GetBalance(name);
            Console.WriteLine("Balance: {0}", balance.ToString("N", CultureInfo.InvariantCulture));

        }

        private static void DoLastBlock()
        {
            Console.Clear();
            Console.WriteLine("\n\n\nLast Block");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");
            var block = Blockchain.GetLastBlock();

            //Console.WriteLine("ID          :{0}", block.ID);
            Console.WriteLine("Height      : {0}", block.Height);
            Console.WriteLine("Timestamp   : {0}", block.TimeStamp.ConvertToDateTime());
            Console.WriteLine("Prev. Hash  : {0}", block.PrevHash);
            Console.WriteLine("Hash        : {0}", block.Hash);

    
            var transactions = JsonConvert.DeserializeObject<List<Transaction>>(block.Transactions);
            Console.WriteLine("Transactions:");
            foreach (Transaction trx in transactions)
            {
                Console.WriteLine("   Timestamp   : {0}", trx.TimeStamp.ConvertToDateTime());
                Console.WriteLine("   Sender      : {0}", trx.Sender);
                Console.WriteLine("   Recipient   : {0}", trx.Recipient);
                Console.WriteLine("   Amount      : {0}", trx.Amount.ToString("N", CultureInfo.InvariantCulture));
                Console.WriteLine("   Fee         : {0}", trx.Fee.ToString("N4", CultureInfo.InvariantCulture));
                Console.WriteLine("   - - - - - - ");

            }
            



        }

        private static void DoGenesisBlock()
        {
            Console.Clear();
            Console.WriteLine("\n\n\n\nGenesis Block");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");
            var genesisBlock = Blockchain.GetGenesisBlock();
            Console.WriteLine(JsonConvert.SerializeObject(genesisBlock, Formatting.Indented));
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

            //get sender balance
            var senderBalance = Transaction.GetBalance(sender);

            // validate amount and fee
            if ((amount + fee) > senderBalance)
            {
                Console.WriteLine("\nError! Sender ({0}) don't have enough balance!", sender);
                Console.WriteLine("Sender ({0}) balance is {1}", sender, senderBalance);
                return;
            }


            //Create transaction
            var newTrx = new Transaction()
            {
                TimeStamp = DateTime.Now.Ticks,
                Sender = sender,
                Recipient = recipient,
                Amount = amount,
                Fee = fee
            };

            Transaction.AddToPool(newTrx);
            Console.Clear();
            Console.WriteLine("\n\n\n\nHoree, transaction added to transaction pool!.");
            Console.WriteLine("Sender: {0}", sender);
            Console.WriteLine("Recipient {0}", recipient);
            Console.WriteLine("Amount: {0}", amount);
            Console.WriteLine("Fee: {0}", fee);


        }

        private static void DoCreateBlock()
        {
            Console.Clear();
            Console.WriteLine("\n\n\nCreate Block");
            Console.WriteLine("======================");
            var trxPool = Transaction.GetPool();
            var transactions = trxPool.FindAll();
            var numOfTrxInPool = trxPool.Count();
            if (numOfTrxInPool <= 0)
            {
                Console.WriteLine("No transaction in pool, please create transaction first!");
            }
            else
            {
                var lastBlock = Blockchain.GetLastBlock();

                // create block from transaction pool
                string tempTransactions = JsonConvert.SerializeObject(transactions);

                var block = new Block(lastBlock, tempTransactions);
                Console.WriteLine("Block created and added to Blockchain");

                Blockchain.AddBlock(block);

                // move all record in trx pool to transactions table
                foreach (Transaction trx in transactions)
                {
                    Transaction.Add(trx);
                }

                // clear mempool
                trxPool.DeleteAll();
            }
        }


    }
}
