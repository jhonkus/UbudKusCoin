
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
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }


        public static string ConvertToDateTime(this Int64 timestamp)
        {
            return new DateTime(timestamp).ToString("dd MMM yyyy hh:mm:ss");;
        }

        public static string GetTransactionHash(TrxInput input, TrxOutput output)
        {
            var trxId = GenHash(input.TimeStamp + input.SenderAddress + output.Amount + output.Fee + output.RecipientAddress);

            return trxId;
        }

    }
}