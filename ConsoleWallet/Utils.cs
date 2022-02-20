
using System;
using System.Security.Cryptography;
using System.Text;
using UbudKusCoin.Services;


namespace Main
{
    public static class Utils
    {

        public static string GenHash(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        public static DateTime ToDateTime(long unixTime)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTime).ToLocalTime();
            return dtDateTime;
        }

        public static long GetTime()
        {
            long epochTicks = new DateTime(1970, 1, 1).Ticks;
            long nowTicks = DateTime.UtcNow.Ticks;
            long tmStamp = ((nowTicks - epochTicks) / TimeSpan.TicksPerSecond);
            return tmStamp;
        }


        public static string StringToHex(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            return BytesToHex(bytes);
        }
        public static string BytesToHex(byte[] bytes)
        {
            return Convert.ToHexString(bytes).ToLower();
        }

        public static byte[] HexToBytes(string hex)
        {
            //    return Enumerable.Range(0, hex.Length)
            //     .Where(x => x % 2 == 0)
            //     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
            //     .ToArray();

            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public static string GetTransactionHash(TxnInput input, TxnOutput output)
        {
            var TxnId = GenHash(GenHash(input.TimeStamp + input.SenderAddress + output.Amount + output.Fee + output.RecipientAddress));
            return TxnId;
        }

    }
}