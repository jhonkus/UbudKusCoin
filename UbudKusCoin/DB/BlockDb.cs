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
    /// <summary>
    /// Block Database to keep block persistant
    /// </summary>
    public class BlockDb
    {

        private LiteDatabase _db;
        public BlockDb(LiteDatabase db)
        {
            _db = db;
        }

        /// <summary>
        /// Add block
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public AddBlockStatus Add(Block block)
        {
            var blocks = GetAll();
            try
            {
                blocks.Insert(block);
                return new AddBlockStatus
                {
                    Status = Others.Constants.TXN_STATUS_SUCCESS,
                    Message = "block added successfully"
                };
            }
            catch
            {
                return new AddBlockStatus
                {
                    Status = Others.Constants.TXN_STATUS_FAIL,
                    Message = "Rttpt add transaction to pool"
                };
            }
        }

        /// <summary>
        /// Get First Block or Genesis block, ordered by block Height
        /// </summary>
        /// <returns></returns>
        public Block GetFirst()
        {
            var block = GetAll().FindAll().FirstOrDefault();
            return block;
        }


        /// <summary>
        /// Get Last block ordered by block Height
        /// </summary>
        /// <returns></returns>
        public Block GetLast()
        {
            var block = GetAll().FindOne(Query.All(Query.Descending));
            return block;
        }

        /// <summary>
        /// Get Block by Block Height
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get Block by block Hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get blocks with paging, page number and number of row per page
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="resultPerPage"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get blocks, starting from spesific height until 50 rows
        /// </summary>
        /// <param name="startHeight"></param>
        /// <returns></returns>
        public List<Block> GetRemains(long startHeight)
        {
            var blocks = GetAll();
            blocks.EnsureIndex(x => x.Height);
            var query = blocks.Query()
                .OrderByDescending(x => x.Height)
                .Where(x => x.Height > startHeight && x.Height <= (startHeight + 50))
                .ToList();
            return query;
        }

        /// <summary>
        /// Get last blocks 
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public List<Block> GetLasts(int num)
        {
            var blocks = GetAll();
            blocks.EnsureIndex(x => x.Height);
            var query = blocks.Query()
                .OrderByDescending(x => x.Height)
                .Limit(num).ToList();
            return query;
        }

        /// <summary>
        /// Get blocks that validate by address / validator
        /// </summary>
        /// <param name="address"></param>
        /// <param name="pageNumber"></param>
        /// <param name="resultPerPage"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get all blocks
        /// </summary>
        /// <returns></returns>
        public ILiteCollection<Block> GetAll()
        {
            var coll = _db.GetCollection<Block>(Constants.TBL_BLOCKS);
            coll.EnsureIndex(x => x.Height);
            return coll;
        }

        /// <summary>
        /// Get all hash of all blocks
        /// </summary>
        /// <returns></returns>
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