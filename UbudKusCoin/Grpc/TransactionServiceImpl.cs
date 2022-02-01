using Grpc.Core;
using System.Threading.Tasks;
using UbudKusCoin.Services;
using NBitcoin;
using System;

namespace UbudKusCoin.Grpc
{

    public class TransactionServiceImpl : TransactionService.TransactionServiceBase
    {


        public override Task<Transaction> GetByHash(Transaction req, ServerCallContext context)
        {
            var transaction = ServicePool.DbService.transactionDb.GetByHash(req.Hash);
            return Task.FromResult(transaction);

        }

        public override Task<TransactionList> GetRangeByAddress(TransactionPaging req, ServerCallContext context)
        {
            var transactions = ServicePool.DbService.transactionDb.GetRangeByAddress(req.Address, req.PageNumber, req.ResultPerPage);
            var response = new TransactionList();
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetRangeByHeight(TransactionPaging req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.transactionDb.GetRangeByHeight(req.Height, req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetRange(TransactionPaging req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.transactionDb.GetRange(req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetPoolRange(TransactionPaging req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.transactionDb.GetRange(req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }



        public static bool verifySignature(Transaction txn)
        {
            var pubKey = new PubKey(txn.PubKey);
            return pubKey.VerifyMessage(txn.Hash, txn.Signature);
        }

        public override Task<TransactionStatus> SendCoin(TransactionPost req, ServerCallContext context)
        {
            Console.WriteLine("== Receive txn {0}", req);
            // verify transaction Hash
            var TxnHash = UbudKusCoin.Others.Utils.GetTransactionHash(req.Transaction);
            Console.WriteLine("== Receive TxnHash2 {0}", TxnHash);
            if (!TxnHash.Equals(req.Transaction.Hash))
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = "fail",
                    Message = "Transaction Hash is not valid!"
                });
            }

            Console.WriteLine("== Receive txn 1 {0}", req);
            // Verify signature
            var TxnValid = verifySignature(req.Transaction);
            if (!TxnValid)
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = "fail",
                    Message = "Signature  is not valid!"
                });
            }

            Console.WriteLine("== Receive txn 2 {0}", req);
            ServicePool.DbService.transactionsPooldb.Add(req.Transaction);
            Console.WriteLine("added to pool ");



            return Task.FromResult(new TransactionStatus
            {
                Status = "success",
                Message = "Transaction done!"
            });
        }


    }
}
