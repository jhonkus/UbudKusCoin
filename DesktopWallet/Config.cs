namespace DesktopWallet
{
    public static class Config
    {
        public const int Version = 1;
        public const string NodeAddress = "127.0.0.1:7170";
        public const int TOTAL_COINS = 1000;
        public const int TRANSACTION_THRESHOLD = 1;
        public const string FIRST_LEADER = "";
        public const int TRANSACTION_FEE = 1;
        public static readonly string[] KnownNodes = { "127.0.0.1:7177" };
        // forger should be one of genesis account
        public const string FORGER_SECREET = "L3qUYwwFNEwF2NZdREKQshZAeAKdogrp2ViaznWeLHxjtiQe2CRf";
        public const string FORGER_PUBLIC_KEY = "03841f58b856388e50cf83a8717cbdb1cf93f20eabaecfda5519c985bc0b7a97d3";
        public const string CREATOR_SECREET = "L3k8aBwu8tKYn3yWiEJbVH1P747jPdNiKBSQEFJCfDhm1NcA2umi";
        public const string CREATOR_PUBLIC_KEY = "02b443cf0150da448b36a6f1f1de331804f85f1eac84265981017990d0b4b02718";
        public const string CREATOR_ADDRESS = "1A6SfoGvX5H398rYAS6pgQ1mUR648LtWrK";
        public const string SEED_SECREET = "-";
    }
}

