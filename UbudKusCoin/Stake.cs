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

            // update list
            StakerList.Add(staker);
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
                // we asume all ICO account stake their coin each 1000000
                Add(new Staker
                {
                    Address = "UkcsYhnBQNBZjhDWHHPiyu8Arx75KjWH9Lnc6MVaCi2SMkb",
                    Amount = 1000000
                });

                Add(new Staker
                {
                    Address = "UkcBEzxjpGmiG5XMUmGwkHLP2soctjkbvbTUiqD3DBK2cAr",
                    Amount = 11000000
                });

                Add(new Staker
                {
                    Address = "Ukcf8vVeDsk99k5T14ENhUezWHGHKhfouTFK1iDzbyEirbP",
                    Amount = 12000000
                });

                Add(new Staker
                {
                    Address = "UkcUdQsv5XUqPS4NrqeGVcACtLazfUrWVto6NF5pPnU5ZuK",
                    Amount = 14000000
                });

                // 5
                Add(new Staker
                {
                    Address = "UkcpZbACs3grAzbkXZZqBdWNnKqLD4iVPFCZmUP9theWHMm",
                    Amount = 15000000
                });

                // 6
                Add(new Staker
                {
                    Address = "UkcbE1dCzu28bNPAXayrdL2ghUsCc4mMDbuS2gpTV18Wmu",
                    Amount = 16000000
                });

                // 7
                Add(new Staker
                {
                    Address = "UkcE5LKJtAU32qYwUFzKa4Tm5v74ZWqB43hHQVaX17Zuoy",
                    Amount = 17000000
                });

                // 8
                Add(new Staker
                {
                    Address = "UkcUY6FMwoLjmGaipWZp6n43Dmg9ETbnQyecGaSy3XvLx7f",
                    Amount = 18000000
                });

                // 9
                Add(new Staker
                {
                    Address = "Ukc9z9Li8csur53KDjhjgmyyZQhmy9xXqE556pcK6qFH2x4",
                    Amount = 19000000
                });

                // 10
                Add(new Staker
                {
                    Address = "Ukc9z9Li8csur53KDjhjgmyyZQhmy9xXqE556pcK6qFH2x4",
                    Amount = 20000000
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


        public static string GetValidator()
        {
            var numOfStakes = StakerList.Count;
            var random = new Random();
            int choosed = random.Next(0, numOfStakes);
            var staker = StakerList[choosed].Address;
            return staker;
        }
    }
}
