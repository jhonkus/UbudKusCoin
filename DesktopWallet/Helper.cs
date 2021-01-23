using System;

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


            Console.Clear();
            Console.WriteLine("\n\n\n\nHoree, transaction added to transaction pool!.");
            Console.WriteLine("Sender: {0}", sender);
            Console.WriteLine("Recipient {0}", recipient);
            Console.WriteLine("Amount: {0}", amount);
            Console.WriteLine("Fee: {0}", fee);
        }

  

        internal static void Create()
        {

            Console.Clear();
            Console.WriteLine("\n\n\n\nCreate Account");
            Console.WriteLine("======================");
          
            Wallet.Create();

        }


    }


}
