
using System.Collections.Generic;
using LiteDB;
using UbudKusCoin.Others;
using UbudKusCoin.Grpc;

namespace UbudKusCoin.DB
{
    /// <summary>
    /// This is the Account Database Service that for Add. Update and retrieve account related data
    /// </summary>
    public class AccountDb
    {

        private readonly LiteDatabase _db;
        public AccountDb(LiteDatabase db)
        {
            this._db =db;
        }

        public void Add(Account acc)
        {
            var accs = GetAll();
            accs.Insert(acc);
        }

        public void Update(Account acc)
        {
            var accs = GetAll();
            accs.Update(acc);
        }

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

        public Account GetByAddress(string address)
        {
            var accounts = GetAll();
            accounts.EnsureIndex(x => x.Address);
            var acc = accounts.FindOne(x => x.Address == address);
            return acc;
        }

        public Account GetByPubKey(string pubkey)
        {
            var accounts = GetAll();
            accounts.EnsureIndex(x => x.PubKey);
            var acc = accounts.FindOne(x => x.PubKey == pubkey);
            return acc;
        }

        private ILiteCollection<Account> GetAll()
        {
            return this._db.GetCollection<Account>(Constants.TBL_ACCOUNTS); ;
        }

   

    }
}