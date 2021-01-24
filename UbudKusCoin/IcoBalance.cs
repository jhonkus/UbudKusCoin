using System;
using System.Collections.Generic;

namespace UbudKusCoin
{
    public class IcoAccount
    {
        public string Address { set; get;}
        public double Balance { set; get; }
    }

    public static class IcoBalance
    {


        public static List<IcoAccount> GetIcoAccounts()
        {

            var list = new List<IcoAccount>
            {
                new IcoAccount
                {
                    //secreet nunber
                    // 11520842075416936956337166257543145030894758329506615265245623459159831684481
                    Address = "UKC_mGyJe2kD3cNs4c8d/KHVe4+DSt9mwrLLqlDejXUgdzA=",
                    Balance = 4000000
                },

                new IcoAccount
                {
                    //secret number
                    //50097633609371174574534106620065769324210518368794492873657273912701099632384
                    Address = "UKC_JavaPsOANbgT5anGjTg0Ih6qdC4mHgbmpF5ptjAJb0g=",
                    Balance = 2000000
                }

            };

            return list;

        }
    
    }
}
