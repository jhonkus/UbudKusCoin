// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using LiteDB;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;

namespace UbudKusCoin.DB
{
    public class StakeDb
    {
        private readonly LiteDatabase _db;

        public StakeDb(LiteDatabase db)
        {
            this._db = db;
        }


        public List<Stake> GetRange(int pageNumber, int resultPerPage)
        {
            var stakes = GetAll();
            stakes.EnsureIndex(x => x.TimeStamp);
            var query = stakes.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }

        public List<Stake> GetRangeByAddress(string address, int pageNumber, int resultPerPage)
        {
            var stakes = GetAll();
            stakes.EnsureIndex(x => x.TimeStamp);
            var query = stakes.Query()
                .OrderByDescending(x => x.TimeStamp)
                .Where(x => x.Address == address)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }


        public Stake GetByID(int id)
        {
            return GetAll().FindById(id);
        }

        public ILiteCollection<Stake> GetAll()
        {
            var stakes = this._db.GetCollection<Stake>(Constants.TBL_STAKES);
            stakes.EnsureIndex(x => x.TimeStamp);
            return stakes;
        }
        public void Add(Stake stake)
        {
            GetAll().Insert(stake);
        }

        public List<Stake> StakerList { get; set; }

        public double GetStake(string address)
        {
            double balance = 0;
            var collection = GetAll();
            var stakes = collection.Find(x => x.Address == address);
            foreach (var stake in stakes)
            {
                balance += stake.Amount;
            }
            return balance;
        }

        public double GetBalance(string publicKey)
        {
            var stakes = GetAll();
            var stake = stakes.FindOne(x => x.PubKey == publicKey);
            if (stake == null)
            {
                return 0;
            }
            else
            {
                return stake.Amount;
            }
        }

        public Stake GetValidator()
        {
            var stakes = GetAll();
            var numOfStakes = stakes.Count();
            var random = new Random();
            int number = random.Next(0, numOfStakes);
            return GetByID(number);
        }
    }
}