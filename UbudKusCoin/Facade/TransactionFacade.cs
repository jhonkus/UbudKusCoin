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

    public class TransactionFacade
    {

        public TransactionFacade()
        {
            Console.WriteLine("...... Transaction initilized.");
        }

        public string AddBulk(List<Transaction> transactions)
        {
            return ServicePool.DbService.transactionDb.AddBulk(transactions);
        }




        /**
        create transaction for each genesis account
        **/
        public List<Transaction> CreateGenesis()
        {
            var genesisTrx = new List<Transaction>();
            var timeStamp = Utils.GetTime();
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
                newTxn.Hash = GetHash(newTxn);
                newTxn.Signature = "-"; //TODO Generate signauter
                genesisTrx.Add(newTxn);
            }

            return genesisTrx;
        }


        public string GetHash(Transaction txn)
        {
            var data = txn.TimeStamp + txn.Sender + txn.Amount + txn.Fee + txn.Recipient;
            return Utils.GenHash(Utils.GenHash(data));
        }

        public void AddBalance(string to, double amount)
        {
            var acc = ServicePool.DbService.accountDb.GetByAddress(to);
            if (acc is null)
            {
                acc = new Account
                {
                    Address = to,
                    Balance = amount,
                    TxnCount = 1,
                    Created = Utils.GetTime(),
                    Updated = Utils.GetTime(),
                    PubKey = "-"
                };
                ServicePool.DbService.accountDb.Add(acc);
            }
            else
            {
                acc.Balance += amount;
                acc.TxnCount += 1;
                acc.Updated = Utils.GetTime();
                ServicePool.DbService.accountDb.Update(acc);
            }
        }

        public void ReduceBalance(string from, double amount, string pubKey)
        {

            var acc = ServicePool.DbService.accountDb.GetByAddress(from);

            if (acc is null)
            {

                acc = new Account
                {
                    Address = from,
                    Balance = -amount,
                    TxnCount = 1,
                    Created = Utils.GetTime(),
                    Updated = Utils.GetTime(),
                    PubKey = pubKey,
                };
                ServicePool.DbService.accountDb.Add(acc);
            }
            else
            {
                acc.Balance -= amount;
                acc.TxnCount += 1;
                acc.PubKey = pubKey;
                acc.Updated = Utils.GetTime();

                ServicePool.DbService.accountDb.Update(acc);
            }
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
                    Created = Utils.GetTime(),
                    Updated = Utils.GetTime(),
                    PubKey = address,
                };
                return 0;
            }
            else
            {
                return acc.Balance;
            }
        }

        public void UpdateBalance(List<Transaction> txns)
        {
            foreach (var txn in txns)
            {

                switch (txn.TxType)
                {

                    case Constants.TXN_TYPE_TRANSFER:
                        ReduceBalance(txn.Sender, txn.Amount, txn.PubKey);
                        AddBalance(txn.Recipient, txn.Amount);
                        break;
                    case Constants.TXN_TYPE_STAKE:
                        
                        // add logic for stake

                        
                        break;
                    case Constants.TXN_TYPE_VALIDATOR_FEE:
                        //    if (this.validators.update(transaction)) {
                        //         this.accounts.decrement(
                        //         transaction.input.from,
                        //         transaction.output.amount
                        //         );
                        //         this.accounts.transferFee(block, transaction);
                        //     }
                        break;
                }


            }
        }

        public void UpdateBalanceGenesis(List<Transaction> trxs)
        {
            foreach (var trx in trxs)
            {
                AddBalance(trx.Recipient, trx.Amount);
            }
        }

        public List<Transaction> GetForMinting(long height)
        {
            // get transaction from pool
            var txnsInPool = ServicePool.DbService.transactionsPooldb.GetAll();
            var txnsList = txnsInPool.FindAll().ToList();
            var validator = ServicePool.FacadeService.Stake.GetValidator();
            var transactions = new List<Transaction>();

            // validator will get coin reward from genesis account
            // to keep total coin in Blockchain not changed
            var conbaseTrx = new Transaction
            {
                Amount = 0,
                Fee = 0,
                TimeStamp = Utils.GetTime(),
                Sender = "UkcDEfU9gGnm9tGjmFtXRjirf2LuohU5CzjWunEkPNbUcFW",
                Height = height,
                PubKey = validator.PubKey,
                Recipient = validator.Address,
                Signature = "-",
                TxType = Constants.TXN_TYPE_VALIDATOR_FEE,
            };


            if (txnsInPool.Count() > 0)
            {
                //sum all fees and give block creator as reward
                conbaseTrx.Amount = Utils.GetTotalFees(txnsList);
                conbaseTrx.Hash = Utils.GetTransactionHash(conbaseTrx); ;

                // add coinbase trx to list    
                transactions.Add(conbaseTrx);
                transactions.AddRange(txnsList);
            }
            else
            {
                conbaseTrx.Hash = Utils.GetTransactionHash(conbaseTrx);
                transactions.Add(conbaseTrx);
            }
            return transactions;
        }


    }

}