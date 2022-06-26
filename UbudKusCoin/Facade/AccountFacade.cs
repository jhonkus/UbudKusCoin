// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;

namespace UbudKusCoin.Facade
{
    public class AccountFacade
    {
        public AccountFacade()
        {
            Console.WriteLine("...... Account innitialized.");
        }

        public Account GetByAddress(string address)
        {
            return ServicePool.DbService.AccountDb.GetByAddress(address);
        }

        /// <summary>
        /// Genesis account have initial balance
        /// </summary>
        public List<Account> GetGenesis()
        {
            var timestamp = UkcUtils.GetTime();
            var list = new List<Account>
            {
                new()
                {
                    // live uniform pudding know thumb hand deposit critic relief asset demand barrel
                    Address = "9SBqYME6T5trNHXqdsYPMPha4yWQbzxd4DPjJBR7KG9A",
                    PubKey = "02c51f708f279643811af172b9f838aabb2cb4c90b683da9c5d4b81d70f00e9af2",
                    Balance = 2000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new()
                {
                    // carbon snack lab junk moment shiver gas dry stem real scale cannon
                    Address = "3pXA6G3o2bu3Mbp9k2NDfXGWPuhCMn4wvZeTAFCf4N5r",
                    PubKey = "03155bbe7fa31d0ebfd779a50a02c1d9444bbf79deb90e1725216d5e8786c632f8",
                    Balance = 3000000000,
                    TxnCount = 2,
                    Created = timestamp,
                    Updated = timestamp
                },
            };
            
            return list;
        }

        /// <summary>
        /// Add amount to Balance
        /// </summary>
        public void AddToBalance(string to, double amount)
        {
            var acc = ServicePool.DbService.AccountDb.GetByAddress(to);
            if (acc is null)
            {
                acc = new Account
                {
                    Address = to,
                    Balance = amount,
                    TxnCount = 1,
                    Created = UkcUtils.GetTime(),
                    Updated = UkcUtils.GetTime(),
                    PubKey = "-"
                };
                
                ServicePool.DbService.AccountDb.Add(acc);
            }
            else
            {
                acc.Balance += amount;
                acc.TxnCount += 1;
                acc.Updated = UkcUtils.GetTime();
                ServicePool.DbService.AccountDb.Update(acc);
            }
        }

        /// <summary>
        /// Reduce amount from Balance
        /// </summary>
        public void ReduceFromBalance(string from, double amount, string publicKey)
        {
            var account = ServicePool.DbService.AccountDb.GetByAddress(from);
            if (account is null)
            {
                account = new Account
                {
                    Address = from,
                    Balance = -amount,
                    TxnCount = 1,
                    Created = UkcUtils.GetTime(),
                    Updated = UkcUtils.GetTime(),
                    PubKey = publicKey,
                };
                
                ServicePool.DbService.AccountDb.Add(account);
            }
            else
            {
                account.Balance -= amount;
                account.TxnCount += 1;
                account.PubKey = publicKey;
                account.Updated = UkcUtils.GetTime();

                ServicePool.DbService.AccountDb.Update(account);
            }
        }

        /// <summary>
        /// Update Balance
        /// </summary>
        public void UpdateBalance(List<Transaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                ReduceFromBalance(transaction.Sender, transaction.Amount, transaction.PubKey);
                AddToBalance(transaction.Recipient, transaction.Amount);
            }
        }

        /// <summary>
        /// Update Genesis Account Balance
        /// </summary>
        public void UpdateBalanceGenesis(List<Transaction> transactions)
        {
            foreach (var transaction in transactions)
            {
                AddToBalance(transaction.Recipient, transaction.Amount);
            }
        }
    }
}