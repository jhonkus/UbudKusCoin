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
    public class BlockService: BlockSrv.BlockSrvBase
    {
        private readonly ILogger<GreeterService> _logger;
        public BlockService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<StringReply> GetGenesis(EmptyRequest request, ServerCallContext context)
        {
            var genesisBlock = Blockchain.GetGenesisBlock();
            var genesis = JsonConvert.SerializeObject(genesisBlock, Formatting.Indented);

            return Task.FromResult(new StringReply
            {
                Message = genesis
            });
        }

        public override Task<StringReply> LastBlock(EmptyRequest request, ServerCallContext context)
        {
            var lastBlock = Blockchain.GetLastBlock();
            var block = JsonConvert.SerializeObject(lastBlock, Formatting.Indented);

            return Task.FromResult(new StringReply
            {
                Message = block
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
