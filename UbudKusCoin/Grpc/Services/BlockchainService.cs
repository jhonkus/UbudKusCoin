using System.Threading.Tasks;
using Grpc.Core;
using Main;
using Newtonsoft.Json;

namespace GrpcService.Services
{
    public class BlockchainService : BChainService.BChainServiceBase
    {

        public override Task<TrxResponse> GenesisBlock(EmptyRequest request, ServerCallContext context)
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

            //Create new transaction
            var newTrx = new Transaction()
            {
                ID = request.TrxId,
                TimeStamp = request.TrxInput.TimeStamp,
                Sender = request.TrxInput.SenderAddress,
                Recipient = request.TrxOutput.RecipientAddress,
                Amount = request.TrxOutput.Amount,
                Fee = request.TrxOutput.Fee
            };


            // verify transaction ID
            var trxHash = Utils.GetTrxHash(newTrx);
            if (!trxHash.Equals(request.TrxId))
            {
                return Task.FromResult(new TrxResponse
                {
                    Result = "Transaction ID not valid"
                });               
            }
           

            // Verify signature   
            var trxValid = Transaction.VerifySignature(request.PublicKey, request.TrxId, request.TrxInput.Signature);
            if (!trxValid)
            {
                return Task.FromResult(new TrxResponse
                {
                    Result = "Signature not valid"
                });
            }


            Transaction.AddToPool(newTrx);
            return Task.FromResult(new TrxResponse
            {
                Result = "Success"
            });
       
        }

        
    }
}
