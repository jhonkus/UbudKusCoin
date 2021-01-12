using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using GrpcService.Protos;

namespace DesktopWallet
{
    public static class Helper
    {
        //'Google.Protobuf.Collections.RepeatedField<grpcservice.Protos.BlockModel>' to 'System.Collections.Generic.List<grpcservice.Protos.BlockModel>'
        public static void DoShowBlockchain(Google.Protobuf.Collections.RepeatedField<BlockModel> blocks)
        {
            Console.Clear();
            Console.WriteLine("\n\n\nBlockchain Explorer");
            Console.WriteLine("Time: {0}", DateTime.Now);
            Console.WriteLine("======================");


            foreach (var block in blocks)
            {
                //Console.WriteLine("ID          :{0}", block.ID);
                Console.WriteLine("Height      : {0}", block.Height);
                Console.WriteLine("Timestamp   : {0}", block.TimeStamp.ConvertToDateTime());
                Console.WriteLine("Prev. Hash  : {0}", block.PrevHash);
                Console.WriteLine("Hash        : {0}", block.Hash);



                if (block.Height == 1)
                {
                    Console.WriteLine("Transactions : {0}", block.Transactions);
                }
                else
                {
                    var transactions = JsonConvert.DeserializeObject<List<TrxModel>>(block.Transactions);
                    Console.WriteLine("Transactions:");
                    foreach (TrxModel trx in transactions)
                    {
                        Console.WriteLine("   Timestamp   : {0}", trx.TimeStamp.ConvertToDateTime());
                        Console.WriteLine("   Sender      : {0}", trx.Sender);
                        Console.WriteLine("   Recipient   : {0}", trx.Recipient);
                        Console.WriteLine("   Amount      : {0}", trx.Amount.ToString("N", CultureInfo.InvariantCulture));
                        Console.WriteLine("   Fee         : {0}", trx.Fee.ToString("N4", CultureInfo.InvariantCulture));
                        Console.WriteLine("   - - - - - - ");

                    }
                }


                Console.WriteLine("--------------\n");

            }


        }

        public static string ConvertToDateTime(this Int64 timestamp)
        {

            DateTime myDate = new DateTime(timestamp);
            var strDate = myDate.ToString("dd MMM yyyy hh:mm:ss");

            return strDate;

        }
    }
}
