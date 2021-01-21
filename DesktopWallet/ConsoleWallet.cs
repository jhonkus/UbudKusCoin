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
                        Helper.ShowHistory();
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

        private void DoSendCoin()
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

            // validating fee
            // asume max fee is 50% of amount
            if (fee > (0.5 * amount))
            {
                Console.WriteLine("\nError! You have inputted the fee to high, max fee 50% of amount!");
                return;
            }

            //get sender balance
            //var senderBalance = Transaction.GetBalance(sender);

            // validate amount and fee
            //if ((amount + fee) > senderBalance)
            //{
            //    Console.WriteLine("\nError! Sender ({0}) don't have enough balance!", sender);
            //    Console.WriteLine("Sender ({0}) balance is {1}", sender, senderBalance);
            //    return;
            //}

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
            var signature = Utils.CreteSignature(trxHash, Wallet.CurrentKeypair);
            trxin.Signature = signature;

            var sendRequest = new SendRequest
            {
                TrxId = trxHash,
                TrxInput = trxin,
                TrxOutput = trxOut
            };

            var strTrx = JsonConvert.SerializeObject(sendRequest);
            Console.WriteLine("send request: {0}", strTrx);

            var response = service.SendCoin(sendRequest);

            if (response.Result.ToLower() == "success")
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
                Console.WriteLine("Error: {0}", response.Result);
            }
       
        }

        private static void DoRestore()
        {
            Console.Clear();
            Console.WriteLine("Restore Account");
            Console.WriteLine("Please enter 12 Sheed Phrase:");
            string sheed = Console.ReadLine();

            if (string.IsNullOrEmpty(sheed))
            {
                Console.WriteLine("\n\nError, Please input 12 words sheed phrase!\n");
                return;
            }

            try
            {
                Console.Clear();
                Console.WriteLine("\n\n\nYour Account");
                Console.WriteLine("======================");
                Account acc = Wallet.Restore(sheed);

                Console.WriteLine("\nADDRESS:\n{0}", acc.Address);
                Console.WriteLine("\nPUBLIC KEY:\n{0}", acc.PublicKey);
                Console.WriteLine("\n12 Words SHEED PHRASE:\n{0}", Wallet.Mnemonic);
                Console.WriteLine("\n - - - - - - - - - - - - - - - - - - - - - - ");
                Console.WriteLine("*** save sheed phrase in safe place!     ***");
                Console.WriteLine("*** use sheed phrase to restore account! ***");
                Console.WriteLine("*** don't tell any one!                  ***");

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

                Account acc =  Wallet.Create(0);

                Console.WriteLine("\nADDRESS:\n{0}", acc.Address);
                Console.WriteLine("\nPUBLIC KEY:\n{0}", acc.PublicKey);
                Console.WriteLine("\n12 Words SHEED PHRASE:\n{0}", Wallet.Mnemonic);
                Console.WriteLine("\n - - - - - - - - - - - - - - - - - - - - - - ");
                Console.WriteLine("*** save sheed phrase in safe place!     ***");
                Console.WriteLine("*** use sheed phrase to restore account! ***");
                Console.WriteLine("*** don't tell any one!                  ***");

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



                    if (block.Height == 1)
                    {
                        Console.WriteLine("Transactions : {0}", block.Transactions);
                    }
                    else
                    {
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
                    }


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
