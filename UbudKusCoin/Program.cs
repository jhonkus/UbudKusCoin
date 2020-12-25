using Models;

using Client;

namespace Main
{
    class Program
    {
        static void Main()
        {

            // Make blockchain
            var bc = new Blockchain();

            // show menu
            var menu = new Menu(bc);
            menu.DisplayMenu(bc);


        }

        

    }



}