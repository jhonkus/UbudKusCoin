using System.Text;
using System;
using System.Security.Cryptography;

namespace Main
{
    public static class Utils
    {
        /**
        Convert array of byte to string 
        */
        public static string ConvertToString(this byte[] arg)
        {
            return Encoding.UTF8.GetString(arg, 0, arg.Length);
        }

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

        public static string GetTrxHash(Transaction input)
        {
            var data = input.TimeStamp + input.Sender + input.Amount + input.Fee + input.Recipient;
            var sha256 = SHA256.Create();
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static string GetHash(long timestamp, string lastHash, string transactions)
        {
            SHA256 sha256 = SHA256.Create();
            var strSum = timestamp + lastHash + transactions;
            byte[] sumBytes = Encoding.ASCII.GetBytes(strSum);
            byte[] hashBytes = sha256.ComputeHash(sumBytes);
            return Convert.ToBase64String(hashBytes);
        }

    }

}