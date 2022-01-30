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
            Console.WriteLine("Minter started started");
        }


        public void Stop()
        {
            cancelTask.Cancel();
            Console.WriteLine("Minter Stoped");
        }

        public void DoGenerateBlock()
        {
            while (true)
            {
                var startTime = DateTime.UtcNow.Second;
                ServicePool.FacadeService.Block.CreateNew();
                var endTime = DateTime.UtcNow.Second;
                var remainTime = Constants.BLOCK_GENERATION_INTERVAL - (endTime - startTime);
                Console.WriteLine("remain Time: {0}", remainTime);

                Thread.Sleep(remainTime < 0 ? 0 : remainTime * 1000);
            }
        }

    }
}