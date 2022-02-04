using System.Collections.Generic;
namespace UbudKusCoin.Models
{
    class Inventory
    {
        public string AddrFrom { set; get; }
        public string Type { set; get; }
        public IList<string> Items { set; get; }
    }
}