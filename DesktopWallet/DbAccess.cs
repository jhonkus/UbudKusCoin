using DesktopWallet;
using LiteDB;

namespace DesktopWallet
{
    public class DbAccess
    {

        public static LiteDatabase DB { set; get; }

        public const string DB_NAME = @"wallet.db";
        public const string TBL_ACCOUNT = "tbl_account";


        /**
        it will create db if no exist 
        **/
        public static void Initialize()
        {
            DB = new LiteDatabase(DB_NAME);
        }

        /**
        Clear Database, delete all rows in each trable
        **/
        public static void ClearDB()
        {
            var coll = DB.GetCollection<Account>(TBL_ACCOUNT);
            coll.DeleteAll();
            
        }
        /**
         * Close database when app closed
         **/
        public static void CloseDB()
        {
            DB.Dispose();
        }

    }
}