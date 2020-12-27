using System.Text;
using Models;
using System;

namespace Utils
{
    public static class Converter
    {


        /**
        Convert string to array of byte
         */
        public static byte[] ConvertToBytes(this string arg)
        {
            return System.Text.Encoding.UTF8.GetBytes(arg);
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


        public static string ConvertToDateTime(this Int64 timestamp)
        {

            DateTime myDate = new DateTime(timestamp);
            var strDate = myDate.ToString("dd MMM yyyy hh:mm:ss");
            return strDate;

        }


    }

}