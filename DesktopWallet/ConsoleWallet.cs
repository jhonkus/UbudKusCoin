using System;
using System.Threading.Tasks;

namespace DesktopWallet
{
    public class ConsoleWallet
    {

        private static BlockchainClient client;
        public static async Task Show()
        {
            client = new BlockchainClient();
            MenuItem();
            await GetInput();
        }

        private static void MenuItem()
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

        private static async Task GetInput()
        {
            int selection = 0;
            while (selection != 20)
            {
                switch (selection)
                {
                    case 1:
                        await client.ShowGenesisBlock();

                        break;
                    case 2:
                        await client.ShowLastBlock();

                        break;

                    case 3:
                        await client.SendCoin();

                        break;

                    case 4:

                        await client.CreateBlock();

                        break;

                    case 5:
                        await client.ShowBalance();

                        break;
                    case 6:
                        await client.ShowHistory();

                        break;
                    case 7:
                        await client.ShowAllBlocks();
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

        private async static void DoExit()
        {
            Console.Clear();
            Console.WriteLine("\n\nApplication closed!\n");
            await Task.Delay(2000);
            Environment.Exit(0);
        }

    }
}
