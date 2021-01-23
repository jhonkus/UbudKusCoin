
using System;
using System.Security.Cryptography;
using System.Text;
using EllipticCurve;
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


        public static string GetTransactionHash(TrxInput input, TrxOutput output)
        {
            var trxId = GenHash(input.TimeStamp + input.SenderAddress + output.Amount + output.Fee + output.RecipientAddress);
            return trxId;
        }

        public static string ConvertToHexString(this byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }
        public static byte[] ConvertHexStringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static string MakeAddress(PublicKey publicKey)
        {
            byte[] hash = SHA256.Create().ComputeHash(publicKey.toString());
            return "UKC_" + Convert.ToBase64String(hash);
        }

    }
}