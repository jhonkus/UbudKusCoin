using System;
using Grpc.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using UbudKusCoin.Others;
using UbudKusCoin.Models;
using UbudKusCoin.Services;

namespace UbudKusCoin.Services
{

    public class AccountServiceImpl : AccountService.AccountServiceBase
    {

        public override Task<AllAccountResponse> GetAll(AllAccountRequest request, ServerCallContext context)
        {
            Console.WriteLine("page {0} numrows {1}", request.PageNumber, request.ResultPerPage);

            var response = new AllAccountResponse();
            var accounts = GetAllFromDB(request.PageNumber, request.ResultPerPage);
            if (accounts is null)
            {
                return Task.FromResult(response);
            }
            response.Accounts.Add(accounts);
            return Task.FromResult(response);
        }


        public static IEnumerable<Account> GetAllFromDB(int pageNumber, int resultPerPage)
        {
            var coll = DbAccess.DB_ACCOUNTS.GetCollection<Account>(DbAccess.TBL_ACCOUNTS);
            coll.EnsureIndex(x => x.Balance);
            var query = coll.Query()
                .OrderByDescending(x => x.Balance)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }



        public static void AddBalance(string to, double amount)
        {
            var accounts = DbAccess.DB_ACCOUNTS.GetCollection<Account>(DbAccess.TBL_ACCOUNTS);
            accounts.EnsureIndex(x => x.Address);
            var acc = accounts.FindOne(x => x.Address == to);

            if (acc is null)
            {
                acc = new Account
                {
                    Address = to,
                    Balance = amount,
                    TxnCount = 0,
                    LastUpdate = Utils.GetTime()
                };
                accounts.Insert(acc);
            }
            else
            {
                acc.Balance += amount;
                // acc.TxnCount += 1; // receipient not consider transaction
                acc.LastUpdate = Utils.GetTime();
                accounts.Update(acc);
            }
        }

        public static void ReduceBalance(string from, double amount)
        {
            var accounts = DbAccess.DB_ACCOUNTS.GetCollection<Account>(DbAccess.TBL_ACCOUNTS);
            accounts.EnsureIndex(x => x.Address);
            var acc = accounts.FindOne(x => x.Address == from);
            if (acc is null)
            {
                Console.WriteLine("== ReduceBalance is null");
                acc = new Account
                {
                    Address = from,
                    Balance = -amount,
                    TxnCount = 1,
                    LastUpdate = Utils.GetTime()
                };
                accounts.Insert(acc);
            }
            else
            {
                Console.WriteLine("== ReduceBalance is null");
                acc.Balance -= amount;
                acc.TxnCount += 1;
                acc.LastUpdate = Utils.GetTime();
                accounts.Update(acc);
                Console.WriteLine("== ReduceBalance acc Balance: {0}", acc.Balance);
            }
        }

        public static double GetBalance(string address)
        {
            var accounts = DbAccess.DB_ACCOUNTS.GetCollection<Account>(DbAccess.TBL_ACCOUNTS);
            accounts.EnsureIndex(x => x.Address);
            var acc = accounts.FindOne(x => x.Address == address);
            if (acc == null)
            {
                acc = new Account
                {
                    Address = address,
                    Balance = 0,
                    TxnCount = 0,
                    LastUpdate = Utils.GetTime()
                };
                return 0;
            }
            else
            {
                return acc.Balance;
            }
        }

        private static void Update(string from, string to, double amount)
        {

            ReduceBalance(from, amount);
            AddBalance(to, amount);
        }

        public static void UpdateAccountBalance(IList<Transaction> trxs)
        {
            foreach (var trx in trxs)
            {
                Update(trx.Sender, trx.Recipient, trx.Amount);
            }
        }
    }
}