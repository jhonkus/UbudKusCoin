
using Client;
using DB;

namespace Main
{
    class Program
    {
        static void Main()
        {

            // Initilize db
            DbAccess.Initialize();

            // Make blockchain
            _ = new Blockchain();

            // show menu
            Menu.DisplayMenu();
        }
    }



}