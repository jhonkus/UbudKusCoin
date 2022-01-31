using UbudKusCoin.DB;

namespace UbudKusCoin.Services
{
    public static class ServicePool
    {

        public static MintingService MintingService { set; get; }

        public static DbService DbService { set; get; }

        public static FacadeService FacadeService { set; get; }

        public static EventService EventService { set; get; }



        public static void Add(
                  DbService db,
                  FacadeService facade,
                  MintingService minter,
                  EventService evtserv)
        {
            DbService = db;
            FacadeService = facade;
            MintingService = minter;
            EventService = evtserv;
        }
        public static void Start()
        {
            DbService.Start();
            FacadeService.start();
            MintingService.Start();
            EventService.Start();
        }

        public static void Stop()
        {
            //stop when application exit
            DbService.Stop();
        }


    }
}
