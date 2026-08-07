// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using UbudKusCoin.Services;
using UbudKusCoin.Others;

namespace UbudKusCoin.Grpc
{
    public class TransactionServiceImpl : TransactionService.TransactionServiceBase
    {
        public override Task<Transaction> GetByHash(Transaction req, ServerCallContext context)
        {
            var transaction = AllCanonicalTransactions()
                .FirstOrDefault(item => item.transaction.ComputeIdHex().Equals(req.Hash, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(transaction.transaction is null
                ? new Transaction()
                : CanonicalExplorerMapper.ToTransaction(transaction.transaction, transaction.height, transaction.timeStamp));
        }

        public override Task<TransactionList> GetRangeByAddress(TransactionPaging req, ServerCallContext context)
        {
            var transactions = AllCanonicalTransactions()
                .Where(item => item.transaction.From.Encoded == req.Address || item.transaction.To.Encoded == req.Address)
                .Skip(Math.Max(0, req.PageNumber - 1) * req.ResultPerPage)
                .Take(Math.Max(0, req.ResultPerPage))
                .Select(item => CanonicalExplorerMapper.ToTransaction(item.transaction, item.height, item.timeStamp));
            var response = new TransactionList();
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetRange(TransactionPaging req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = AllCanonicalTransactions()
                .Skip(Math.Max(0, req.PageNumber - 1) * req.ResultPerPage)
                .Take(Math.Max(0, req.ResultPerPage))
                .Select(item => CanonicalExplorerMapper.ToTransaction(item.transaction, item.height, item.timeStamp));
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetPoolRange(TransactionPaging req, ServerCallContext context)
        {
            var response = new TransactionList();
            var transactions = ServicePool.DbService.PoolTransactionsDb.GetRange(req.PageNumber, req.ResultPerPage);
            response.Transactions.AddRange(transactions);
            return Task.FromResult(response);
        }

        public override Task<TransactionList> GetPendingTxns(TransactionPaging req, ServerCallContext context)
            => GetPoolRange(req, context);

        private static IEnumerable<(UbudKusCoin.Core.Types.Transaction transaction, long height, long timeStamp)> AllCanonicalTransactions()
            => ServicePool.CanonicalNodeService.Chain.GetCanonicalBlocks(0)
                .SelectMany(block => block.Txs.Select(transaction => (transaction, block.Height, block.TimeStamp)));

        public static bool VerifySignature(Transaction txn)
        {
            return WalletService.CheckSignature(txn.PubKey, txn.Signature, txn.Hash)
                && WalletService.GetAddress(new NBitcoin.PubKey(txn.PubKey).ToBytes()) == txn.Sender;
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
            SafeTask.Run(() => ServicePool.P2PService.BroadcastTransaction(req.Transaction), "gRPC Transfer P2P Broadcast");

            // Response transaction success
            return Task.FromResult(new TransactionStatus
            {
                Status = Others.Constants.TXN_STATUS_SUCCESS,
                Message = "Transaction completed!"
            });
        }
    }
}
