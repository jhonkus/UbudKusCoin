
using System;
using System.Security.Cryptography;
using System.Text;
using GrpcService;
using Newtonsoft.Json;


namespace DesktopWallet
{
    public static class Utils
    {

        public static string GenHash(object data)
        {
            var sha256 = SHA256.Create();
            byte[] bytes = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(data));
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }



        public static string GenHash(string data)
        {
            var sha256 = SHA256.Create();
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }



        public static string ConvertToDateTime(this Int64 timestamp)
        {

            DateTime myDate = new DateTime(timestamp);
            var strDate = myDate.ToString("dd MMM yyyy hh:mm:ss");

            return strDate;

        }

        public static string CreateID()
        {
            Guid g = Guid.NewGuid();
            return g.ToString();
        }

        public static string GetTransactionHash(TrxInput input, TrxOutput output)
        {
            var trxId = GenHash(input.TimeStamp + input.SenderAddress + output.Amount + output.Fee + output.RecipientAddress);
            return trxId;
        }


        public static string CreteSignature(string hash, KeyPair keyPair)
        {
            var signature = keyPair.PrivateKey.PrivateKey.SignMessage(hash);
            Console.WriteLine("trx signature: {0}", signature);
            return signature;
        }

    }
}