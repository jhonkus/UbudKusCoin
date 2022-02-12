// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
  
            Utils.PrintBlock(block);
            // TODO BroadCast("A New Block Created: " + _port);
        }

        void Evt_EventTransactionCreated(object sender, Transaction txn)
        {
            Console.WriteLine("... Transaction created ...{0}", txn);
            // TODO BroadCast("A New Transaction created");
        }

    }


}