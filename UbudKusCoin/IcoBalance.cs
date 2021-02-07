using System.Collections.Generic;

namespace UbudKusCoin
{
    public class IcoAccount
    {
        public string Address { set; get;}
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
                    // secreet nunber
                    // 11520842075416936956337166257543145030894758329506615265245623459159831684481
                    Address = "UKC_JavaPsOANbgT5anGjTg0Ih6qdC4mHgbmpF5ptjAJb0g=",
                    PublicKey = "a5185d90719f52615930a6f0249ef7e1310a90159eef573805577288db66e107f5b71cdfa607eb759c1e43a1e08375b775019a2086794c61a042a6db7ea58af4",
                    Balance = 10000
                },

                new IcoAccount
                {
                    // secret number
                    // 50097633609371174574534106620065769324210518368794492873657273912701099632384
                    Address = "UKC_mGyJe2kD3cNs4c8d/KHVe4+DSt9mwrLLqlDejXUgdzA=",
                    PublicKey = "9c8f0d364104cad2d7928e606e7927d23c181297969d95be3e273c4a1aff0f3362f4c9c8b49e5987a13da1102b555392620865eb54f14d938f180d460daeb0d3",
                    Balance = 20000
                },

                new IcoAccount
                {
                    // secret number
                    // 17444769289605527965285869029990128769824151562466867025336412934980476651053
                    Address = "UKC_ZOm+XeyKAEbIb/L41TPEzRRxwMOsZW6HE2WjdxeCFFI=",
                    PublicKey = "7b2240cc9370f6446b7df3b5b27a5da32e1d9c657fbfc9860fff8d9a9cbc250ea224536045e289db13f36561e73b6ecad88a977604d98de56623e64431ba165b",
                    Balance = 15000
                },

                new IcoAccount
                {
                    // secret number
                    // 7611318794264389102622393469331744367164041724526682955404226892006202219169
                    Address = "UKC_rMOHTqvkDCLtaoqbkgF3GmM2lewE3R2ZFYDGfq0A/fI=",
                    PublicKey = "f19681e8249493fc3b42a2e4de8dc544a136137fa27b46a6feff3be1d849e61d89e4c4b6ebf15ad5a67c7600de55bc39cb71758b96f8e9ec90f81e729b1f8d39",
                    Balance = 15000
                }

            };

            return list;

        }
    
    }
}
