// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Numerics;
using System.Security.Cryptography;
using EllipticCurve;
using SimpleBase;

namespace UbudKusCoin.ConsoleWallet
{

    public class AccountExt
    {
        public BigInteger SecretNumber { set; get; }
        public PrivateKey PrivKey { set; get; }
        public PublicKey PubKey { set; get; }

        public AccountExt(string screet = "")
        {
            if (screet != "")
            {
                this.PrivKey = new PrivateKey("secp256k1", BigInteger.Parse(screet));
            }
            else
            {
                this.PrivKey = new PrivateKey();
            }
            this.SecretNumber = PrivKey.secret;
            this.PubKey = PrivKey.publicKey();
        }

        public string GetPubKeyHex()
        {
            return Convert.ToHexString(PubKey.toString()).ToLower();
        }

        public string GetAddress()
        {
            byte[] hash = SHA256.Create().ComputeHash(PubKey.toString());
            string result = Base58.Ripple.Encode(hash);
            return "Ukc" + result;
        }

        public string CreateSignature(string message)
        {
            Signature signature = Ecdsa.sign(message, PrivKey);
            return signature.toBase64();
        }


    }
}