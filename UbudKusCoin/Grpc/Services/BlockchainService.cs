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

            if (block is null)
            {
                return Task.FromResult(new BlockResponse());
            }

            BlockModel mdl = ConvertBlock(block);

            return Task.FromResult(new BlockResponse
            {
                Block = mdl
            });
        }


        public override Task<BalanceResponse> GetBalance(CommonRequest request, ServerCallContext context)
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

            if (block is null)
            {
                return Task.FromResult(new BlockResponse());
            }

            BlockModel mdl = ConvertBlock(block);
            return Task.FromResult(new BlockResponse
            {
                Block = mdl
            });
        }


        public override Task<TxnResponse> GetTxnByHash(CommonRequest request, ServerCallContext context)
        {
            var txn = Transaction.GetTxnByHash(request.TxnHash);

            if (txn is null)
            {
                return Task.FromResult(new TxnResponse());
            }

            TxnModel mdl = ConvertTxnModel(txn);
            return Task.FromResult(new TxnResponse
            {
                Txn = mdl
            });

        }

        public override Task<BlockResponse> GetBlockByHeight(CommonRequest request, ServerCallContext context)
        {
            var block = Blockchain.GetBlockByHeight(request.BlockHeight);
            if (block is null)
            {
                return Task.FromResult(new BlockResponse());
            }
            BlockModel mdl = ConvertBlock(block);
            return Task.FromResult(new BlockResponse
            {
                Block = mdl
            });
        }

        public override Task<TxnsResponse> GetTxnsByAccount(CommonRequest request, ServerCallContext context)
        {
            var transactions = Transaction.GetAccountTransactions(request.Address);
            if (transactions is null)
            {
                return Task.FromResult(new TxnsResponse());
            }

            TxnsResponse response = new TxnsResponse();
            foreach (Transaction Txn in transactions)
            {
                TxnModel mdl = ConvertTxnModel(Txn);
                response.Transactions.Add(mdl);
            }
            return Task.FromResult(response);
        }

        public override Task<BlockResponse> GetBlockByHash(CommonRequest request, ServerCallContext context)
        {
            var block = Blockchain.GetBlockByHash(request.BlockHash);
            if (block is null)
            {
                return Task.FromResult(new BlockResponse());
            }
            BlockModel mdl = ConvertBlock(block);
            return Task.FromResult(new BlockResponse
            {
                Block = mdl
            });
        }
        public override Task<BlocksResponse> GetBlocks(PagingRequest request, ServerCallContext context)
        {
            var blocks = Blockchain.GetBlocks(request.PageNumber, request.ResultPerPage);

            if (blocks is null)
            {
                return Task.FromResult(new BlocksResponse());
            }

            BlocksResponse response = new BlocksResponse();
            foreach (Block block in blocks)
            {
                BlockModel mdl = ConvertBlockForList(block);
                response.Blocks.Add(mdl);
            }
            return Task.FromResult(response);
        }



        public override Task<SearchResponse> Search(CommonRequest request, ServerCallContext context)
        {
            var searchText = request.SearchText.Trim();

            var response = new SearchResponse
            {
                Id = 0,
                Title = "no data found",
                SarchText = searchText,
                Url = "/notfound",
                Status = "no"
            };
            //search block by hash
            try
            {
                var block = Blockchain.GetBlockByHash(searchText);
                if (block is not null && block.Hash is not null)
                {
                    response = new SearchResponse
                    {
                        Id = 1,
                        Title = "search found block by hash",
                        SarchText = searchText,
                        Url = "/block/hash/" + searchText,
                        Status = "ok"
                    };
                    return Task.FromResult(response);
                }
            }
            catch
            {
                Console.WriteLine("no block by hash");
            }

            // search block by height
            try
            {
                var height = int.Parse(searchText);
                var block = Blockchain.GetBlockByHeight(height);
                if (block is not null && block.Hash is not null)
                {
                    response = new SearchResponse
                    {
                        Id = 2,
                        Title = "search found block by height",
                        SarchText = searchText,
                        Url = "/block/height/" + searchText,
                        Status = "ok"
                    };
                    return Task.FromResult(response);
                }
            }
            catch
            {
                Console.WriteLine("no block by height");
            }


            //search tnxs by hash
            try
            {
                var txn = Transaction.GetTxnByHash(searchText);
                if (txn is not null && txn.Hash is not null)
                {
                    response = new SearchResponse
                    {
                        Id = 3,
                        Title = "Found transaction",
                        SarchText = searchText,
                        Url = "/txn/" + searchText,
                        Status = "ok"
                    };
                    return Task.FromResult(response);
                }

            }
            catch
            {
                Console.WriteLine("no txn by hash");
            }


            // get Account 
            try
            {
                // 1. get one transaction by address
                var txn = Transaction.GetOneTxnByAddress(searchText);
                if (txn is not null && txn.Hash is not null)
                {
                    response = new SearchResponse
                    {
                        Id = 4,
                        Title = "search found one address ",
                        SarchText = searchText,
                        Url = "/address/" + searchText,
                        Status = "ok"
                    };
                    return Task.FromResult(response);
                }

            }
            catch
            {
                Console.WriteLine("Error when search by account");
            }


            return Task.FromResult(response);
        }

        public override Task<AccountResponse> GetAccount(CommonRequest request, ServerCallContext context)
        {
            AccountResponse response = new AccountResponse();


            // 1. get all transaction bellong this account
            var transactions = Transaction.GetAccountTransactions(request.Address);

            if (transactions is null)
            {
                response.Transactions.Add(new TxnModel()); //no txn
            }
            else
            {

                foreach (Transaction Txn in transactions)
                {
                    TxnModel mdl = ConvertTxnModel(Txn);
                    response.Transactions.Add(mdl);
                }
            }

            // get Blocks by validator
            var blocks = Blockchain.GetBlocksByValidator(request.Address);
            if (blocks is null)
            {
                response.Blocks.Add(new BlockModel()); //no txn
            }
            else
            {
                response.NumBlockValidate = 0;
                foreach (Block block in blocks)
                {
                    BlockModel mdl = ConvertBlockForList(block);
                    response.Blocks.Add(mdl);
                    // count number of block validated by this account
                    response.NumBlockValidate += 1;
                }


            }

            // Get Account Balance
            var balance = Transaction.GetBalance(request.Address);
            response.Balance = balance;

            return Task.FromResult(response);
        }

        public override Task<TxnsResponse> GetTxnsByHeight(CommonRequest request, ServerCallContext context)
        {
            TxnsResponse response = new TxnsResponse();
            var transactions = Transaction.GetTxnsByHeight(request.BlockHeight);
            if (transactions is null)
            {
                return Task.FromResult(response);
            }

            foreach (Transaction txn in transactions)
            {
                TxnModel mdl = ConvertTxnModel(txn);
                response.Transactions.Add(mdl);
            }
            return Task.FromResult(response);

        }

        public override Task<TxnsResponse> GetTxns(PagingRequest request, ServerCallContext context)
        {

            TxnsResponse response = new TxnsResponse();
            var transactions = Transaction.GetTransactions(request.PageNumber, request.ResultPerPage);
            if (transactions is null)
            {
                return Task.FromResult(response);
            }

            foreach (Transaction txn in transactions)
            {
                TxnModel mdl = ConvertTxnModel(txn);
                response.Transactions.Add(mdl);
            }
            return Task.FromResult(response);
        }


        public override Task<SendResponse> SendCoin(SendRequest request, ServerCallContext context)
        {
            var newTxn = new Transaction()
            {
                Hash = request.TxnId,
                TimeStamp = request.TxnInput.TimeStamp,
                // TxnType = request.TxnInput.TxnType,
                Sender = request.TxnInput.SenderAddress,
                Recipient = request.TxnOutput.RecipientAddress,
                Amount = request.TxnOutput.Amount,
                Fee = request.TxnOutput.Fee,
                // Status = "-",
            };


            // verify transaction ID
            var TxnHash = newTxn.GetHash();
            if (!TxnHash.Equals(request.TxnId))
            {
                return Task.FromResult(new SendResponse
                {
                    Result = "Transaction ID not valid"
                });
            }

            // Verify signature
            var TxnValid = Transaction.VerifySignature(request.PublicKey, request.TxnId, request.TxnInput.Signature);
            if (!TxnValid)
            {
                return Task.FromResult(new SendResponse
                {
                    Result = "Signature not valid"
                });
            }


            Transaction.AddToPool(newTxn);
            return Task.FromResult(new SendResponse
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
                    Validator = block.Validator,
                    BuildTime = block.BuildTime
                };
                return mdl;
            }
            catch
            {
                return null;
            }
        }

        /**
Use this to comvert block for list, to reduce data size when transfered over internet 
*/
        private static BlockModel ConvertBlockForList(Block block)
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


        private static TxnModel ConvertTxnModel(Transaction txn)
        {
            TxnModel mdl = new TxnModel
            {
                Hash = txn.Hash,
                Recipient = txn.Recipient,
                Sender = txn.Sender,
                Fee = txn.Fee,
                Amount = txn.Amount,
                Height = txn.Height,
                TimeStamp = txn.TimeStamp,
            };

            return mdl;
        }
    }
}
