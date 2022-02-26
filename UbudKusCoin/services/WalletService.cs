// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Security.Cryptography;

using NBitcoin;

using NBitcoin.DataEncoders;

namespace UbudKusCoin.Services
{

    public class KeyPair
    {
        public ExtKey PrivateKey { set; get; }
        public ExtPubKey PublicKey { set; get; }
        public string PublicKeyHex { set; get; }
    }
    public class WalletService
    {
        public KeyPair KeyPair { get; set; }
        public Mnemonic Mnemonic { set; get; }

        public string passphrase { set; get; }
        public WalletService()
        {
            this.passphrase = DotNetEnv.Env.GetString("NODE_PASSPHRASE");
        }
        public void Start()
        {
            Console.WriteLine("... Wallet service is starting");
            this.Mnemonic = new Mnemonic(this.passphrase);
            this.KeyPair = GenerateKeyPair(this.Mnemonic, 0);
            Console.WriteLine("...... Wallet service is Ready");
        }

        public static KeyPair GenerateKeyPair(Mnemonic mnemonic, int path)
        {
            var masterKey = mnemonic.DeriveExtKey();
            ExtPubKey masterPubKey = masterKey.Neuter();

            ExtKey privateKeyDer = masterKey.Derive((uint)path);
            ExtPubKey publicKeyDer = masterPubKey.Derive((uint)path);

            var publicKeyHex = publicKeyDer.PubKey.ToHex();
            var keyPair = new KeyPair()
            {
                PrivateKey = privateKeyDer,
                PublicKeyHex = publicKeyHex,
                PublicKey = publicKeyDer,
            };
            return keyPair;
        }

        public ExtPubKey GetPublicKey()
        {
            return this.KeyPair.PublicKey;
        }

        public KeyPair GetKeyPair()
        {
            return this.KeyPair;
        }

        public string GetAddress()
        {
            byte[] hash = SHA256.Create().ComputeHash(this.KeyPair.PublicKey.ToBytes());
            return Encoders.Base58.EncodeData(hash);
        }

        public string Sign(string dataHash)
        {
            return this.KeyPair.PrivateKey.PrivateKey.SignMessage(dataHash); ;
        }

        public static bool verifySignature(string publicKeyHex, string signature, string dataHash)
        {
            var pubKey = new PubKey(publicKeyHex);
            return pubKey.VerifyMessage(dataHash, signature);
        }
    }
}