// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Security.Cryptography;
using NBitcoin.DataEncoders;
using NBitcoin;

namespace UbudKusCoin.ConsoleWallet
{
    public class KeyPair
    {
        public ExtKey PrivateKey { set; get; }
        public ExtPubKey PublicKey { set; get; }
        public string PublicKeyHex { set; get; }
    }

    public class Wallet
    {
        public KeyPair KeyPair { get; set; }
        public Mnemonic Mnemonic { set; get; }
        public string Passphrase { set; get; }

        public Wallet()
        {
            Mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            Passphrase = Mnemonic.ToString();
            KeyPair = GenerateKeyPair(Mnemonic, 0);
        }

        public Wallet(string passphrase)
        {
            Mnemonic = new Mnemonic(passphrase);
            Passphrase = Mnemonic.ToString();
            KeyPair = GenerateKeyPair(Mnemonic, 0);
        }

        public ExtPubKey GetPublicKey()
        {
            return KeyPair.PublicKey;
        }

        public KeyPair GetKeyPair()
        {
            return KeyPair;
        }

        public string GetAddress()
        {
            byte[] bytes = SHA256.Create().ComputeHash(KeyPair.PublicKey.ToBytes());
            return Encoders.Base58.EncodeData(bytes);
        }

        public string Sign(string dataHash)
        {
            return KeyPair.PrivateKey.PrivateKey.SignMessage(dataHash);
        }

        public static bool verifySignature(string publicKeyHex, string signature, string dataHash)
        {
            var pubKey = new PubKey(publicKeyHex);
            return pubKey.VerifyMessage(dataHash, signature);
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
    }
}