using System;
using DB;
using Models;
using LiteDB;
using Newtonsoft.Json;

namespace Main
{
    public class Blockchain
    {

        public Blockchain()
        {
            Initialize();
        }


        private void Initialize()
        {

            var blocks = this.GetBlocks();
            // blocks.EnsureIndex(x => x.Height);

            if (blocks.Count() < 1)
            {
                // Create genesis block
                var gnsBlock = Block.Genesis();
                blocks.Insert(gnsBlock);
            }

        }

        public ILiteCollection<Models.Block> GetBlocks()
        {
            var coll = DbAccess.DB.GetCollection<Block>(DbAccess.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height);
            return coll;
        }



        public Block GetGenesisBlock()
        {
            var blockchain = this.GetBlocks();
            var block = blockchain.FindOne(Query.All(Query.Ascending));
            return block;
        }

        public Block GetLastBlock()
        {
            var blockchain = this.GetBlocks();
            var block = blockchain.FindOne(Query.All(Query.Descending));
            return block;
        }

        public int GetHeight()
        {
            var lastBlock = this.GetLastBlock();
            return lastBlock.Height;
        }

        public void AddBlock(Block block)
        {
            var blocks = GetBlocks();
            blocks.Insert(block);
        }

        public double GetBalance(string name)
        {
            double balance = 0;
            //TODO
            return balance;
        }

        public void PrintBlocks()
        {
            var blockchain = this.GetBlocks();
            var results = blockchain.FindAll();
            Console.WriteLine(JsonConvert.SerializeObject(results, Formatting.Indented));
        }
    }
}