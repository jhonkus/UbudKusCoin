// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace UbudKusCoin.Others
{
    public class Constants
    {

        public const int VERSION = 0;
        public const double DEFAULT_TRANSACTION_FEE = 0.001;
        public const double COINT_REWARD = 0.001f;
        public const string TBL_BLOCKS = "tbl_blocks";
        public const string TBL_TRANSACTIONS = "tbl_txns";
        public const string TBL_TRANSACTIONS_POOL = "tbl_txns_pool";
        public const string TBL_STAKES = "tbl_stakes";
        public const string TBL_PEERS = "tbl_peers";
        public const string TBL_ACCOUNTS = "tbl_accounts";
        public const string TXN_TYPE_STAKE = "Staking";
        public const string TXN_TYPE_TRANSFER = "Transfer";
        public const string TXN_TYPE_VALIDATOR_FEE = "Validation_Fee";

        public const string TXN_STATUS_SUCCESS = "Success";
        public const string TXN_STATUS_FAIL = "Fail";

        public const string MESSAGE_TYPE_INV = "INVENTORY";
        public const string MESSAGE_TYPE_GET_BLOCKS = "GET_BLOCKS";
        public const string MESSAGE_TYPE_GET_DATA = "GET_DATA";
        public const string MESSAGE_TYPE_STATE = "NODE_SATE";
        public const string MESSAGE_SEPARATOR = "||";
        public const string MESSAGE_TYPE_BLOCK = "BLOCK";
        public const string MESSAGE_TYPE_TRANSACTION = "TRANSACTION";
        public const int TXN_THRESHOLD = 5;
        public const int BLOCK_GENERATION_INTERVAL = 30;
        public const int DIFFICULTY_ADJUSTMENT_INTERVAL = 10;
        public const string MESSAGE_TYPE_CLEAR_TRANSACTIONS = "CLEAR_TRANSACTIONS";

    }
}