// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using LiteDB;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using System.Collections.Generic;
using System.Linq;


namespace UbudKusCoin.DB
{
    public class BlockDb
    {

        private LiteDatabase _db;
        public BlockDb(LiteDatabase db)
        {
            _db = db;
        }

        public void Add(Block block)
        {
            var blocks = GetAll();
            blocks.Insert(block);
        }

        public Block GetFirst()
        {
            var block = GetAll().FindAll().FirstOrDefault();
            return block;
        }

        public Block GetLast()
        {
            var block = GetAll().FindOne(Query.All(Query.Descending));
            return block;
        }

        public Block GetByHeight(long height)
        {
            var coll = GetAll();
            var block = coll.Query().Where(x => x.Height == height).ToEnumerable();
            if (block.Any())
            {
                return block.FirstOrDefault();
            }
            return null;
        }


        public Block GetByHash(string hash)
        {
            var coll = GetAll();
            var block = coll.Query().Where(x => x.Hash == hash).ToEnumerable();
            if (block.Any())
            {
                return block.FirstOrDefault();
            }
            return null;
        }

        public List<Block> GetRange(int pageNumber, int resultPerPage)
        {
            var blocks = GetAll();
            blocks.EnsureIndex(x => x.Height);
            var query = blocks.Query()
                .OrderByDescending(x => x.Height)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }


        public List<Block> GetLasts(int num)
        {
            var blocks = GetAll();
            blocks.EnsureIndex(x => x.Height);
            var query = blocks.Query()
                .OrderByDescending(x => x.Height)
                .Limit(num).ToList();
            return query;
        }

        public IEnumerable<Block> GetByValidator(string address, int pageNumber, int resultPerPage)
        {
            var coll = GetAll();
            coll.EnsureIndex(x => x.Validator);
            var query = coll.Query()
                .OrderByDescending(x => x.Height)
                .Where(x => x.Validator == address)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            return query;
        }
        public ILiteCollection<Block> GetAll()
        {
            var coll = _db.GetCollection<Block>(Constants.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height);
            return coll;
        }

       public IList<string> GetHashList()
        {
            var blocks = GetAll();
            IList<string> hashList = new List<string>();
            foreach (var block in blocks.FindAll())
            {
                var hash = block.Hash;
                hashList.Append(hash);
            }
            return hashList;
        }
    }
}