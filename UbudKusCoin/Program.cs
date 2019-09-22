using Models;
using System;
using System.Collections.Generic;

namespace Main
{
    class Program
    {
        static void Main()
        {

            // Make blockchain
            var bc = new Blockchain();
           
            //Create new transaction
            var trx1 = new Transaction
            {
                Sender = "Genesis Account",
                Recipient = "Ricardo",
                Amount = 300,
                Fee = 0.001
            };

            //Create new transaction
            var trx2 = new Transaction
            {
                Sender = "Genesis Account",
                Recipient = "Frodo",
                Amount = 250,
                Fee = 0.001
            };

            //Create new transaction
            var trx3 = new Transaction
            {
                Sender = "Ricardo",
                Recipient = "Madona",
                Amount = 20,
                Fee = 0.0001
            };

            //create list of transactions
            var lsTrx = new List<Transaction>
            {
                trx1,
                trx2
            };

            var transactions = lsTrx.ToArray();
            bc.AddBlock(transactions);

            lsTrx = new List<Transaction>
            {
                trx3         
            };

            transactions = lsTrx.ToArray();
            bc.AddBlock(transactions);

            //Print all blocks
            bc.PrintBlocks();

            //check balance for each account account
            var balance = bc.GetBalance("Genesis Account");
            Console.WriteLine("Genesis Account balance: {0}", balance);

            balance = bc.GetBalance("Ricardo");
            Console.WriteLine("Ricardo balance: {0}", balance);

            balance = bc.GetBalance("Frodo");
            Console.WriteLine("Frodo  balance: {0}", balance);


            balance = bc.GetBalance("Madona");
            Console.WriteLine("Madona balance: {0}", balance);

            Console.ReadKey();
        }



    }



}