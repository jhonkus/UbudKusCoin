// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using NBitcoin;

namespace UbudKusCoin.Models
{
    public class KeyPair
    {
        public ExtKey PrivateKey { set; get; }
        public ExtPubKey PublicKey { set; get; }
        public string PublicKeyHex { set; get; }
    }
}