using LiteDB;


namespace DesktopWallet
{
    public class Account{

        public string Name { set; get; }
        public string Address {set; get;}
        public string PublicKey {set; get;}
        public int Balance {set; get;}
    

        public static ILiteCollection<Account> GetAll()
        {
            var coll = DbAccess.DB.GetCollection<Account>(DbAccess.TBL_ACCOUNT);
            coll.EnsureIndex(x => x.Address);
            return coll;
        }

        public static void Add(Account acc)
        {
            var accounts = GetAll();
            accounts.Insert(acc);
        }

        public static Account Get(string address)
        {
            var coll = DbAccess.DB.GetCollection<Account>(DbAccess.TBL_ACCOUNT);
            coll.EnsureIndex(x => x.Address);
            var acc = coll.FindOne(x => x.Address == address);
            return acc;
        }


        public static void Update(Account acc)
        {
            var accOld = Get(acc.Address);
            if (accOld!=null)
            {
                var coll = DbAccess.DB.GetCollection<Account>(DbAccess.TBL_ACCOUNT);
                coll.EnsureIndex(x => x.Name);
                coll.Update(accOld);
            }
        }

    }
}