// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Security.Cryptography;
using System;
using System.Linq;
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
            var compact = KeyPair.PrivateKey.PrivateKey.SignCompact(new uint256(dataHash), true);
            var encoded = new byte[65];
            encoded[0] = (byte)(27 + compact.RecoveryId + 4);
            System.Buffer.BlockCopy(compact.Signature, 0, encoded, 1, compact.Signature.Length);
            return Convert.ToBase64String(encoded);
        }

        public static bool verifySignature(string publicKeyHex, string signature, string dataHash)
        {
            try
            {
                var encoded = Convert.FromBase64String(signature);
                if (encoded.Length != 65 || encoded[0] < 27 || encoded[0] > 35)
                {
                    return false;
                }

                var recoveryId = (encoded[0] - 27) & 3;
                var compact = new CompactSignature(recoveryId, encoded.Skip(1).ToArray());
                var expected = new PubKey(publicKeyHex);
                var recovered = compact.RecoverPubKey(new uint256(dataHash));
                return recovered.ToHex() == expected.ToHex();
            }
            catch
            {
                return false;
            }
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
