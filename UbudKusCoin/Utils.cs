using System;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Main
{
    public static class Utils
    {
        public static string BytesToHex(byte[] bytes)
        {
            return Convert.ToHexString(bytes).ToLower();
        }

        public static byte[] HexToBytes(string hex)
        {
            // int NumberChars = hex.Length;
            // byte[] bytes = new byte[NumberChars / 2];
            // for (int i = 0; i < NumberChars; i += 2)
            // {
            //     bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            // }
            // return bytes;

            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        public static string GenHash(string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            byte[] hash = SHA256.Create().ComputeHash(bytes);
            return BytesToHex(hash);
        }

        public static long GetTime()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            long unixTimeMilliseconds = now.ToUnixTimeMilliseconds();
            return unixTimeMilliseconds;
        }

        public static string ComputeHash(string hex)
        {
            var algo = new SHA256Managed();
            algo.ComputeHash(HexToBytes(hex));
            var result = algo.Hash;
            return BytesToHex(result);
        }

        public static string SwapString(string arg)
        {
            string result = string.Empty; ;
            for (int i = 0; i < arg.Count(); i += 2)
            {
                result += string.Concat(arg[i + 1], arg[i]);
            }
            return result;
        }

        public static string ReverseString(string arg)
        {
            var result = new string(arg.Reverse().ToArray());
            return result;
        }

        public static string CreateMerkleRoot(IList<string> merkelLeaves)
        {
            if (merkelLeaves == null || !merkelLeaves.Any())

                return string.Empty;

            if (merkelLeaves.Count == 1)
            {
                return merkelLeaves.First();
            }

            if (merkelLeaves.Count % 2 > 0)
            {
                merkelLeaves.Add(merkelLeaves.Last());
            }

            var merkleBranches = new List<string>();

            for (int i = 0; i < merkelLeaves.Count; i += 2)
            {
                var leafPair = string.Concat(merkelLeaves[i], merkelLeaves[i + 1]);
                merkleBranches.Add(GenHash(GenHash(leafPair)));
            }
            return CreateMerkleRoot(merkleBranches);
        }


    }



}