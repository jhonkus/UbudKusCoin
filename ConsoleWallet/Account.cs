using System;
using System.Numerics;
using System.Security.Cryptography;
using EllipticCurve;

namespace Main
{

    public  class Account
    {
        public BigInteger SecretNumber { set; get; }
        public PrivateKey PrivKey { set; get; }
        public PublicKey PubKey { set; get; }

        public Account(string screet="")
        {
            if (screet != "")
            {
                PrivKey = new PrivateKey("secp256k1", BigInteger.Parse(screet));
            }
            else
            {
                PrivKey = new PrivateKey();
            }
            SecretNumber = PrivKey.secret;
            PubKey = PrivKey.publicKey();
        }

        public string GetPubKeyHex()
        {
            return Convert.ToHexString(PubKey.toString());
        }

        public string GetAddress()
        {
            byte[] hash = SHA256.Create().ComputeHash(PubKey.toString());
            return "UKC_" + Convert.ToBase64String(hash);
        }

        public string CreateSignature(string message)
        {
            Signature signature = Ecdsa.sign(message, PrivKey);
            return signature.toBase64();
        }


    }
}