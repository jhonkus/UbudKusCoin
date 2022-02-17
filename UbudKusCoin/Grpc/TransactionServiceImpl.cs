// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Threading.Tasks;

using Grpc.Core;

using NBitcoin;

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

        public override Task<TransactionStatus> Receive(TransactionPost req, ServerCallContext context)
        {

            var TxnHash = UbudKusCoin.Others.Utils.GetTransactionHash(req.Transaction);
            if (!TxnHash.Equals(req.Transaction.Hash))
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = "fail",
                    Message = "Transaction Hash is not valid!"
                });
            }


            var TxnValid = verifySignature(req.Transaction);
            if (!TxnValid)
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = "fail",
                    Message = "Signature  is not valid!"
                });
            }

            ServicePool.DbService.transactionsPooldb.Add(req.Transaction);

            return Task.FromResult(new TransactionStatus
            {
                Status = "success",
                Message = "Transaction done!"
            });
        }

        public override Task<TransactionStatus> Transfer(TransactionPost req, ServerCallContext context)
        {

            var TxnHash = UbudKusCoin.Others.Utils.GetTransactionHash(req.Transaction);
            if (!TxnHash.Equals(req.Transaction.Hash))
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = "fail",
                    Message = "Transaction Hash is not valid!"
                });
            }


            var TxnValid = verifySignature(req.Transaction);
            if (!TxnValid)
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = "fail",
                    Message = "Signature  is not valid!"
                });
            }

            //triger event block created
            ServicePool.EventService.OnEventTransactionCreated(req.Transaction);
            // ServicePool.DbService.transactionsPooldb.Add(req.Transaction);

            return Task.FromResult(new TransactionStatus
            {
                Status = "success",
                Message = "Transaction done!"
            });
        }


    }
}
