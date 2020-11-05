using LiteDB;

namespace DB
{
    public class DbAccess
    {
      
        public static LiteDatabase DB { set; get; }
   
        public static string TBL_BLOCKS = "tbl_blocs";
        public static string TBL_TRANSACTION_POOL = "tbl_transaction_pool";
        public static string TBL_TRANSACTIONS = "tbl_transactions";

        /**
        it will create db with name node1.db
        **/       
        public static void Initialize()
        {
            DB = new LiteDatabase(@"node1.db");
        }

        public static void CloseDB(){
            DB.Dispose();
        }

    }
}