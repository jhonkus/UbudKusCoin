using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using GrpcService;
using Newtonsoft.Json;
using static GrpcService.BChainService;

namespace DesktopWallet
{
    public class ConsoleWallet
    {
        readonly BChainServiceClient service;
        public ConsoleWallet(BChainServiceClient service)
        {
            this.service = service;
            MenuItem();
            GetInput();
        }
    

        private static void MenuItem()
        {
            Console.Clear();
            Console.WriteLine("\n\n\n");
            Console.WriteLine("                UBUDKUS COIN WALLET ");
            Console.WriteLine("==================================================");
            Console.WriteLine("  Address: {0}", Wallet.GetAddress());
            Console.WriteLine("==================================================");
            Console.WriteLine("                1. Create Account");
            Console.WriteLine("                2. Restore Account");
            Console.WriteLine("                3. Send Coin");
            Console.WriteLine("                4. Check Balance");
            Console.WriteLine("                5. Transaction History");
            Console.WriteLine("                9. Exit");
            Console.WriteLine("--------------------------------------------------");
            //Console.WriteLine("               ");
            Console.WriteLine("                6. Block Explorer (Not part of wallet)");
        }

        private  void GetInput()
        {
            int selection = 0;
            while (selection != 20)
            {
                switch (selection)
                {
                    case 1:
                        DoCreateAccount();

                        break;
                    case 2:
                        DoRestore();

                        break;

                    case 3:
                        DoSendCoin();

                        break;

                    case 4:

                        DoGetBalance();

                        break;

                    case 5:
                        DoGetTransactionHistory();
                        break;


                    case 6:
                        DoShowBlockchain();
                        break;

                    case 9:
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
                        MenuItem();

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
                    MenuItem();
                }
            }

        }

