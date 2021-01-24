using System;
using System.Numerics;
using EllipticCurve;

namespace DesktopWallet
{

    public static class Wallet
    {
        public static BigInteger SECREET_NUMBER { set; get; }
        public static string STR_PEM { set; get; }

        public static KeyPair CurrentKeypair { get; set; }

        private static Account CreateAccount()
        {
               var acc = new Account
            {
                Name = "Account 1",
                PublicKey = GetPublicKeyHex(),
                Address = GetAddress(),
                Balance = 0
            };

            return acc;
        }
        public static Account Restore(string secreet)
        {
            CurrentKeypair = GenerateKeyPair(secreet);
         
            return CreateAccount();
        }


        public static Account Create()
        {
            CurrentKeypair = GenerateKeyPair();
            return CreateAccount();
        }

        private static KeyPair GenerateKeyPair(string screet="")
        {

            PrivateKey privateKey = new PrivateKey();
            if (screet != "")
            {
                privateKey = new PrivateKey("secp256k1", BigInteger.Parse(screet));
            }
            
            SECREET_NUMBER = privateKey.secret;
            STR_PEM = privateKey.toPem();
            PublicKey publicKey = privateKey.publicKey();


            var keyPair = new KeyPair()
            {
                PrivateKey = privateKey,
                PublicKey = publicKey
            };
            return keyPair;
        }

        public static string GetPublicKeyHex()
        {
            if (CurrentKeypair == null)
            {
                return null;
            }
            return CurrentKeypair.PublicKey.toString().ConvertToHexString();
        }

        public static string GetAddress()
        {
            if (CurrentKeypair == null)
            {
                return "- - - - - - - - - -";
            }
            return Utils.MakeAddress(CurrentKeypair.PublicKey); ;
        }



        public static string Sign(string dataHash)
        {
            Signature signature = Ecdsa.sign(dataHash, CurrentKeypair.PrivateKey);
            Console.WriteLine(signature.toBase64());
            return signature.toBase64();
        }

      

    }
}