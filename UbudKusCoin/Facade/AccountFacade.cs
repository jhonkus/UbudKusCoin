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
                    // owner elevator visual public loyal actual outside trumpet sugar drama impact royal
                    Address = "NFrwmp2Wo6zvPBpzCmDeLH5PNu6pcAg1ZDAUyUTDRYVC",
                    PubKey = "03d8bf992ebda445a512ae687a0601a43d85e623b3df052c3b32a44a895d9b3abd",
                    Balance = 2000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // actual lucky tail message acquire alarm bomb finger route wool reward bike
                    Address = "nBnrTvVoNyx2qg3yeBg3HtaHMbEyFPGJ4EQ183BBsphM",
                    PubKey = "034ab41cd9592200344a8c170cd26510966be1920a60943ea883458719d9e916f9",
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