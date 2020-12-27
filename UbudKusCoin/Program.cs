using Models;
using System;

namespace Main
{
    class Program
    {
        static void Main()
        {

            // Initialize blockchain
            var bc = new Blockchain();


            // Create new transaction
            var trx1 = new Transaction
            {
                TimeStamp = DateTime.Now.Ticks,
                Sender = "Genesis Account",
                Recipient = "Ricardo",
                Amount = 300,
                Fee = 0.001
            };

            // add transaction to pool
            bc.AddTransactionToPool(trx1);


            //Create new transaction
            var trx2 = new Transaction
            {
                TimeStamp = DateTime.Now.Ticks,
                Sender = "Genesis Account",
                Recipient = "Frodo",
                Amount = 250,
                Fee = 0.001
            };

            // add transaction to pool
            bc.AddTransactionToPool(trx2);

            // add a block to blockchain
            bc.CreateBlock();

            // clear transaction pool after block created
            bc.ClearPool();


            // create new transaction again
            var trx3 = new Transaction
            {
                TimeStamp = DateTime.Now.Ticks,
                Sender = "Ricardo",
                Recipient = "Madona",
                Amount = 20,
                Fee = 0.0001
            };

            // add transaction to pool
            bc.AddTransactionToPool(trx3);


            // forging a block
            bc.CreateBlock();

            // clear transaction pool again
            bc.ClearPool();

            // -----------------------------------------
            // till here we create 3 transactions and 2 blocks.
            // all bloks already added to blockchain
            // ----------------------------------------


            // Now lets get some data from blockchain

            // print genesis block
            bc.PrintGenesisBlock();

            // print last block
            bc.PrintLastBlock();

            //check balance for Ricardo
            bc.PrintBalance("Ricardo");


            //check balance for Frodo
            bc.PrintBalance("Frodo");


            //check balance for Madona
            bc.PrintBalance("Madona");



            bc.PrintTransactionHistory("Ricardo");

            // print all block in Blockchain
            bc.PrintBlocks();

            // press enter to close    
            Console.ReadKey();
        }
    }
}