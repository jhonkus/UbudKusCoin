using LiteDB;

namespace DB
{
    public class DbAccess
    {
      
        public static LiteDatabase DB { set; get; }

        public const string DB_NAME = @"node3.db";
        public const string TBL_BLOCKS = "tbl_blocks";
        public const string TBL_TRANSACTION_POOL = "tbl_transaction_pool";
        public const string TBL_TRANSACTIONS = "tbl_transactions";

        /**
        it will create db with name node.db
        **/       
        public static void Initialize()
        {
            DB = new LiteDatabase(DB_NAME);
        }

        /**
         * Close database when app closed
         **/
        public static void CloseDB(){
            DB.Dispose();
        }

    }
}