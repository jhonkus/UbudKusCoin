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
            Console.WriteLine("...... Account initilized.");
        }

        public Account GetByAddress(string address)
        {
            return ServicePool.DbService.accountDb.GetByAddress(address);
        }

        /// <summary>
        /// Genesis account have initial balance
        /// </summary>
        /// <returns></returns>
        public List<Account> GetGenesis()
        {
            var timestamp = UkcUtils.GetTime();
            var list = new List<Account> {
                new Account{
                    // live uniform pudding know thumb hand deposit critic relief asset demand barrel
                    Address = "9SBqYME6T5trNHXqdsYPMPha4yWQbzxd4DPjJBR7KG9A",
                    PubKey = "02c51f708f279643811af172b9f838aabb2cb4c90b683da9c5d4b81d70f00e9af2",
                    Balance = 2000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
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
        /// <param name="to"></param>
        /// <param name="amount"></param>
        public void AddToBalance(string to, double amount)
        {
            var acc = ServicePool.DbService.accountDb.GetByAddress(to);
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
                ServicePool.DbService.accountDb.Add(acc);
            }
            else
            {
                acc.Balance += amount;
                acc.TxnCount += 1;
                acc.Updated = UkcUtils.GetTime();
                ServicePool.DbService.accountDb.Update(acc);
            }
        }

        /// <summary>
        /// Reduce amount from Balance
        /// </summary>
        /// <param name="from"></param>
        /// <param name="amount"></param>
        /// <param name="pubKey"></param>
        public void ReduceFromBalance(string from, double amount, string pubKey)
        {

            var acc = ServicePool.DbService.accountDb.GetByAddress(from);

            if (acc is null)
            {

                acc = new Account
                {
                    Address = from,
                    Balance = -amount,
                    TxnCount = 1,
                    Created = UkcUtils.GetTime(),
                    Updated = UkcUtils.GetTime(),
                    PubKey = pubKey,
                };
                ServicePool.DbService.accountDb.Add(acc);
            }
            else
            {
                acc.Balance -= amount;
                acc.TxnCount += 1;
                acc.PubKey = pubKey;
                acc.Updated = UkcUtils.GetTime();

                ServicePool.DbService.accountDb.Update(acc);
            }
        }

        /// <summary>
        /// Update Balance
        /// </summary>
        /// <param name="txns"></param>
        public void UpdateBalance(List<Transaction> txns)
        {
            foreach (var txn in txns)
            {

                ReduceFromBalance(txn.Sender, txn.Amount, txn.PubKey);
                AddToBalance(txn.Recipient, txn.Amount);

            }
        }

        /// <summary>
        /// Update Genesis Account Balance
        /// </summary>
        /// <param name="trxs"></param>
        public void UpdateBalanceGenesis(List<Transaction> trxs)
        {
            foreach (var trx in trxs)
            {
                AddToBalance(trx.Recipient, trx.Amount);
            }
        }


    }

}