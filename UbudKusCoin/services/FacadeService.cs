using System;
using UbudKusCoin.Facade;

namespace UbudKusCoin.Services
{
    public class FacadeService
    {
        public AccountFacade Account {set; get;}
        public BlockFacade Block  {set; get;}
        public TransactionFacade Transaction {set; get;}
        public TransactionPoolFacade TransactionPool {set; get;}
        public StakeFacade Stake  {set; get;}
        public ReportFacade Report  {set; get;}

        public FacadeService()
        {
            Console.WriteLine("Facade initilize ===");
        }

        public void start()
        {
            this.Report = new ReportFacade();
            this.Stake = new StakeFacade();
            this.Account = new AccountFacade();
            this.TransactionPool = new TransactionPoolFacade();
            this.Transaction = new TransactionFacade();
            this.Block = new BlockFacade();           
        }
    }
}