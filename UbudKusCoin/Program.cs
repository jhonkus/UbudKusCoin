
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

            /**
             * remove all record in all table
             * uncomment this if you want
            **/
            // DbAccess.ClearDB();

            // Make blockchain
            _ = new Blockchain();

            // show menu
            Menu.DisplayMenu();
        }
    }



}