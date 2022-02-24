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
            Console.WriteLine("...... Transaction initilized.");
        }

        /// <summary>
        /// Add some transactions in same times
        /// </summary>
        /// <param name="transactions"></param>
        /// <returns></returns>
        public string AddBulk(List<Transaction> transactions)
        {
            return ServicePool.DbService.transactionDb.AddBulk(transactions);
        }

        /// <summary>
        /// Create genesis transaction for each genesis account
        /// Sender and recipeint is same
        /// </summary>
        /// <returns></returns>
        public List<Transaction> CreateGenesis()
        {
            var genesisTrx = new List<Transaction>();
            var timeStamp = UkcUtils.GetTime();
            var accounts = ServicePool.FacadeService.Account.GetGenesis();
            for (int i = 0; i < accounts.Count; i++)
            {

                var newTxn = new Transaction()
                {
                    TimeStamp = timeStamp,
                    Sender = accounts[i].Address,
                    Recipient = accounts[i].Address,
                    Amount = accounts[i].Balance,
                    Fee = 0.0f,
                    Height = 1,
                    PubKey = accounts[i].PubKey
                };
                var txHash = GetHash(newTxn);

                newTxn.Hash = txHash;
                newTxn.Signature = ServicePool.WalletService.Sign(txHash);

                genesisTrx.Add(newTxn);
            }

            return genesisTrx;
        }


        /// <summary>
        ///  Get transaction hash
        /// </summary>
        /// <param name="txn"></param>
        /// <returns></returns>
        public string GetHash(Transaction txn)
        {
            var data = txn.TimeStamp + txn.Sender + txn.Amount + txn.Fee + txn.Recipient;
            return UkcUtils.GenHash(UkcUtils.GenHash(data));
        }

    

        public double GetBalance(string address)
        {
            var acc = ServicePool.DbService.accountDb.GetByAddress(address);
            if (acc == null)
            {
                acc = new Account
                {
                    Address = address,
                    Balance = 0,
                    TxnCount = 0,
                    Created = UkcUtils.GetTime(),
                    Updated = UkcUtils.GetTime(),
                    PubKey = address,
                };
                return 0;
            }
            else
            {
                return acc.Balance;
            }
        }


        public List<Transaction> GetForMinting(long height)
        {
            // get transaction from pool
            var txnsInPool = ServicePool.DbService.transactionsPooldb.GetAll();
            var txnsList = txnsInPool.FindAll().ToList();
            var transactions = new List<Transaction>();

            // validator will get coin reward from genesis account
            // to keep total coin in Blockchain not changed
            var conbaseTrx = new Transaction
            {
                TimeStamp = UkcUtils.GetTime(),
                Sender = "-",
                Signature = "-",
                PubKey = "-",
                Height = height,
                Recipient = ServicePool.WalletService.GetAddress(),
                TxType = Constants.TXN_TYPE_VALIDATOR_FEE,
                Fee = 0,
            };


            if (txnsInPool.Count() > 0)
            {
                //sum all fees and give block creator as reward
                conbaseTrx.Amount = UkcUtils.GetTotalFees(txnsList);
                conbaseTrx.Hash = UkcUtils.GetTransactionHash(conbaseTrx);

                // add coinbase trx to list    
                transactions.Add(conbaseTrx);
                transactions.AddRange(txnsList);
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