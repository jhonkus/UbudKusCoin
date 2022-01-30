using System;
using System.Collections.Generic;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;

namespace UbudKusCoin.Facade
{
    public class StakeFacade
    {
        public StakeFacade()
        {
            Initialize();
            Console.WriteLine("Stake initilize ....");
        }

        internal void Initialize()
        {
            var stakes = ServicePool.DbService.stakeDb.GetAll();
            var timestamp = Utils.GetTime();

            if (stakes.Count() < 1)
            {
                // we asume all ICO account stake their coin each 1000000
                stakes.Insert(new Stake
                {
                    Address = "UkcsYhnBQNBZjhDWHHPiyu8Arx75KjWH9Lnc6MVaCi2SMkb",
                    Amount = 1000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"

                });

                stakes.Insert(new Stake
                {
                    Address = "UkcBEzxjpGmiG5XMUmGwkHLP2soctjkbvbTUiqD3DBK2cAr",
                    Amount = 11000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"
                });

                stakes.Insert(new Stake
                {
                    Address = "Ukcf8vVeDsk99k5T14ENhUezWHGHKhfouTFK1iDzbyEirbP",
                    Amount = 12000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"
                });

                stakes.Insert(new Stake
                {
                    Address = "UkcUdQsv5XUqPS4NrqeGVcACtLazfUrWVto6NF5pPnU5ZuK",
                    Amount = 14000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"
                });

                // 5
                stakes.Insert(new Stake
                {
                    Address = "UkcpZbACs3grAzbkXZZqBdWNnKqLD4iVPFCZmUP9theWHMm",
                    Amount = 15000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"
                });

                // 6
                stakes.Insert(new Stake
                {
                    Address = "UkcbE1dCzu28bNPAXayrdL2ghUsCc4mMDbuS2gpTV18Wmu",
                    Amount = 16000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"
                });

                // 7
                stakes.Insert(new Stake
                {
                    Address = "UkcE5LKJtAU32qYwUFzKa4Tm5v74ZWqB43hHQVaX17Zuoy",
                    Amount = 17000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"
                });

                // 8
                stakes.Insert(new Stake
                {
                    Address = "UkcUY6FMwoLjmGaipWZp6n43Dmg9ETbnQyecGaSy3XvLx7f",
                    Amount = 18000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"
                });

                // 9
                stakes.Insert(new Stake
                {
                    Address = "Ukc9z9Li8csur53KDjhjgmyyZQhmy9xXqE556pcK6qFH2x4",
                    Amount = 19000000,
                    Height = 1,
                    TimeStamp = timestamp,
                    PubKey = "--"
                });


            }
        }

        public Stake GetValidator()
        {
            var stakes = ServicePool.DbService.stakeDb.GetAll();
            var random = new Random();
            int choosed = random.Next(1, stakes.Count());
            var validator = stakes.FindById(choosed);
            return validator;
        }

        public List<Stake> GetRange(int pageNumber, int resultPerPage)
        {
            return ServicePool.DbService.stakeDb.GetRange(pageNumber, resultPerPage);
        }
    }
}
