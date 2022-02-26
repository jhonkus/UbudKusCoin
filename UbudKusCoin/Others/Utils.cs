// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using UbudKusCoin.Grpc;

namespace UbudKusCoin.Others
{
    public static class UkcUtils
    {
        public static string GenHash(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return BytesToHex(hash);
        }

        public static byte[] GenHashBytes(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return hash;
        }

        public static string GenHashHex(string hex)
        {
            byte[] bytes = HexToBytes(hex);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return BytesToHex(hash);
        }

        public static string BytesToHex(byte[] bytes)
        {
            return Convert.ToHexString(bytes).ToLower();
        }

        public static byte[] HexToBytes(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
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

        public static string CreateMerkleRoot(string[] txsHash)
        {

            while (true)
            {
                if (txsHash.Length == 0)
                {
                    return string.Empty;
                }

                if (txsHash.Length == 1)
                {
                    return txsHash[0];
                }

                List<string> newHashList = new List<string>();

                int len = (txsHash.Length % 2 != 0) ? txsHash.Length - 1 : txsHash.Length;

                for (int i = 0; i < len; i += 2)
                {
                    newHashList.Add(DoubleHash(txsHash[i], txsHash[i + 1]));
                }

                if (len < txsHash.Length)
                {
                    newHashList.Add(DoubleHash(txsHash[^1], txsHash[^1]));
                }

                txsHash = newHashList.ToArray();
            }
        }

        static string DoubleHash(string leaf1, string leaf2)
        {
            byte[] leaf1Byte = HexToBytes(leaf1);
            //Array.Reverse(leaf1Byte);

            byte[] leaf2Byte = HexToBytes(leaf2);
            //Array.Reverse(leaf2Byte);

            var concatHash = leaf1Byte.Concat(leaf2Byte).ToArray();
            SHA256 sha256 = SHA256.Create();
            byte[] sendHash = sha256.ComputeHash(sha256.ComputeHash(concatHash));

            //Array.Reverse(sendHash);

            return BytesToHex(sendHash).ToLower();
        }

        public static double GetTotalFees(List<Transaction> txns)
        {
            var totFee = txns.AsEnumerable().Sum(x => x.Fee);
            return totFee;
        }

        public static double GetTotalAmount(List<Transaction> txns)
        {
            var totalAmount = txns.AsEnumerable().Sum(x => x.Amount);
            return totalAmount;
        }

        public static string GetTransactionHash(Transaction txn)
        {
            // Console.WriteLine(" get transaction hash {0}", txn);
            var TxnId = GenHash(GenHash(txn.TimeStamp + txn.Sender + txn.Amount + txn.Fee + txn.Recipient));
            // Console.WriteLine(" get transaction hash {0}", TxnId);
            return TxnId;
        }

        public static void PrintBlock(Block block)
        {
            Console.WriteLine("\n===========\nNew Block created");
            Console.WriteLine(" = Height      : {0}", block.Height);
            Console.WriteLine(" = Version     : {0}", block.Version);
            Console.WriteLine(" = Prev Hash   : {0}", block.PrevHash);
            Console.WriteLine(" = Hash        : {0}", block.Hash);
            Console.WriteLine(" = Merkle Hash : {0}", block.MerkleRoot);
            Console.WriteLine(" = Timestamp   : {0}", UkcUtils.ToDateTime(block.TimeStamp));
            Console.WriteLine(" = Difficulty  : {0}", block.Difficulty);
            Console.WriteLine(" = Validator   : {0}", block.Validator);
            Console.WriteLine(" = Nonce       : {0}", block.Nonce);
            Console.WriteLine(" = Number Of Tx: {0}", block.NumOfTx);
            Console.WriteLine(" = Amout       : {0}", block.TotalAmount);
            Console.WriteLine(" = Reward      : {0}", block.TotalReward);
            Console.WriteLine(" = Size        : {0}", block.Size);
            Console.WriteLine(" = Build Time  : {0}", block.BuildTime);
            Console.WriteLine(" = Signature   : {0}", block.Signature);
        }
    }

}