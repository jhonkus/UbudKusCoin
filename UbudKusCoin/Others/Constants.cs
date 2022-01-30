namespace UbudKusCoin.Others
{
    public class Constants
    {

        public const double DEFAULT_TRANSACTION_FEE = 0.001;
        public const float COINT_REWARD = 0.001f;
        public const string TBL_BLOCKS = "tbl_blocks";
        public const string TBL_TRANSACTIONS = "tbl_txns";
        public const string TBL_TRANSACTIONS_POOL = "tbl_txns_pool";
        public const string TBL_STAKES = "tbl_stakes";
        public const string TBL_PEERS = "tbl_peers";
        public const string TBL_ACCOUNTS = "tbl_accounts";

        public const string TRANSACTION_TYPE_STAKE = "Staking";
        public const string TRANSACTION_TYPE_TRANSFER = "Transfer";
        public const string TRANSACTION_TYPE_VALIDATOR_FEE = "Validation fee";

        public const int BLOCK_GENERATION_INTERVAL = 30;
        public enum MESSAGE_TYPE { CHAIN, BLOCK, TRANSACTION, CLEAR_TRANSACTIONS };

        //=========
        // public const int TOTAL_COINS = 1000;
        public const int TRANSACTION_THRESHOLD = 5;
        // public const string FIRST_LEADER = "";
        // public const int TRANSACTION_FEE = 1;
        // public const string CHAIN = "chain";
        // public const string STATE = "state";
        // public const string KEY = "key";
        // public const string VALUE = "value";
        // public const string LastHashKey = "l";
        public const string TRANSACTION_TYPE_TRANSACTION = "trx_type_transaction_";
        public const string MESSAGE_TYPE_INENTORY = "INVENTORY";
        public const string MESSAGE_TYPE_GET_BLOCKS = "GET_BLOCKS";
        public const string MESSAGE_TYPE_GET_DATA = "GET_DATA";
        public const string MESSAGE_TYPE_VERSION = "VERSION";
        public const string MESSAGE_SEPARATOR = "||";
        public const string MESSAGE_TYPE_BLOCK = "BLOCK";
        public const string MESSAGE_TYPE_TRANSACTION = "TRANSACTION";
        public const int DIFFICULTY_ADJUSTMENT_INTERVAL = 10;
        public const string MESSAGE_TYPE_CLEAR_TRANSACTIONS = "CLEAR_TRANSACTIONS";
        //=========
    }
}