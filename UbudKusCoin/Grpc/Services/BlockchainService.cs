using System;
using System.Threading.Tasks;
using Grpc.Core;
using Main;
using Newtonsoft.Json;

namespace GrpcService.Services
{
    public class BlockchainService : BChainService.BChainServiceBase
    {

        public override Task<TrxResponse> GetGenesis(EmptyRequest request, ServerCallContext context)
        {
            var blockObj = Blockchain.GetGenesisBlock();
            var blockJson = JsonConvert.SerializeObject(blockObj, Formatting.Indented);

            return Task.FromResult(new TrxResponse
            {
                Result = blockJson
            });
        }


        public override Task<BalanceResponse> GetBalance(AccountRequest request, ServerCallContext context)
        {
            var balance = Transaction.GetBalance(request.Address);
            return Task.FromResult(new BalanceResponse
            {
                Balance = balance
            });
        }

        public override Task<TrxResponse> LastBlock(EmptyRequest request, ServerCallContext context)
        {
            var lastBlockObj = Blockchain.GetLastBlock();
            var blockJson = JsonConvert.SerializeObject(lastBlockObj, Formatting.Indented);

            return Task.FromResult(new TrxResponse
            {
                Result = blockJson
            });
        }

        public override Task<BlocksResponse> GetBlocks(BlockRequest request, ServerCallContext context)
        {
            var blocks = Blockchain.GetBlocks(request.PageNumber, request.ResultPerPage);

            BlocksResponse response = new BlocksResponse();
            foreach (Block block in blocks)
            {
                BlockModel mdl = new BlockModel
                {
                    Height = block.Height,
                    Hash = block.Hash,
                    PrevHash = block.PrevHash,
                    TimeStamp = block.TimeStamp,
                    Transactions = block.Transactions
                };
                response.Blocks.Add(mdl);
            }
            return Task.FromResult(response);
        }


        public override Task<TransactionsResponse> GetTransactions(AccountRequest request, ServerCallContext context)
        {
            TransactionsResponse response = new TransactionsResponse();
            var transactions = Transaction.GetTransactions(request.Address);
            foreach (Transaction trx in transactions)
            {
                TrxModel mdl = new TrxModel
                {
                    Recipient = trx.Recipient,
                    Sender = trx.Sender,
                    Fee = trx.Fee,
                    Amount = trx.Amount,
                    TimeStamp = trx.TimeStamp,
                };
                response.Transactions.Add(mdl);
            }
            return Task.FromResult(response);
        }

        public override Task<TrxResponse> SendCoin(SendRequest request, ServerCallContext context)
        {

            //Create transaction
            var newTrx = new Transaction()
            {
                ID = request.TrxId,
                TimeStamp = request.TrxInput.TimeStamp,
                Sender = request.TrxInput.SenderAddress,
                Recipient = request.TrxOutput.RecipientAddress,
                Amount = request.TrxOutput.Amount,
                Fee = request.TrxOutput.Fee
            };
            try
            {
                Transaction.AddToPool(newTrx);
                return Task.FromResult(new TrxResponse
                {
                    Result = "Success"
                });
            }
            catch (Exception e)
            {
                return Task.FromResult(new TrxResponse
                {
                    Result = "Error: " + e.Message
                });
            }
        }

        public override Task<TrxResponse> CreateBlock(EmptyRequest request, ServerCallContext context)
        {

            var trxPool = Transaction.GetPool();
            var transactions = trxPool.FindAll();
            var numOfTrxInPool = trxPool.Count();
            if (numOfTrxInPool <= 0)
            {
                return Task.FromResult(new TrxResponse
                {
                    Result = "Fail, No transaction in pool, please create transaction first!"
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


            return Task.FromResult(new TrxResponse
            {
                Result = "Success, a block created and added to Blockchain"
            });
        }

    }
}
