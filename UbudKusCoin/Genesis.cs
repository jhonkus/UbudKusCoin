using System.Collections.Generic;

namespace UbudKusCoin
{
    public class GenesisAccount
    {
        public string Address { set; get;}
        public string PublicKey { set; get; }
        public double Balance { set; get; }
    }

    public static class Genesis
    {


        public static List<GenesisAccount> GetAll()
        {

            var list = new List<GenesisAccount>
            {

                new GenesisAccount
                {
                    // secreet nunber
                    // 2
                    // UKC_QPQY9wHP0jxi/0c/YRlch2Uk5ur/T8lcOaawqyoe66o=
                    Address = "Ukcn4Yy7CMVxNGRqRM6s1p88fCkym3P4q4FeSXgD4s81J6P",
                    PublicKey = "b3295dd867da1117b56edf09049daa93cadc2d83b8b17f4f004e8eaef818ae1aae3bd96dfb25eccc6d3227659b1778191f2dfb42a6a5226d054d73d7dd6f9970",
                    Balance = 5000000000
                },

                new GenesisAccount
                {
                    // secret number
                    // 46084958288583143460506686453126733781485555622618603681695930748076603235149
                    // UKC_rcyChuW7cQcIVoKi1LfSXKfCxZBHysTwyPm88ZsN0BM=
                    Address = "UkcU6SQGuPqrDWgD8AY5oRD7PRxVQV5LWrbf6vkrTtuDtBc",
                    PublicKey = "23b3f7b8806d30d765ecef49035249ef96b5f3fab2e6ed5c196c55d1fec9d55e6c04cb21ff078f8c06ddeb2b9a5d37b4396cbb0e01db8d519a25f1816a6fd803",
                    Balance = 10000000000
                }

            };

            return list;

        }
    
    }
}
