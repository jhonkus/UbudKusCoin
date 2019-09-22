using Xunit;
using Models;
using Utils;
using System.Collections.Generic;

namespace UnitTest
{
    public class BlockchainUnitTest
    {

        [Fact]
        public void CreateGenesisBlockTest() {
            var bc = new Blockchain();
            var genesisBlock = bc.Blocks[0];
            Assert.Equal(1, bc.Blocks.Count);
        }

        [Fact]
        public void GetLastBlockTest() {
            var bc = new Blockchain();
            //lastblock is genesis for first time
            var lastBlock = bc.GetLastBlock();
            Assert.Equal(1, lastBlock.Height);
            Assert.Equal(string.Empty.ConvertToBytes(), lastBlock.PrevHash);
            Assert.Equal(32, lastBlock.Hash.Length);
         

        }

        [Fact]
        public void AddBlockTest()
        {

            var bc = new Blockchain();

            var lsTrx = new List<Transaction>();

            //Create first transaction
            var trx1 = new Transaction
            {
                Sender = "Stevano",
                Recipient = "Valentino",
                Amount = 3.0,
                Fee = 0.3
            };

            lsTrx.Add(trx1);
            bc.AddBlock(lsTrx.ToArray());
            Assert.Equal(2, bc.Blocks.Count);

            //Create 2nd transaction
            trx1 = new Transaction
            {
                Sender = "Budiawan",
                Recipient = "Norita",
                Amount = 2.5,
                Fee = 0.2
            };

            lsTrx = new List<Transaction>();
            lsTrx.Add(trx1);
            bc.AddBlock(lsTrx.ToArray());
            Assert.Equal(3, bc.Blocks.Count);


        }

    
    }
}
