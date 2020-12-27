using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Utils;

namespace Models
{

    public class Block
    {
        public int Height { get; set; }
        public long TimeStamp { get; set; }
        public byte[] PrevHash { get; set; }
        public byte[] Hash { get; set; }
        public Transaction[] Transactions { get; set; }
        public string Creator { get; set; }

        public Block(int height, byte[] prevHash, List<Transaction> transactions, string creator)
        {
            Height = height;
            PrevHash = prevHash;
            TimeStamp = DateTime.Now.Ticks;
            Transactions = transactions.ToArray();
            Hash = GenerateHash();
            Creator = creator;
        }

        public byte[] GenerateHash()
        {
            var sha = SHA256.Create();
            byte[] timeStamp = BitConverter.GetBytes(TimeStamp);

            var transactionHash = Transactions.ConvertToByte();

            byte[] headerBytes = new byte[timeStamp.Length + PrevHash.Length + transactionHash.Length];

            Buffer.BlockCopy(timeStamp, 0, headerBytes, 0, timeStamp.Length);
            Buffer.BlockCopy(PrevHash, 0, headerBytes, timeStamp.Length, PrevHash.Length);
            Buffer.BlockCopy(transactionHash, 0, headerBytes, timeStamp.Length + PrevHash.Length, transactionHash.Length);

            byte[] hash = sha.ComputeHash(headerBytes);

            return hash;

        }


    }
}
