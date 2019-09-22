using System;
using Models;
using Utils;
using System.Collections.Generic;
using Xunit;

namespace UnitTest
{
    public class BlockUnitTest
    {
        [Fact]
        public void NewBlockTest()
        {



            var lsTrx = new List<Models.Transaction>();

            //Create first transaction
            var trx1 = new Transaction
            {
                Sender = "Johana",
                Recipient = "Merlin",
                Amount = 3.0,
                Fee = 0.3
            };

            lsTrx.Add(trx1);

            var block = new Block(0, String.Empty.ConvertToBytes(), lsTrx.ToArray(), "Admin");
            Assert.Equal(0, block.Height);
            Assert.Equal(String.Empty.ConvertToBytes(), block.PrevHash);
            Assert.Equal("Admin", block.Creator);
            Assert.Equal(lsTrx, block.Transactions);
            Assert.Single(block.Transactions);
            Assert.Equal(32, block.Hash.Length);

        }
    }
}
