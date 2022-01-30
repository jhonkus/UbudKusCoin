using Grpc.Core;
using System.Threading.Tasks;
using UbudKusCoin.Services;

namespace UbudKusCoin.Grpc
{

    public class TransactionServiceImpl : TransactionService.TransactionServiceBase
    {


        public override Task<Transaction> GetByHash(Transaction req, ServerCallContext context)
        {
            var transaction = ServicePool.DbService.transactionDb.GetByHash(req.Hash);
            return Task.FromResult(transaction);

        }

        public override Task<TransactionList> GetRangeByAddress(TransactionParams req, ServerCallContext context)
        {
            var transactions = ServicePool.DbService.transactionDb.GetRangeByAddress(req.Address, req.PageNumber, req.ResultPerPage);
            var response = new TransactionList();
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetRangeByHeight(TransactionParams req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.transactionDb.GetRangeByHeight(req.Height, req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetRange(TransactionParams req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.transactionDb.GetRange(req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetPoolRange(TransactionParams req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.transactionDb.GetRange(req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }



        public Task<TransactionStatus> Add2(Transaction req, ServerCallContext context)
        {
            var status = ServicePool.DbService.transactionDb.Add(req);
            var trxStatus = new TransactionStatus();
            trxStatus.Status = status;
            return Task.FromResult(trxStatus);

        }

        // public override Task<TransactionStatus> Add(Transaction req, ServerCallContext context)
        // {

        //     var status = 
        //     // verify transaction ID
        //     var TxnHash = newTxn.GetHash();

        //     if (!TxnHash.Equals(req.Hash))
        //     {
        //         return Task.FromResult(new TransactionStatus
        //         {
        //             Status = "Transaction Hash is not valid"
        //         });
        //     }

        //     // Verify signature
        //     var TxnValid = Transaction.VerifySignature(request.PublicKey, request.TxnId, request.TxnInput.Signature);
        //     if (!TxnValid)
        //     {
        //         return Task.FromResult(new SendResponse
        //         {
        //             Result = "Signature not valid"
        //         });
        //     }


        //     Transaction.AddToPool(newTxn);


        //     return Task.FromResult(new SendResponse
        //     {
        //         Result = "Success"
        //     });
        // }


    }
}
