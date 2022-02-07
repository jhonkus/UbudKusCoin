// Created by I Putu Kusuma Negara. markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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