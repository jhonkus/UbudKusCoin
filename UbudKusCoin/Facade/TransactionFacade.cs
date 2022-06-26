// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;

namespace UbudKusCoin.Facade
{
    /// <summary>
    /// Transaction Facade
    /// </summary>
    public class TransactionFacade
    {
        public TransactionFacade()
        {
            Console.WriteLine("...... Transaction innitialized.");
        }

        /// <summary>
        /// Add some transactions in same times
        /// </summary>
        public bool AddBulk(List<Transaction> transactions)
        {
            return ServicePool.DbService.TransactionDb.AddBulk(transactions);
        }

        /// <summary>
        /// Create genesis transaction for each genesis account
        /// Sender and recipeint is same
        /// </summary>
        public List<Transaction> CreateGenesis()
        {
            var genesisTransactions = new List<Transaction>();
            var timeStamp = UkcUtils.GetTime();
            var accounts = ServicePool.FacadeService.Account.GetGenesis();
            
            for (int i = 0; i < accounts.Count; i++)
            {
                var newTransaction = new Transaction()
                {
                    TimeStamp = timeStamp,
                    Sender = accounts[i].Address,
                    Recipient = accounts[i].Address,
                    Amount = accounts[i].Balance,
                    Fee = 0.0f,
                    Height = 1,
                    PubKey = accounts[i].PubKey
                };
                
                var transactionHash = GetHash(newTransaction);
                newTransaction.Hash = transactionHash;
                newTransaction.Signature = ServicePool.WalletService.Sign(transactionHash);

                genesisTransactions.Add(newTransaction);
            }

            return genesisTransactions;
        }
        
        /// <summary>
        ///  Get transaction hash
        /// </summary>
        public string GetHash(Transaction txn)
        {
            var data = $"{txn.TimeStamp}{txn.Sender}{txn.Amount}{txn.Fee}{txn.Recipient}";
            return UkcUtils.GenHash(UkcUtils.GenHash(data));
        }
        
        public double GetBalance(string address)
        {
            var account = ServicePool.DbService.AccountDb.GetByAddress(address);
            if (account == null)
            {
                return 0;
            }

            return account.Balance;
        }

        public List<Transaction> GetForMinting(long weight)
        {
            // get transaction from pool
            var poolTransactions = ServicePool.DbService.PoolTransactionsDb.GetAll().FindAll().ToList();
            var transactions = new List<Transaction>();

            // validator will get coin reward from genesis account
            // to keep total coin in Blockchain not changed
            var conbaseTrx = new Transaction
            {
                TimeStamp = UkcUtils.GetTime(),
                Sender = "-",
                Signature = "-",
                PubKey = "-",
                Height = weight,
                Recipient = ServicePool.WalletService.GetAddress(),
                TxType = Constants.TXN_TYPE_VALIDATOR_FEE,
                Fee = 0,
            };
            
            if (poolTransactions.Any())
            {
                //sum all fees and give block creator as reward
                conbaseTrx.Amount = UkcUtils.GetTotalFees(poolTransactions);
                conbaseTrx.Hash = UkcUtils.GetTransactionHash(conbaseTrx);

                // add coinbase trx to list    
                transactions.Add(conbaseTrx);
                transactions.AddRange(poolTransactions);
            }
            else
            {
                conbaseTrx.Hash = UkcUtils.GetTransactionHash(conbaseTrx);
                transactions.Add(conbaseTrx);
            }

            return transactions;
        }
    }
}