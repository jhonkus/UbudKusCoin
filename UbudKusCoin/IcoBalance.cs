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

            // List initial Account
            //wife sense can lava lucky same gate wear laundry rocket achieve taste
            //1APyMkWk4ftGNhq6AA3qifzZbmpYDU63LL

            //scout join easily tilt will square found venture obscure switch fuel glue
            //162wTk4uZ2MpPWgqFWkMHE5KjkhSxhU4BG

            //visit calm priority grass person paddle spike old thank online nose release
            //1P2tQJ5DymKdEWorZtc6nPMh8Nw98ch7W8

            //van comic opinion aisle topic holiday online round bus bamboo service seat
            //1Ab3hiGuyMNNyKPnvTYmq4DQajgaYS5WVL

            var list = new List<IcoAccount>
            {
                new IcoAccount
                {
                    Address = "UKC_mGyJe2kD3cNs4c8d/KHVe4+DSt9mwrLLqlDejXUgdzA=",
                    Balance = 4000000
                },

                new IcoAccount
                {
                    Address = "UKC_JavaPsOANbgT5anGjTg0Ih6qdC4mHgbmpF5ptjAJb0g=",
                    Balance = 2000000
                }


            };

            return list;

        }
    
    }
}
