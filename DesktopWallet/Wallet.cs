using System;
using NBitcoin;

namespace DesktopWallet
{

    public static class Wallet
    {

        //TODO WILL SAVE LATER IN APP
        public static KeyPair CurrentKeypair { get; set; }
        public static Mnemonic Mnemonic { get; set; }

        public static Account Restore(string passphrase)
        {
            Mnemonic = new Mnemonic(passphrase, Wordlist.English);
            CurrentKeypair = GenerateKeyPair(Mnemonic, 0);
            var acc = new Account
            {
                Name = "Account 1",
                PublicKey = GetStringPublicKey(),
                Address = GetAddress(),
                Path = 0,
                Balance = 0
            };
            return acc;
        }


        public static Account Create(int path)
        {
            Mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            CurrentKeypair = GenerateKeyPair(Mnemonic, path);

            var acc = new Account
            {
                Name = "Account " + (path + 1),
                PublicKey = GetStringPublicKey(),
                Address = GetAddress(),
                Path = path,
                Balance = 0
            };
            return acc;
        }

        public static KeyPair GenerateKeyPair(Mnemonic mnemonic, int path)
        {
            var masterKey = mnemonic.DeriveExtKey(Config.SEED_SECREET);
            ExtPubKey masterPubKey = masterKey.Neuter();

            // make derivated private and public key 
            ExtKey privateKeyDer = masterKey.Derive((uint)path);
            ExtPubKey publicKeyDer = masterPubKey.Derive((uint)path);

            var keyPair = new KeyPair()
            {
                PrivateKey = privateKeyDer,
                PublicKey = publicKeyDer.Derive((uint)path)
            };
            return keyPair;
        }

        public static string GetStringPublicKey()
        {
            if (CurrentKeypair == null)
            {
                return null;
            }            
            return CurrentKeypair.PublicKey.PubKey.ToHex();
        }

        public static string GetAddress()
        {
            // Console.WriteLine ("== Public Key generated: {0}, length: {1}", publicKeyHex, publicKeyHex.Length );
            // privateKeyDer.ToString (Network.Main));
            if (CurrentKeypair == null)
            {
                return "- - - - - - - - - -";
            }

            var address = CurrentKeypair.PublicKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);
            return address.ToString();
        }

        public static string Sign(string dataHash)
        {
            string signature = CurrentKeypair.PrivateKey.PrivateKey.SignMessage(dataHash);
            Console.WriteLine("== Signature: {0}", signature);
            return signature;
        }

      

    }
}