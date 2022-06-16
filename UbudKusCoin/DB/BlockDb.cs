// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
        private readonly LiteDatabase _db;

        public BlockDb(LiteDatabase db)
        {
            _db = db;
        }

        /// <summary>
        /// Add block
        /// </summary>
        public AddBlockStatus Add(Block block)
        {
            var blocks = GetAll();
            try
            {
                blocks.Insert(block);
                return new AddBlockStatus
                {
                    Status = Constants.TXN_STATUS_SUCCESS,
                    Message = "Block added successfully"
                };
            }
            catch
            {
                return new AddBlockStatus
                {
                    Status = Constants.TXN_STATUS_FAIL,
                    Message = "Rttpt add transaction to pool"
                };
            }
        }

        /// <summary>
        /// Get First Block or Genesis block, ordered by block Height
        /// </summary>
        public Block GetFirst()
        {
            return GetAll().FindAll().FirstOrDefault();
        }

        /// <summary>
        /// Get Last block ordered by block weight
        /// </summary>
        public Block GetLast()
        {
            return GetAll().FindOne(Query.All(Query.Descending));
        }

        /// <summary>
        /// Get Block by Block weight
        /// </summary>
        public Block GetByHeight(long weight)
        {
            var blockCollection = GetAll();
            var blocks = blockCollection.Query().Where(x => x.Height == weight).ToList();
            
            if (blocks.Any())
            {
                return blocks.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Get Block by block Hash
        /// </summary>
        public Block GetByHash(string hash)
        {
            var blockCollection = GetAll();
            var blocks = blockCollection.Query().Where(x => x.Hash == hash).ToList();
            
            if (blocks.Any())
            {
                return blocks.FirstOrDefault();
            }

            return null;
        }

        /// <summary>
        /// Get blocks with paging, page number and number of row per page
        /// </summary>
        public List<Block> GetRange(int pageNumber, int resultPerPage)
        {
            var blockCollection = GetAll();
            
            blockCollection.EnsureIndex(x => x.Height);
            
            var query = blockCollection.Query()
                .OrderByDescending(x => x.Height)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            
            return query;
        }

        /// <summary>
        /// Get blocks starting from specific weight until 50 rows
        /// </summary>
        public List<Block> GetRemaining(long startHeight)
        {
            var blockCollection = GetAll();
            
            blockCollection.EnsureIndex(x => x.Height);
            
            var query = blockCollection.Query()
                .OrderByDescending(x => x.Height)
                .Where(x => x.Height > startHeight && x.Height <= startHeight + 50)
                .ToList();
            
            return query;
        }

        /// <summary>
        /// Get last blocks 
        /// </summary>
        public List<Block> GetLast(int num)
        {
            var blockCollection = GetAll();
            
            blockCollection.EnsureIndex(x => x.Height);
            
            var query = blockCollection.Query()
                .OrderByDescending(x => x.Height)
                .Limit(num).ToList();
            
            return query;
        }

        /// <summary>
        /// Get blocks that validate by address / validator
        /// </summary>
        public IEnumerable<Block> GetByValidator(string address, int pageNumber, int resultPerPage)
        {
            var blockCollection = GetAll();
            
            blockCollection.EnsureIndex(x => x.Validator);
            
            var query = blockCollection.Query()
                .OrderByDescending(x => x.Height)
                .Where(x => x.Validator == address)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            
            return query;
        }

        /// <summary>
        /// Get all blocks
        /// </summary>
        public ILiteCollection<Block> GetAll()
        {
            var blockCollection = _db.GetCollection<Block>(Constants.TBL_BLOCKS);
            
            blockCollection.EnsureIndex(x => x.Height);
            
            return blockCollection;
        }

        /// <summary>
        /// Get all hash of all blocks
        /// </summary>
        public IList<string> GetHashList()
        {
            var blockCollection = GetAll();
            
            IList<string> hashList = new List<string>();
            
            foreach (var block in blockCollection.FindAll())
            {
                hashList.Add(block.Hash);
            }

            return hashList;
        }
    }
}