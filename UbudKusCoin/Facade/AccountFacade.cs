using System;
using System.Collections.Generic;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;

namespace UbudKusCoin.Facade
{

    public class AccountFacade
    {

        public AccountFacade()
        {
            Console.WriteLine("Account initilize ....");
        }

        public List<Account> GetGenesis()
        {
            var timestamp = Utils.GetTime();
            var list = new List<Account> {
                new Account{
                    // secreet nunber
                    // 21781280861377830129150550553571098979074587004064115989578006651854952332712
                    Address = "UkcDrMshfqdHrfckb2SLoSCoG8Lp6MBdrkZ2T83FivTpWC8",
                    PubKey = "cb7dfcd7bc043b39e0f8f48950a51388a63ff075e4e501140fbf40246a8626f7ea09e8d96bfd933ce1cae613788bfd5ff151bc80ae84feb0329ebb88a468156f",
                    Balance = 2000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // secreet nunber
                    // 12236578000846032767994806805824689205819985942907379847526216303818570266507
                    Address = "UkcDw4nSzSc6zBxjWnqpEDg5AwaooSbT469QUh7DN6qmQht",
                    PubKey = "02738ecd788b54b8e1262eb9deeffb47f684442c091a759b138b9c50eb46073b5a58986b2c9cc70a304fd0e9938a836a28cfc89d47c5bebc0582f8550385c8af",
                    Balance = 3000000000,
                    TxnCount = 2,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // secreet nunber
                    // 46804102943937360112040874256984951134177138107532596818275297470849863615297
                    Address = "UkcsW98Qn8nv89JDYqYTcoPNygwT4KBtnd7xHSkWGks2J48",
                    PubKey = "39e15285feddb6687f31a7ee1a8d396736a1e7104957aebd7db5a52189aaf10809c7eb6981be0d16ae9369768cd2fc0732b55c9547be595ff8121c06394c0a96",
                    Balance = 4000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // secreet nunber
                    // 31888415667462342300498151145899343418960075407939127462687665800618862359710
                    Address = "UkcUpZbKQx28vF7snZ3bmftb7ht8KnLSC5nNBYj9didNQ6z",
                    PubKey = "e63617a1e2a2c30adef1672b3e4f9c23fd80af36e65f8600bbaa6ec4737f69177496f53f07651609de424b8a51ae855b487deaa54009de6960d753d57d5fe09d",
                    Balance = 5000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                },

                new Account
                {
                    // secret number
                    // 53957268829728400022200509029430095589656640993872744068590938611985184147408
                    Address = "UkcDEfU9gGnm9tGjmFtXRjirf2LuohU5CzjWunEkPNbUcFW",
                    PubKey = "ec2a5cb374d9b3488955697415725ac4b409037b619394c116cbdf14cf8c1f6b7bb3c8a393bd0ba3132105c7f03e51d6d64a96f1f8bf3dc47ee7d38f524fc1ef",
                    Balance = 10000000000,
                    TxnCount = 1,
                    Created = timestamp,
                    Updated = timestamp
                }
            };
            return list;
        }
    }

}