using System;
using System.Collections.Generic;
using LiteDB;
using Main;

namespace UbudKusCoin
{
    public class Staker
    {
        public string Address { set; get; }
        public double Amount { set; get; }
    }

    public class Stake
    {

        public static List<Staker> StakerList { get; set; }

        public static void Add(Staker staker)
        {
            var stakes = GetAll();

            // insert into database
            stakes.Insert(staker);
        }

        public static ILiteCollection<Staker> GetAll()
        {
            var coll = DbAccess.DB.GetCollection<Staker>(DbAccess.TBL_STACKER);
            return coll;
        }

        internal static void Initialize()
        {
            StakerList = new List<Staker>();
            var staker = GetAll();
            if (staker.Count() < 1)
            {
                // we asume all ICO account stake their coin each 1000
                Add(new Staker
                {
                    Address = "UKC_JavaPsOANbgT5anGjTg0Ih6qdC4mHgbmpF5ptjAJb0g=",
                    Amount = 1000
                });

                Add(new Staker
                {
                    Address = "UKC_mGyJe2kD3cNs4c8d/KHVe4+DSt9mwrLLqlDejXUgdzA=",
                    Amount = 1000
                });

                Add(new Staker
                {
                    Address = "UKC_ZOm+XeyKAEbIb/L41TPEzRRxwMOsZW6HE2WjdxeCFFI=",
                    Amount = 1000
                });

                Add(new Staker
                {
                    Address = "UKC_rMOHTqvkDCLtaoqbkgF3GmM2lewE3R2ZFYDGfq0A/fI=",
                    Amount = 1000
                });

                StakerList.AddRange(GetAll().FindAll());
            }
            else
            {
                StakerList.AddRange(GetAll().FindAll());
            }


        }

        public static double GetStake(string address)
        {
            double balance = 0;
            var collection = GetAll();
            var stakes = collection.Find(x => x.Address == address);
            foreach (var stake in stakes)
            {
                balance += stake.Amount;
            }
            return balance;
        }


        public static string GetCreator()
        {
            var numOfStakes = StakerList.Count;
            // Console.WriteLine("numOfStakes {0}", numOfStakes);
            var random = new Random();
            // Console.WriteLine("random {0}", random);
            int choosed = random.Next(0, numOfStakes);
            // Console.WriteLine("choosed {0}", choosed);
            var staker = StakerList[choosed].Address;
            // Console.WriteLine("staker {0}", staker);
            return staker;
        }
    }
}
