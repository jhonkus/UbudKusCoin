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
            Report = new ReportFacade();
            Stake = new StakeFacade();
            Account = new AccountFacade();
  
            TransactionPool = new TransactionPoolFacade();
            Console.WriteLine("Transaction pool initilize ....");

            Transaction = new TransactionFacade();
            
            Block = new BlockFacade();           

            Console.WriteLine("All facade initilize ....");
        }
    }
}