using System.Collections.Generic;

namespace UbudKusCoin.Models
{
    public class IcoAccount
    {
        public string Address { set; get; }
        public double Balance { set; get; }
        public string PublicKey { set; get; }
    }

    public static class IcoBalance
    {


        public static List<IcoAccount> GetIcoAccounts()
        {

            var list = new List<IcoAccount>
            {

                new IcoAccount
                {
                    // 1. secreet nunber
                    // 11520842075416936956337166257543145030894758329506615265245623459159831684481
                    Address = "UkcsYhnBQNBZjhDWHHPiyu8Arx75KjWH9Lnc6MVaCi2SMkb",
                    PublicKey = "a5185d90719f52615930a6f0249ef7e1310a90159eef573805577288db66e107f5b71cdfa607eb759c1e43a1e08375b775019a2086794c61a042a6db7ea58af4",
                    Balance = 70000000
                },

                new IcoAccount
                {
                    // 2. secret number
                    // 50097633609371174574534106620065769324210518368794492873657273912701099632384
                    Address = "UkcBEzxjpGmiG5XMUmGwkHLP2soctjkbvbTUiqD3DBK2cAr",
                    PublicKey = "9c8f0d364104cad2d7928e606e7927d23c181297969d95be3e273c4a1aff0f3362f4c9c8b49e5987a13da1102b555392620865eb54f14d938f180d460daeb0d3",
                    Balance = 50000000
                },

                new IcoAccount
                {
                    // 3. secret number
                    // 17444769289605527965285869029990128769824151562466867025336412934980476651053
                    Address = "Ukcf8vVeDsk99k5T14ENhUezWHGHKhfouTFK1iDzbyEirbP",
                    PublicKey = "7b2240cc9370f6446b7df3b5b27a5da32e1d9c657fbfc9860fff8d9a9cbc250ea224536045e289db13f36561e73b6ecad88a977604d98de56623e64431ba165b",
                    Balance = 45000000
                },

                new IcoAccount
                {
                    // 4. secret number
                    // 7611318794264389102622393469331744367164041724526682955404226892006202219169
                    Address = "UkcUdQsv5XUqPS4NrqeGVcACtLazfUrWVto6NF5pPnU5ZuK",
                    PublicKey = "f19681e8249493fc3b42a2e4de8dc544a136137fa27b46a6feff3be1d849e61d89e4c4b6ebf15ad5a67c7600de55bc39cb71758b96f8e9ec90f81e729b1f8d39",
                    Balance = 30000000
                },

                new IcoAccount
                {
                    // 5. secret number
                    // 47560670064048294590896946082628569204733394872380954885025618568906799210109
                    Address = "UkcpZbACs3grAzbkXZZqBdWNnKqLD4iVPFCZmUP9theWHMm",
                    PublicKey = "23812994c2edcb014eae1b3920e0021aed931d8c112066787c5a2e4e4b5f63a20e38ca5d1262c395e78e44f231372d6ff201cd7219d19c1393044209d8ee0f95",
                    Balance = 35000000
                },

                new IcoAccount
                {
                    // 6. secret number
                    // 33925117546276674132447621473897413222334686876070034354053857008736343968929
                    Address = "UkcbE1dCzu28bNPAXayrdL2ghUsCc4mMDbuS2gpTV18Wmu",
                    PublicKey = "535fd4c6c22c07f36dae2ccfafb4731dabe1f0ce4c215b0f1200345998c966b78afbe9d3362ed37a662d0d3d3d41f404026a71d891645434bfd3736db5d22fd3",
                    Balance = 40000000
                },

                new IcoAccount
                {
                    // 7. secret number
                    // 15881581657373450654931468902347847231082990296681270036967598645931367828621
                    Address = "UkcE5LKJtAU32qYwUFzKa4Tm5v74ZWqB43hHQVaX17Zuoy",
                    PublicKey = "5e15ab7cb03f46898b4ff0cf6271d0da67c3807baff45df0297c15dd5d5cd002c003a76a250abc0a13ec7a149c3c929a6e1f52647cab3bbeb6098c982b4fd630",
                    Balance = 45000000
                }
,

                new IcoAccount
                {
                    // 8. secret number
                    // 33269239093815321863877378749235853369177971894417238924007173673219753657731
                    Address = "UkcUY6FMwoLjmGaipWZp6n43Dmg9ETbnQyecGaSy3XvLx7f",
                    PublicKey = "6f2e1592d2478fc81947a587e7dfd0fc91c4675f0fda07b8a17f81d42e88f81c41077bde55681acd0e698567b1dafe7e8327d798933744c559d6e8151f2b63f2",
                    Balance = 50000000
                }
,

                new IcoAccount
                {
                    // 9. secret number
                    // 7674628509672541661147609495429555806759724050925370667667199268257454082631
                    Address = "Ukc9z9Li8csur53KDjhjgmyyZQhmy9xXqE556pcK6qFH2x4",
                    PublicKey = "4e1f22f72f49a9dd55073bd42bfbc1ee7a12346346550323b69ca6cc945a6b8355239abeb621aa14936459e4aa459a4d634d964ad0846bdaeae64f228b94eb9e",
                    Balance = 55000000
                }
,

                new IcoAccount
                {
                    // 10. secret number
                    // 40087544037550555351360184926331574181465800732544946533184024036710347268977
                    Address = "UkcUisLxpBgLmMq5nXnQiq5q1Y6HcjzrXqfEJe5LdENXzvJ",
                    PublicKey = "2dc0bc95b0afcc2915c72009bd5e4560227dbf56b3df74119d082c09eb78b40d5ba7ce2a4f9aa1698131543b4eb8c7e9c72f7ad27600f51f76662ee1874c0a6e",
                    Balance = 60000000
                }





            };

            return list;

        }

    }
}
