using System;
using System.Numerics;
using System.Security.Cryptography;
using EllipticCurve;
using SimpleBase;

namespace Main
{

    public class Account
    {
        public BigInteger SecretNumber { set; get; }
        public PrivateKey PrivKey { set; get; }
        public PublicKey PubKey { set; get; }

        public Account(string screet = "")
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