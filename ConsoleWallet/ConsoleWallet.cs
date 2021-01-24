using System;
using System.Globalization;
using System.Threading.Tasks;
using GrpcService;
using static GrpcService.BChainService;

namespace Main
{
    public class ConsoleWallet
    {
        readonly BChainServiceClient service;
        public Account account;
        public ConsoleWallet(BChainServiceClient service)
        {
            this.service = service;
            MenuItem();
            GetInput();
        }
        private void MenuItem()
        {

            if (account == null)
            {
                Console.Clear();
                Console.WriteLine("\n\n\n");
                Console.WriteLine("                    UBUDKUS COIN WALLET ");
                Console.WriteLine("============================================================");
                Console.WriteLine("  Address: - ");
                Console.WriteLine("============================================================");
                Console.WriteLine("                    1. Create Account");
                Console.WriteLine("                    2. Restore Account");
                Console.WriteLine("                    9. Exit");
                Console.WriteLine("------------------------------------------------------------");
            }
            else
            {
                Console.Clear();
                Console.WriteLine("\n\n\n");
                Console.WriteLine("                    UBUDKUS COIN WALLET ");
                Console.WriteLine("============================================================");
                Console.WriteLine("  Address: {0}", account.GetAddress());
                Console.WriteLine("============================================================");
                Console.WriteLine("                    1. Create Account");
                Console.WriteLine("                    2. Restore Account");
                Console.WriteLine("                    3. Send Coin");
                Console.WriteLine("                    4. Check Balance");
                Console.WriteLine("                    5. Transaction History");
                Console.WriteLine("                    9. Exit");
                Console.WriteLine("------------------------------------------------------------");
                
            }
        }

        private void GetInput()
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

        private void DoSendCoin()
        {
            Console.Clear();
            Console.WriteLine("\n\n\n\nTransfer Coin");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");

            Console.WriteLine("Sender address:");
            string sender = account.GetAddress();
            Console.WriteLine(sender);


            Console.WriteLine("\nPlease enter the recipient address!:");
            string recipient = Console.ReadLine();

            Console.WriteLine("\nPlease enter the amount (number)!:");
            string strAmount = Console.ReadLine();

            Console.WriteLine("\nPlease enter fee (number)!:");
            string strFee = Console.ReadLine();
            double amount;

            if (string.IsNullOrEmpty(sender) ||
                string.IsNullOrEmpty(recipient) ||
                string.IsNullOrEmpty(strAmount) ||
                string.IsNullOrEmpty(strFee))
            {

                Console.WriteLine("\n\nError, Please input all data: sender, recipient, amount and fee!\n");
                return;
            }

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
            try
            {
                fee = float.Parse(strFee);
            }
            catch
            {
                Console.WriteLine("\nError! You have inputted the wrong value for the fee!");
                return;
            }


            var response = service.GetBalance(new AccountRequest
            {
                Address = sender
            });

            var senderBalance = response.Balance;


            if ((amount + fee) > senderBalance)
            {
                Console.WriteLine("\nError! Sender ({0}) don't have enough balance!", sender);
                Console.WriteLine("Sender ({0}) balance is {1}", sender, senderBalance);
                return;
            }

            var trxin = new TrxInput
            {
                SenderAddress = account.GetAddress(),
                TimeStamp = DateTime.Now.Ticks
            };

            var trxOut = new TrxOutput
            {
                RecipientAddress = recipient,
                Amount = amount,
                Fee = fee,
            };

            var trxHash = Utils.GetTransactionHash(trxin, trxOut);
            var signature = account.CreateSignature(trxHash);

            trxin.Signature = signature;

            var sendRequest = new SendRequest
            {
                TrxId = trxHash,
                PublicKey = account.GetPubKeyHex(),
                TrxInput = trxin,
                TrxOutput = trxOut
            };

            try
            {
                var responseSend = service.SendCoin(sendRequest);

                if (responseSend.Result.ToLower() == "success")
                {
                    Console.Clear();
                    Console.WriteLine("\n\n\n\nTransaction has send to Blockchain.!.");
                    Console.WriteLine("Sender: {0}", sender);
                    Console.WriteLine("Recipient {0}", recipient);
                    Console.WriteLine("Amount: {0}", amount);
                    Console.WriteLine("Fee: {0}", fee);
                    Console.WriteLine("-------------------");
                    Console.WriteLine("Need around 30 second to be processed!");
                }
                else
                {
                    Console.WriteLine("Error: {0}", responseSend.Result);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }

        }

        private void DoRestore()
        {
            Console.Clear();
            Console.WriteLine("Restore Wallet");
            Console.WriteLine("Please enter Screet number:");
            string screet = Console.ReadLine();

            if (string.IsNullOrEmpty(screet))
            {
                Console.WriteLine("\n\nError, Please input secreet number!\n");
                return;
            }


            account = new Account(screet);
            WalletInfo();

         
        }

        private void DoCreateAccount()
        {
      
            account = new Account();
            WalletInfo();
           
        }

        private void WalletInfo()
        {
            Console.Clear();
            Console.WriteLine("\n\n\nYour Wallet");
            Console.WriteLine("======================");
            Console.WriteLine("\nADDRESS:\n{0}", account.GetAddress());
            Console.WriteLine("\nPUBLIC KEY:\n{0}", account.PubKey);
            Console.WriteLine("\nSECREET NUMBER:\n{0}", account.SecretNumber);
            Console.WriteLine("\n - - - - - - - - - - - - - - - - - - - - - - ");
            Console.WriteLine("*** save secreet number!                   ***");
            Console.WriteLine("*** use secreet number to restore account! ***");
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
            string address = account.GetAddress();
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

        private void DoGetBalance()
        {

            string address = account.GetAddress();
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


    }
}
