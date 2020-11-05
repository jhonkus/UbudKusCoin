using Models;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Main
{
    class Program
    {
        static void Main()
        {

            // Make blockchain
            var bc = new Blockchain();


            int selection = 0;
            while (selection != 100)
            {
                switch (selection)
                {
                    case 1:
                        Console.WriteLine("Geting Genesis Block");
                        var genesisBlock = bc.GetGenesisBlock();
                        Console.WriteLine(JsonConvert.SerializeObject(genesisBlock, Formatting.Indented));

                        break;
                    case 2:
                        var lastBlock = bc.GetLastBlock();
                        Console.WriteLine("Geting Last Block");
                        Console.WriteLine(JsonConvert.SerializeObject(lastBlock, Formatting.Indented));
                        break;

                    case 3:
                        Console.WriteLine("Sender name (type any name): ");
                        string from = Console.ReadLine();

                        Console.WriteLine("Please enter the Recipient name (type any name):");
                        string to = Console.ReadLine();

                        Console.WriteLine("Please enter the amount");
                        string strAmount = Console.ReadLine();
                        int amount = 0;
                        try
                        {
                            amount = int.Parse(strAmount);
                            var trx = new Transaction
                            {
                                Sender = from,
                                Recipient = to,
                                Amount = 20,
                                Fee = 0.1
                            };

                            bc.TrxPool.Add(trx);
                            Console.WriteLine(" Transaction added to transaction pool.");

                        }
                        catch
                        {
                            Console.WriteLine(" please check amount.");
                        }


                        break;

                    case 4:
                        Console.WriteLine("Crate Block");
                        if (bc.TrxPool.Count <= 0)
                        {
                            Console.WriteLine("No transaction in pool, please create transaction first!");
                        }
                        else
                        {
                            var transactions = bc.TrxPool.ToArray();
                            bc.AddBlock(transactions);
                            bc.TrxPool = new List<Transaction>();
                        }
                        break;

                    case 5:
                        Console.WriteLine("\n\nBlock List\n- - - - - - -");
                        bc.PrintBlocks();
                        break;

                    case 9:
                        Console.WriteLine("Exit");
                        Environment.Exit(-1);
                        break;
                }

                Console.WriteLine("\n\nUBUD PKUS Crypto v0.0.1\n=========================");
                Console.WriteLine("1. Get Genesis Block");
                Console.WriteLine("2. Get Last Block");
                Console.WriteLine("3. Add a transaction");
                Console.WriteLine("4. Create Block");
                Console.WriteLine("5. Print Blocks");
                Console.WriteLine("9. Exit");
                Console.WriteLine("=========================");

                Console.WriteLine("Please selectd menu by input the number!");
                string action = Console.ReadLine();
                try{
                    selection = int.Parse(action);
                }catch{
                    Console.WriteLine("--- Please input correct number! ---");
                }
            }

            Console.ReadKey();
        }
    }
}