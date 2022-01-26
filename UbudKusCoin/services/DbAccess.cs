using LiteDB;
using UbudKusCoin.Models;
using UbudKusCoin.Services;

namespace UbudKusCoin.Services
{
    public class DbAccess
    {

        public static LiteDatabase DB_BLOCKS { set; get; }
        public static LiteDatabase DB_ACCOUNTS { set; get; }
        public static LiteDatabase DB_TXNS { set; get; }
        public static LiteDatabase DB_OTHERS { set; get; }
        public const string TBL_BLOCKS = "tbl_blocks";
        public const string TBL_TRANSACTIONS = "tbl_txns";
        public const string TBL_STAKES = "tbl_stakes";
        public const string TBL_PEERS = "tbl_peers";
        public const string TBL_ACCOUNTS = "tbl_accounts";

        /**
        it will create db with name node.db
        **/
        public static void Initialize()
        {
            DB_BLOCKS = new LiteDatabase(@"DbFiles//blocks.db");
            DB_ACCOUNTS = new LiteDatabase(@"DbFiles//accounts.db");
            DB_TXNS = new LiteDatabase(@"DbFiles//txns.db");
            DB_OTHERS = new LiteDatabase(@"DbFiles//others.db");
        }

        /**
        Clear Database, delete all rows in each trable
        **/
        public static void ClearDB()
        {
            DB_BLOCKS.GetCollection<Block>(TBL_BLOCKS).DeleteAll();
            DB_TXNS.GetCollection<Transaction>(TBL_TRANSACTIONS).DeleteAll();
            DB_ACCOUNTS.GetCollection<Account>(TBL_ACCOUNTS).DeleteAll();
            DB_OTHERS.GetCollection<Block>(TBL_STAKES).DeleteAll();
            DB_OTHERS.GetCollection<Block>(TBL_PEERS).DeleteAll();
        }
        /**
         * Close database when app closed
         **/
        public static void CloseDB()
        {
            DB_BLOCKS.Dispose();
        }

    }
}