        private void  DoSendCoin()
        {
            Console.Clear();
            Console.WriteLine("\n\n\n\nTransfer Coin");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");

            Console.WriteLine("Sender address:");
            //Console.WriteLine("(type 'ga1' or 'ga2' for first time)");
            string sender = Wallet.GetAddress();
            Console.WriteLine(sender);


            Console.WriteLine("\nPlease enter the recipient address!:");
            string recipient = Console.ReadLine();

            Console.WriteLine("\nPlease enter the amount (number)!:");
            string strAmount = Console.ReadLine();

            Console.WriteLine("\nPlease enter fee (number)!:");
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

            float fee;
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

            Console.WriteLine("=== amount: {0}", amount);
            Console.WriteLine("=== fee: {0}", fee);

            //get sender balance
            var response = service.GetBalance(new AccountRequest
            {
                Address = sender
            });

            var senderBalance = response.Balance;
       
            Console.WriteLine("=== SenderBalane: {0}", senderBalance);

            //validate amount and fee
            if ((amount + fee) > senderBalance)
            {
                Console.WriteLine("\nError! Sender ({0}) don't have enough balance!", sender);
                Console.WriteLine("Sender ({0}) balance is {1}", sender, senderBalance);
                return;
            }

            var trxin = new TrxInput
            {
                SenderAddress = Wallet.GetAddress(),
                TimeStamp = DateTime.Now.Ticks
            };

            var trxOut = new TrxOutput
            {
                RecipientAddress = recipient,
                Amount = amount,
                Fee = fee,
            };

            var trxHash = Utils.GetTransactionHash(trxin, trxOut);
            var signature = Wallet.Sign(trxHash);

            trxin.Signature = signature;

            var sendRequest = new SendRequest
            {
                TrxId = trxHash,
                PublicKey = Wallet.GetPublicKeyHex(),
                TrxInput = trxin,
                TrxOutput = trxOut
            };

            try
            {
                var responseSend = service.SendCoin(sendRequest);

                if (responseSend.Result.ToLower() == "success")
                {
                    //  Console.Clear();
                    Console.WriteLine("\n\n\n\nHoree, transaction added to transaction pool!.");
                    Console.WriteLine("Sender: {0}", sender);
                    Console.WriteLine("Recipient {0}", recipient);
                    Console.WriteLine("Amount: {0}", amount);
                    Console.WriteLine("Fee: {0}", fee);
                }
                else
                {
                    Console.WriteLine("Error: {0}", responseSend.Result);
                }

            }
            catch(Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
       
        }

        private static void DoRestore()
        {
            Console.Clear();
            Console.WriteLine("Restore Account");
            Console.WriteLine("Please enter Screet number:");
            string screet = Console.ReadLine();

            if (string.IsNullOrEmpty(screet))
            {
                Console.WriteLine("\n\nError, Please input secreet number!\n");
                return;
            }

            try
            {
                Console.Clear();
                Console.WriteLine("\n\n\nYour Account");
                Console.WriteLine("======================");
                Account acc = Wallet.Restore(screet);

                Console.WriteLine("\nADDRESS:\n{0}", acc.Address);
                Console.WriteLine("\nPUBLIC KEY:\n{0}", acc.PublicKey);
                Console.WriteLine("\nSECREET NUMBER:\n{0}", Wallet.SECREET_NUMBER);
                Console.WriteLine("\n - - - - - - - - - - - - - - - - - - - - - - ");
                Console.WriteLine("*** save secreet number!                   ***");
                Console.WriteLine("*** use secreet number to restore account! ***");
               

            }
            catch (Exception e)
            {
                Console.WriteLine("Error!: {0}", e.Message);
            }
        }

        private static void DoCreateAccount()
        {
            Console.Clear();
            Console.WriteLine("\n\n\nYour Account");
            Console.WriteLine("======================");
            try
            {

                Account acc =  Wallet.Create();

                Console.WriteLine("\nADDRESS:\n{0}", acc.Address);
                Console.WriteLine("\nPUBLIC KEY:\n{0}", acc.PublicKey);
                Console.WriteLine("\nSECREET NUMBER:\n{0}", Wallet.SECREET_NUMBER);
                Console.WriteLine("\n - - - - - - - - - - - - - - - - - - - - - - ");
                Console.WriteLine("*** save secreet number!                   ***");
                Console.WriteLine("*** use secreet number to restore account! ***");


            }
            catch (Exception e)
            {
                Console.WriteLine("\nError! {0}", e.Message);
                return;
            }
        }

        private static async void DoExit()
        {
            Console.Clear();
            Console.WriteLine("\n\nApplication closed!\n");
            await Task.Delay(2000);
            Environment.Exit(0);
        }

        private void DoGetTransactionHistory()
        {
            string address = Wallet.GetAddress();
            if (string.IsNullOrEmpty(address))
            {
                Console.WriteLine("\n\nError, Address empty, please create account first!\n");
                return;
            }

            Console.Clear();
            Console.WriteLine("Transaction History for {0}", address);
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");

            try
            {
                var response = service.GetTransactions(new AccountRequest
                {
                    Address = address
                });


                if (response != null && response.Transactions != null)
                {
                    foreach (var trx in response.Transactions)
                    {
                        Console.WriteLine("ID          : {0}", trx.TrxID);
                        Console.WriteLine("Timestamp   : {0}", trx.TimeStamp.ConvertToDateTime());
                        Console.WriteLine("Sender      : {0}", trx.Sender);
                        Console.WriteLine("Recipient   : {0}", trx.Recipient);
                        Console.WriteLine("Amount      : {0}", trx.Amount.ToString("N", CultureInfo.InvariantCulture));
                        Console.WriteLine("Fee         : {0}", trx.Fee.ToString("N4", CultureInfo.InvariantCulture));
                        Console.WriteLine("--------------\n");

                    }
                }
                else
                {
                    Console.WriteLine("\n---- no record found! ---");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

        }

        private  void DoGetBalance()
        {

            string address = Wallet.GetAddress();
            if (string.IsNullOrEmpty(address))
            {
                Console.WriteLine("\n\nError, Address empty, please create account first!\n");
                return;
            }

            Console.Clear();
            Console.WriteLine("Balance for {0}", address);
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");
            try
            {
                var response = service.GetBalance(new AccountRequest
                {
                    Address = address
                });
                Console.WriteLine("Balance: {0}", response.Balance.ToString("N", CultureInfo.InvariantCulture));

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }       

        }


        private  void DoShowBlockchain()
        {
            Console.Clear();
            Console.WriteLine("\n\n\nBlockchain Explorer");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");


            Console.WriteLine("\nPlease enter the page number!:");
            string strPageNumber = Console.ReadLine();



            var pageNumber = 0;
            // validate input
            if (string.IsNullOrEmpty(strPageNumber))
            {

                Console.WriteLine("\n\nError, Please input page number!\n");
                return;
            }
            try
            {
                pageNumber = int.Parse(strPageNumber);
            }
            catch(Exception e)
            {
                Console.WriteLine("\n\nError, Please input number {0}!\n", e.Message);
                return;
            }

            try
            {
                var response = service.GetBlocks(new BlockRequest
                {
                    PageNumber = pageNumber,
                    ResultPerPage = 5
                });

                foreach (var block in response.Blocks)
                {
                    //Console.WriteLine("ID          :{0}", block.ID);
                    Console.WriteLine("Height      : {0}", block.Height);
                    Console.WriteLine("Timestamp   : {0}", block.TimeStamp.ConvertToDateTime());
                    Console.WriteLine("Prev. Hash  : {0}", block.PrevHash);
                    Console.WriteLine("Hash        : {0}", block.Hash);



                    //if (block.Height == 1)
                    //{
                    //    Console.WriteLine("Transactions : {0}", block.Transactions);
                    //}
                    //else
                    //{
                        var transactions = JsonConvert.DeserializeObject<List<TrxModel>>(block.Transactions);
                        Console.WriteLine("Transactions:");
                        foreach (var trx in transactions)
                        {
                            Console.WriteLine("   Timestamp   : {0}", trx.TimeStamp.ConvertToDateTime());
                            Console.WriteLine("   Sender      : {0}", trx.Sender);
                            Console.WriteLine("   Recipient   : {0}", trx.Recipient);
                            Console.WriteLine("   Amount      : {0}", trx.Amount.ToString("N", CultureInfo.InvariantCulture));
                            Console.WriteLine("   Fee         : {0}", trx.Fee.ToString("N4", CultureInfo.InvariantCulture));
                            Console.WriteLine("   - - - - - - ");

                        }
                    //}


                    Console.WriteLine("--------------\n");

                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


        }


    }
}
