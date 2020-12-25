        using System;
        using System.Security.Cryptography;
        using System.Text;

        namespace Models
    {

        public class Block
            {
                public int Height { get; set; }
                public long TimeStamp { get; set; }
                public string PrevHash { get; set; }
                public string Hash { get; set; }
                public string Transactions { get; set; }


                public Block(Block lastBlock, string transactions)
                {
                    var lastHeight = lastBlock.Height;
                    var lastHash = lastBlock.Hash;
                    Height = lastHeight + 1;
                    TimeStamp = DateTime.Now.Ticks;
                    PrevHash = lastHash;
                    Transactions = transactions;
                    Hash = GetHash(TimeStamp, lastHash, transactions);
                }

                public Block(int height, long timestamp, string lastHash, string hash, string transactions)
                {
                    Height = height;
                    TimeStamp = timestamp;
                    PrevHash = lastHash;
                    Hash = hash;
                    Transactions = transactions;
                }


                /**
                Create genesis block
                **/
                public static Block Genesis()
                {
                    var ts = new DateTime(2019, 10, 24);
                    var genesisTrx = "Genesis Block created by P.Kusuma  on 2019 10 24";
                    var hash = GetHash(ts.Ticks, "-", genesisTrx);
                    //var block = new Block(1, ts.Ticks, Convert.ToBase64String(Encoding.ASCII.GetBytes("-")), hash, Convert.ToBase64String(Encoding.ASCII.GetBytes(genesisTrx)));
                    var block = new Block(1, ts.Ticks, Convert.ToBase64String(Encoding.ASCII.GetBytes("-")), hash, genesisTrx);
                    return block;
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
