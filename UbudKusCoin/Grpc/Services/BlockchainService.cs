using System;
using System.Threading.Tasks;
using Grpc.Core;
using Main;
using Newtonsoft.Json;

namespace GrpcService.Services
{
    public class BlockchainService : BChainService.BChainServiceBase
    {

        public override Task<BlockResponse> GenesisBlock(EmptyRequest request, ServerCallContext context)
        {
            var block = Blockchain.GetGenesisBlock();
            BlockModel mdl = ConvertBlock(block);

            return Task.FromResult(new BlockResponse
            {
                Block = mdl
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

        public override Task<BlockResponse> LastBlock(EmptyRequest request, ServerCallContext context)
        {
            var block = Blockchain.GetLastBlock();
            BlockModel mdl = ConvertBlock(block);
            return Task.FromResult(new BlockResponse
            {
                Block = mdl
            });
        }


        public override Task<BlocksResponse> GetBlocks(PagingRequest request, ServerCallContext context)
        {
            var blocks = Blockchain.GetBlocks(request.PageNumber, request.ResultPerPage);

            BlocksResponse response = new BlocksResponse();
            foreach (Block block in blocks)
            {
                BlockModel mdl = ConvertBlock(block);
                response.Blocks.Add(mdl);
            }
            return Task.FromResult(response);
        }


        public override Task<TransactionsResponse> GetAccountTransactions(AccountRequest request, ServerCallContext context)
        {
            TransactionsResponse response = new TransactionsResponse();
            var transactions = Transaction.GetAccountTransactions(request.Address);
            foreach (Transaction trx in transactions)
            {
                TrxModel mdl = ConvertTrxModel(trx);
                response.Transactions.Add(mdl);
            }
            return Task.FromResult(response);
        }

        public override Task<TransactionsResponse> GetTransactions(PagingRequest request, ServerCallContext context)
        {

             TransactionsResponse response = new TransactionsResponse();
            var transactions = Transaction.GetTransactions(request.PageNumber, request.ResultPerPage);
            foreach (Transaction trx in transactions)
            {
                TrxModel mdl = ConvertTrxModel(trx);
                response.Transactions.Add(mdl);
            }
            return Task.FromResult(response);
        }


        public override Task<TrxResponse> SendCoin(SendRequest request, ServerCallContext context)
        {
            //Console.WriteLine("request.TrxId: {0}", request.TrxId);
            //Create new transaction
            var newTrx = new Transaction()
            {
                Hash = request.TrxId,
                TimeStamp = request.TrxInput.TimeStamp,
                Sender = request.TrxInput.SenderAddress,
                Recipient = request.TrxOutput.RecipientAddress,
                Amount = request.TrxOutput.Amount,
                Fee = request.TrxOutput.Fee
            };


            // verify transaction ID
            var trxHash = newTrx.GetHash();
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

        private static BlockModel ConvertBlock(Block block)
        {

            try
            {
                BlockModel mdl = new BlockModel
                {
                    Version = block.Version,
                    Height = block.Height,
                    Hash = block.Hash,
                    PrevHash = block.PrevHash,
                    TimeStamp = block.TimeStamp,
                    Transactions = JsonConvert.SerializeObject(block.Transactions),
                    MerkleRoot = block.MerkleRoot,
                    NumOfTx = block.NumOfTx,
                    TotalAmount = block.TotalAmount,
                    TotalReward = block.TotalReward,
                    Difficulty = block.Difficulty,
                    Validator = block.Validator

                };
                return mdl;
            }
            catch
            {
                return null;
            }

          
        }

        private static TrxModel ConvertTrxModel(Transaction trx)
        {
            TrxModel mdl = new TrxModel
            {
                Hash = trx.Hash,
                Recipient = trx.Recipient,
                Sender = trx.Sender,
                Fee = trx.Fee,
                Amount = trx.Amount,
                TimeStamp = trx.TimeStamp,
            };

            return mdl;
        }
    }
}
