using System;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
namespace UbudKusCoin.Services
{
    public delegate void EventHandler(object sender, EventArgs e);
    public class EventService
    {

        public EventService() { }

        public event EventHandler<Transaction> EventTransactionCreated;

        public event EventHandler<Block> EventBlockCreated;
        private void ListenEvent()
        {
            EventBlockCreated += Evt_EventBlockCreated;
            EventTransactionCreated += Evt_EventTransactionCreated;
        }

        public void Start()
        {
            ListenEvent();
        }

        public virtual void OnEventBlockCreated(Block arg)
        {
            EventBlockCreated?.Invoke(this, arg);
        }

        protected virtual void OnEventTransactionCreated(Transaction arg)
        {
            EventTransactionCreated?.Invoke(this, arg);
        }


        void Evt_EventBlockCreated(object sender, Block block)
        {
            //if (sender == null)
            //{
            //    Console.WriteLine("No New Block Added.");
            //    return;
            //}
            Console.WriteLine("... print block ...");
            Utils.PrintBlock(block);

            // build report   
            // ServicePool.FacadeService.Report.BuildReport();
            // BroadCast("A New Block from: " + _port);
        }

        void Evt_EventTransactionCreated(object sender, Transaction txn)
        {
            Console.WriteLine("... Transaction created ...{0}", txn);

            // build report   
            // ServicePool.FacadeService.Report.BuildReport();
            // BroadCast("A New Block from: " + _port);
        }

    }


}