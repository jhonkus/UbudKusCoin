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
using UbudKusCoin.Others;

namespace UbudKusCoin.Grpc
{
    public class TransactionServiceImpl : TransactionService.TransactionServiceBase
    {
        public override Task<Transaction> GetByHash(Transaction req, ServerCallContext context)
        {
            var transaction = ServicePool.DbService.TransactionDb.GetByHash(req.Hash);
            return Task.FromResult(transaction);
        }

        public override Task<TransactionList> GetRangeByAddress(TransactionPaging req, ServerCallContext context)
        {
            var transactions = ServicePool.DbService.TransactionDb.GetRangeByAddress(req.Address, req.PageNumber, req.ResultPerPage);
            var response = new TransactionList();
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetRange(TransactionPaging req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.TransactionDb.GetRange(req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetPoolRange(TransactionPaging req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.TransactionDb.GetRange(req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public static bool VerifySignature(Transaction txn)
        {
            try
            {
                var pubKey = new PubKey(txn.PubKey);
                return pubKey.VerifyMessage(txn.Hash, txn.Signature)
                    && WalletService.GetAddress(pubKey.ToBytes()) == txn.Sender;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidTransfer(Transaction txn)
        {
            if (txn is null || string.IsNullOrWhiteSpace(txn.Sender) || string.IsNullOrWhiteSpace(txn.Recipient)
                || txn.Sender == "-" || txn.Sender == txn.Recipient
                || !double.IsFinite(txn.Amount) || !double.IsFinite(txn.Fee)
                || txn.Amount <= 0 || txn.Fee < 0)
            {
                return false;
            }

            if (Others.UkcUtils.GetTransactionHash(txn) != txn.Hash || !VerifySignature(txn))
            {
                return false;
            }

            var balance = ServicePool.FacadeService.Transaction.GetBalance(txn.Sender);
            return balance >= txn.Amount + txn.Fee;
        }

        public override Task<TransactionStatus> Receive(TransactionPost req, ServerCallContext context)
        {
            Console.WriteLine("-- Received TXH with hash: {0}, amount {1}", req.Transaction.Hash, req.Transaction.Amount);

            if (!IsValidTransfer(req.Transaction))
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Invalid transaction"
                });
            }
            if (ServicePool.DbService.PoolTransactionsDb.GetByHash(req.Transaction.Hash) != null)
            {
                return Task.FromResult(new TransactionStatus { Status = Others.Constants.TXN_STATUS_FAIL, Message = "Duplicate transaction" });
            }

            ServicePool.DbService.PoolTransactionsDb.Add(req.Transaction);
            return Task.FromResult(new TransactionStatus
            {
                Status = Others.Constants.TXN_STATUS_SUCCESS,
                Message = "Transaction received successfully!"
            });
        }

        public override Task<TransactionStatus> Transfer(TransactionPost req, ServerCallContext context)
        {
            Console.WriteLine("=== Req: {0}", req);

            if (!IsValidTransfer(req.Transaction))
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Invalid transaction"
                });
            }

            // Check if the transaction is in the pool already
            var txinPool = ServicePool.DbService.PoolTransactionsDb.GetByHash(req.Transaction.Hash);
            if (txinPool is not null)
            {
                return Task.FromResult(new TransactionStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Double transaction!"
                });
            }

            ServicePool.DbService.PoolTransactionsDb.Add(req.Transaction);

            // broadcast transaction to all peer including myself.
            Task.Run(() => ServicePool.P2PService.BroadcastTransaction(req.Transaction));

            // Response transaction success
            return Task.FromResult(new TransactionStatus
            {
                Status = Others.Constants.TXN_STATUS_SUCCESS,
                Message = "Transaction completed!"
            });
        }
    }
}
