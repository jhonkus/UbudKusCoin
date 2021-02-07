
using System;
using System.Security.Cryptography;
using System.Text;
using GrpcService;


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

        public static long GetTime()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
            return unixTimeMilliseconds;
        }

        public static string ConvertToDateTime(this Int64 timestamp)
        {
            return new DateTime(timestamp).ToString("dd MMM yyyy hh:mm:ss"); ;
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

        public static string GetTransactionHash(TrxInput input, TrxOutput output)
        {
            var trxId = GenHash(input.TimeStamp + input.SenderAddress + output.Amount + output.Fee + output.RecipientAddress);

            return trxId;
        }

    }
}
