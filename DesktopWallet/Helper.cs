using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using GrpcService;

namespace DesktopWallet
{
    public static class Helper
    {



        internal static void DoSendCoin()
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
            var senderBalance = 0; // Transaction.GetBalance(sender);

            // validate amount and fee
            if ((amount + fee) > senderBalance)
            {
                Console.WriteLine("\nError! Sender ({0}) don't have enough balance!", sender);
                Console.WriteLine("Sender ({0}) balance is {1}", sender, senderBalance);
                return;
            }


            //Create transaction
            //var newTrx = new Transaction()
            //{
            //    TimeStamp = DateTime.Now.Ticks,
            //    Sender = sender,
            //    Recipient = recipient,
            //    Amount = amount,
            //    Fee = fee
            //};

            //Transaction.AddToPool(newTrx);
            Console.Clear();
            Console.WriteLine("\n\n\n\nHoree, transaction added to transaction pool!.");
            Console.WriteLine("Sender: {0}", sender);
            Console.WriteLine("Recipient {0}", recipient);
            Console.WriteLine("Amount: {0}", amount);
            Console.WriteLine("Fee: {0}", fee);
        }

        internal static void ShowHistory()
        {
            Console.WriteLine("ShowHistory");
        }

        internal static void ShowBalance()
        {
            Console.WriteLine("ShowBalance");
        }

        internal static void SendCoin()
        {
            Console.WriteLine("Send Coin");
        }

        internal static void Restore()
        {
            Console.WriteLine("Restre Wallet");
        }

        internal static void Create()
        {

            Console.Clear();
            Console.WriteLine("\n\n\n\nCreate Account");
            Console.WriteLine("======================");


            Console.WriteLine("Please enter  3 digit number!:");

            string strPin = Console.ReadLine();
            try
            {
                var temp = float.Parse(strPin);
                var id = Utils.CreateID();
                string secreet = strPin + '-' + id;
                Wallet.Create(0);

            }
            catch (Exception e)
            {
                Console.WriteLine("\nError! You have inputted the wrong value for the PIN! {0}", e.Message);
                return;
            }


          
        }


        //'Google.Protobuf.Collections.RepeatedField<grpcservice.Protos.BlockModel>' to 'System.Collections.Generic.List<grpcservice.Protos.BlockModel>'
        public static void DoShowBlockchainXXX(Google.Protobuf.Collections.RepeatedField<BlockModel> blocks)
        {
            Console.Clear();
            Console.WriteLine("\n\n\nBlockchain Explorer");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");


            foreach (var block in blocks)
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
                    foreach (TrxModel trx in transactions)
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

 
    }


}
