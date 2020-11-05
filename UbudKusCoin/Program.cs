using Models;

using Client;
using System;
using DB;
using Newtonsoft.Json;
using System.Text;

namespace Main
{
    class Program
    {
        static void Main()
        {

            // Initilize db
            DbAccess.Initialize();

            // Make blockchain
            var bc = new Blockchain();

            // show menu
            var menu = new Menu(bc);
            menu.DisplayMenu(bc);
        }
    }



}