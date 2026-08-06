// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Linq;
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
        public string Passphrase { set; get; }

        public WalletService()
        {
            Passphrase = DotNetEnv.Env.GetString("NODE_PASSPHRASE");
        }

        public void Start()
        {
            Console.WriteLine("... Wallet service is starting");
            var walletPath = DotNetEnv.Env.GetString("WALLET_STORE_PATH", @"DbFiles/wallet.vault");
            var seedWords = DotNetEnv.Env.GetString("NODE_PASSPHRASE", string.Empty).Trim();
            var snapshot = WalletVault.LoadOrCreate(walletPath, seedWords, 0);
            Mnemonic = new Mnemonic(snapshot.MnemonicWords);
            Passphrase = Mnemonic.ToString();
            KeyPair = GenerateKeyPair(Mnemonic, snapshot.DerivationPath);
            Console.WriteLine("...... Wallet service is ready");
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
            return KeyPair.PublicKey;
        }

        public KeyPair GetKeyPair()
        {
            return KeyPair;
        }

        public string GetAddress()
        {
            return GetAddress(KeyPair.PublicKey.PubKey.ToBytes());
        }

        public static string GetAddress(byte[] publicKey)
        {
            byte[] hash = SHA256.Create().ComputeHash(publicKey);
            return Encoders.Base58.EncodeData(hash);
        }

        public string Sign(string dataHash)
        {
            var compact = KeyPair.PrivateKey.PrivateKey.SignCompact(new uint256(dataHash), true);
            var encoded = new byte[65];
            encoded[0] = (byte)(27 + compact.RecoveryId + 4);
            Buffer.BlockCopy(compact.Signature, 0, encoded, 1, compact.Signature.Length);
            return Convert.ToBase64String(encoded);
        }

        public static bool CheckSignature(string publicKeyHex, string signature, string dataHash)
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

        public static bool CheckSignatureForAddress(string address, string signature, string dataHash)
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
                var recovered = compact.RecoverPubKey(new uint256(dataHash));
                return GetAddress(recovered.ToBytes()) == address;
            }
            catch
            {
                return false;
            }
        }
    }
}
