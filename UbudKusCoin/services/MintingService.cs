using System;
using System.Threading;
using System.Threading.Tasks;
using UbudKusCoin.Others;

namespace UbudKusCoin.Services
{
    public class MintingService
    {

        private CancellationTokenSource cancelTask;

        public MintingService()
        {
            Console.WriteLine("Minting Service started...");
        }

        public void Start()
        {
            cancelTask = new CancellationTokenSource();
            Task.Run(() => DoGenerateBlock(), cancelTask.Token);
            Console.WriteLine("Forger started");
        }


        public void Stop()
        {
            cancelTask.Cancel();
            Console.WriteLine("Forger Stoped");
        }

        public void DoGenerateBlock()
        {
            while (true)
            {
                var startTime = DateTime.UtcNow.Second;
                //Int32 startTime = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                ServicePool.FacadeService.Block.CreateNew();

                var _random = new Random();
                int num = _random.Next(3000, 20000);
                Console.WriteLine("num: {0}", num);
                
                Thread.Sleep(num);


                var endTime = DateTime.UtcNow.Second;

                var remainTime = Constants.BLOCK_GENERATION_INTERVAL - (endTime - startTime);

                Console.WriteLine("remain Time: {0}", remainTime);
                Thread.Sleep(remainTime < 0 ? 0 : remainTime * 1000);
            }
        }

    }
}