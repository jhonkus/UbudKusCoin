// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;

namespace UbudKusCoin.DB
{
    /// <summary>
    /// Block Database to keep block persistent. Blocks are keyed by their
    /// height (big-endian 8-byte) so iteration follows chain order.
    /// </summary>
    public class BlockDb
    {
        private readonly LmdbStore _store;

        public BlockDb(LmdbStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Add block
        /// </summary>
        public AddBlockStatus Add(Block block)
        {
            try
            {
                _store.Put(LmdbSerializer.ToOrderedBytes(block.Height), LmdbSerializer.ToBytes(block));
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
                    Message = "Failed to add block"
                };
            }
        }

        public bool RemoveByHash(string hash)
        {
            var block = GetByHash(hash);
            if (block is null)
            {
                return false;
            }

            return _store.Delete(LmdbSerializer.ToOrderedBytes(block.Height));
        }

        /// <summary>
        /// Get First Block or Genesis block, ordered by block Height
        /// </summary>
        public Block GetFirst()
        {
            return GetAll().FirstOrDefault();
        }

        /// <summary>
        /// Get Last block ordered by block height
        /// </summary>
        public Block GetLast()
        {
            return GetAll().LastOrDefault();
        }

        /// <summary>
        /// Get Block by Block height
        /// </summary>
        public Block GetByHeight(long height)
        {
            if (_store.TryGet(LmdbSerializer.ToOrderedBytes(height), out var bytes))
            {
                return LmdbSerializer.FromBytes<Block>(bytes);
            }

            return null;
        }

        /// <summary>
        /// Get Block by block Hash
        /// </summary>
        public Block GetByHash(string hash)
        {
            return GetAll().FirstOrDefault(x => x.Hash == hash);
        }

        /// <summary>
        /// Get blocks with paging, page number and number of row per page
        /// </summary>
        public List<Block> GetRange(int pageNumber, int resultPerPage)
        {
            return GetAll()
                .OrderByDescending(x => x.Height)
                .Skip((pageNumber - 1) * resultPerPage)
                .Take(resultPerPage)
                .ToList();
        }

        /// <summary>
        /// Get blocks starting from specific height until 50 rows
        /// </summary>
        public List<Block> GetRemaining(long startHeight)
        {
            return GetAll()
                .Where(x => x.Height > startHeight && x.Height <= startHeight + 50)
                .OrderByDescending(x => x.Height)
                .ToList();
        }

        /// <summary>
        /// Get last blocks
        /// </summary>
        public List<Block> GetLast(int num)
        {
            return GetAll()
                .OrderByDescending(x => x.Height)
                .Take(num)
                .ToList();
        }

        /// <summary>
        /// Get blocks that validate by address / validator
        /// </summary>
        public IEnumerable<Block> GetByValidator(string address, int pageNumber, int resultPerPage)
        {
            return GetAll()
                .Where(x => x.Validator == address)
                .OrderByDescending(x => x.Height)
                .Skip((pageNumber - 1) * resultPerPage)
                .Take(resultPerPage)
                .ToList();
        }

        /// <summary>
        /// Get all blocks
        /// </summary>
        public List<Block> GetAll()
        {
            return _store.Scan(Array.Empty<byte>())
                .Select(pair => LmdbSerializer.FromBytes<Block>(pair.Value))
                .ToList();
        }

        /// <summary>
        /// Get all hash of all blocks
        /// </summary>
        public IList<string> GetHashList()
        {
            return GetAll().Select(x => x.Hash).ToList();
        }
    }
}