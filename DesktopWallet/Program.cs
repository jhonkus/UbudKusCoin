using System.Threading.Tasks;


namespace DesktopWallet
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // show wallet
            await ConsoleWallet.Show();
        }

    }


}
