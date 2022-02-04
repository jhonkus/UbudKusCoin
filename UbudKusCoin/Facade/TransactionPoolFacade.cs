using System;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;

namespace UbudKusCoin.Facade
{
    public class TransactionPoolFacade
    {

        public TransactionPoolFacade()
        {
            Console.WriteLine("Transaction pool initilize ....");
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
