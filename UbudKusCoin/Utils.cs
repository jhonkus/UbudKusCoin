using System.Text;
using System;
using System.Security.Cryptography;
using GrpcService;
using EllipticCurve;

namespace Main
{
    public static class Utils
    {
        /**
        Convert array of byte to string 
        */
        public static string ConvertToString(this byte[] arg) => Encoding.UTF8.GetString(arg, 0, arg.Length);

        /**
        Convert string to array of byte
         */
        public static byte[] ConvertToBytes(this string arg)
        {
            return Encoding.UTF8.GetBytes(arg);
        }

        public static byte[] ConvertToByte(this Transaction[] lsTrx)
        {
            var transactionsString = Newtonsoft.Json.JsonConvert.SerializeObject(lsTrx);
            return transactionsString.ConvertToBytes();
        }

        public static string ConvertToString(this Transaction[] lsTrx)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(lsTrx);
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


        public static string ConvertToDateTime(this long timestamp)
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

        public static string GenHash(string data)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static string GetTrxHash(Transaction input)
        {
            var data = input.TimeStamp + input.Sender + input.Amount + input.Fee + input.Recipient;
            return GenHash(data);
        }

    }

}