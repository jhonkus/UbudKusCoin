// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Threading.Tasks;

using System;

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
            Console.WriteLine("-- Receive Txn with Hash: {0}, amount {1}", req.Transaction.Hash, req.Transaction.Amount);

            var TxnHash = UbudKusCoin.Others.UkcUtils.GetTransactionHash(req.Transaction);
            if (!TxnHash.Equals(req.Transaction.Hash))
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Transaction Hash is not valid!"
                });
            }


            var isSignatureValid = verifySignature(req.Transaction);
            if (!isSignatureValid)
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Signature  is not valid!"
                });
            }

            //TODO add more validation here


            ServicePool.DbService.transactionsPooldb.Add(req.Transaction);
            return Task.FromResult(new TransactionStatus
            {
                Status = Others.Constants.TXN_STATUS_SUCCESS,
                Message = "Transaction received successfully!"
            });
        }

        public override Task<TransactionStatus> Transfer(TransactionPost req, ServerCallContext context)
        {
            Console.WriteLine("=== REq: {0}", req);

            // Validating hash
            var calculateHash = UbudKusCoin.Others.UkcUtils.GetTransactionHash(req.Transaction);

            if (!calculateHash.Equals(req.Transaction.Hash))
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Transaction Hash is not valid!"
                });
            }

            Console.WriteLine("=== calculateHash: {0}", calculateHash);
            // validating signature
            var isSignatureValid = verifySignature(req.Transaction);
            if (!isSignatureValid)
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Signature  is not valid!"
                });
            }

            Console.WriteLine("=== isSignatureValid: {0}", isSignatureValid);

            // Check if transaction already in Pool
            var txinPool = ServicePool.DbService.transactionsPooldb.GetByHash(req.Transaction.Hash);
            if (txinPool is not null)
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Double transaction!"
                });

            }

            ServicePool.DbService.transactionsPooldb.Add(req.Transaction);

            // broadcast transaction to all peer including myself.
            Task.Run(() => ServicePool.P2PService.BroadcastTransaction(req.Transaction));

            // Response transaction success
            return Task.FromResult(new TransactionStatus
            {
                Status = Others.Constants.TXN_STATUS_SUCCESS,
                Message = "Transaction done!"
            });
        }


    }
}
