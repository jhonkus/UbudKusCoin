// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

using LiteDB;

using UbudKusCoin.Grpc;
using UbudKusCoin.Others;

namespace UbudKusCoin.DB
{
    /// <summary>
    /// Account Database, for Add. Update and retrieve account
    /// </summary>
    public class AccountDb
    {

        private readonly LiteDatabase _db;
        public AccountDb(LiteDatabase db)
        {
            this._db = db;
        }

        /// <summary>
        /// Add new Account
        /// </summary>
        /// <param name="acc"></param>
        public void Add(Account acc)
        {
            var accs = GetAll();
            accs.Insert(acc);
        }

        /// <summary>
        /// update an Account 
        /// </summary>
        /// <param name="acc"></param>
        public void Update(Account acc)
        {
            var accs = GetAll();
            accs.Update(acc);
        }

        /// <summary>
        /// Get accounts with paging, page number and result per page
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="resultPerPage"></param>
        /// <returns></returns>
        public IEnumerable<Account> GetRange(int pageNumber, int resultPerPage)
        {
            var accs = GetAll();
            accs.EnsureIndex(x => x.Balance);
            var query = accs.Query()
                .OrderByDescending(x => x.Balance)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }

        /// <summary>
        /// Get Account by it's Address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public Account GetByAddress(string address)
        {
            var accounts = GetAll();
            if (accounts is null)
            {
                return null;
            }
            accounts.EnsureIndex(x => x.Address);
            var acc = accounts.FindOne(x => x.Address == address);
            return acc;
        }

        /// <summary>
        /// Get an Account by its Public Key
        /// </summary>
        /// <param name="pubkey"></param>
        /// <returns></returns>
        public Account GetByPubKey(string pubkey)
        {
            var accounts = GetAll();
            accounts.EnsureIndex(x => x.PubKey);
            var acc = accounts.FindOne(x => x.PubKey == pubkey);
            return acc;
        }

        private ILiteCollection<Account> GetAll()
        {
            return _db.GetCollection<Account>(Constants.TBL_ACCOUNTS); ;
        }



    }
}