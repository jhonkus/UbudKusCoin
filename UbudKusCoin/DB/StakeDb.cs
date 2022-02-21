// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using LiteDB;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;

namespace UbudKusCoin.DB
{

    /// <summary>
    /// Stake databases
    /// </summary>
    public class StakeDb
    {
        private readonly LiteDatabase _db;

        public StakeDb(LiteDatabase db)
        {
            this._db = db;
        }

        /// <summary>
        /// add or update stake
        /// </summary>
        /// <param name="stake"></param>
        public void AddOrUpdate(Stake stake)
        {
            var locStake = GetByAddress(stake.Address);
            if (locStake is null)
            {
                GetAll().Insert(stake);
            }
            GetAll().Update(stake);
        }

        /// <summary>
        /// Delete all stake
        /// </summary>
        public void DeleteAll()
        {
            var stakers = GetAll();
            if (stakers is null || stakers.Count() < 1)
            {
                return;
            }
            stakers.DeleteAll();
        }


        /// <summary>
        /// Get maximum stake, base on amount
        /// </summary>
        /// <returns></returns>
        public Stake GetMax()
        {
            var stakes = GetAll();
            if (stakes is null || stakes.Count() < 1)
            {
                return null;
            }

            stakes.EnsureIndex(x => x.Amount);
            var query = stakes.Query()
                .OrderByDescending(x => x.Amount);
            return query.FirstOrDefault();
        }

        /// <summary>
        /// Get stake by address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public Stake GetByAddress(string address)
        {
            var stakes = GetAll();
            if (stakes is null)
            {
                return null;
            }
            stakes.EnsureIndex(x => x.Address);
            var stake = stakes.FindOne(x => x.Address == address);
            return stake;
        }


        /// <summary>
        /// Get all stake
        /// </summary>
        /// <returns></returns>
        public ILiteCollection<Stake> GetAll()
        {
            var stakes = this._db.GetCollection<Stake>(Constants.TBL_STAKES);
            stakes.EnsureIndex(x => x.Amount);
            return stakes;
        }

    }
}