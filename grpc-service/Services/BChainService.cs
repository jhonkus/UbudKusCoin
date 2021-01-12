using System;
using System.Threading.Tasks;
using Grpc.Core;
using grpcservice.Protos;
using Main;
using Microsoft.Extensions.Logging;
using Models;
using Newtonsoft.Json;

namespace grpcservice.Services
{
    public class BlockchainService: BchainService.BchainServiceBase
    {

        public override Task<StringReply> GetGenesis(EmptyRequest request, ServerCallContext context)
        {
            var blockObj = Blockchain.GetGenesisBlock();
            var blockJson = JsonConvert.SerializeObject(blockObj, Formatting.Indented);

            return Task.FromResult(new StringReply
            {
                Message = blockJson
            });
        }


        public override Task<StringReply> LastBlock(EmptyRequest request, ServerCallContext context)
        {
            var lastBlockObj = Blockchain.GetLastBlock();
            var blockJson = JsonConvert.SerializeObject(lastBlockObj, Formatting.Indented);

            return Task.FromResult(new StringReply
            {
                Message = blockJson
            });
        }


        public override Task<StringReply> SendCoin(SendRequest request, ServerCallContext context)
        {

            //Create transaction
            var newTrx = new Transaction()
            {
                TimeStamp = DateTime.Now.Ticks,
                Sender = request.Sender,
                Recipient = request.Recipient,
                Amount = request.Amount,
                Fee = request.Fee
            };
            try
            {
                Transaction.AddToPool(newTrx);
                return Task.FromResult(new StringReply
                {
                    Message = "Success"
                });
            }
            catch
            {
                return Task.FromResult(new StringReply
                {
                    Message = "Error"
                });
            }
        }

        public override Task<StringReply> Minting(EmptyRequest request, ServerCallContext context)
        {

            var trxPool = Transaction.GetPool();
            var transactions = trxPool.FindAll();
            var numOfTrxInPool = trxPool.Count();
            if (numOfTrxInPool <= 0)
            {
                return Task.FromResult(new StringReply
                {
                    Message = "Fail, No transaction in pool, please create transaction first!"
                });
            }

            
            var lastBlock = Blockchain.GetLastBlock();

            // create block from transaction pool
            string tempTransactions = JsonConvert.SerializeObject(transactions);

            var block = new Block(lastBlock, tempTransactions);
            Blockchain.AddBlock(block);

            // move all record in trx pool to transactions table
            foreach (Transaction trx in transactions)
            {
                Transaction.Add(trx);
            }

            // clear mempool
            trxPool.DeleteAll();
            

            return Task.FromResult(new StringReply
            {
                Message = "Success, a block created and added to Blockchain"
            });
        }

    }
}
