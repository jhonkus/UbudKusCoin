// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;

namespace UbudKusCoin.Facade
{
    public class TransactionPoolFacade
    {

        public TransactionPoolFacade()
        {
            Console.WriteLine("...... Transaction pool initilized");
        }

        public bool IsTransactionExist(Transaction txn)
        {
            var transaction = ServicePool.DbService.transactionsPooldb.GetByHash(txn.Hash);
            if (transaction is null)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

    }
}
