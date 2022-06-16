// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using LiteDB;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;

namespace UbudKusCoin.DB
{
    /// <summary>
    /// Peer database, for add, update list of peers
    /// </summary>
    public class PeerDb
    {
        private readonly LiteDatabase _db;

        public PeerDb(LiteDatabase db)
        {
            _db = db;
        }

        /// <summary>
        /// Add a peer
        /// </summary>
        public void Add(Peer peer)
        {
            var existingPeer = GetByAddress(peer.Address);
            if (existingPeer is null)
            {
                GetAll().Insert(peer);
            }
        }

        /// <summary>
        /// Get list of peer, page number and number of row per page
        /// </summary>
        public List<Peer> GetRange(int pageNumber, int resultPerPage)
        {
            var peers = GetAll();
            
            peers.EnsureIndex(x => x.LastReach);
            
            var query = peers.Query()
                .OrderByDescending(x => x.LastReach)
                .Offset((pageNumber - 1) * resultPerPage)
                .Limit(resultPerPage).ToList();
            
            return query;
        }


        /// <summary>
        /// Get all peer
        /// </summary>
        public ILiteCollection<Peer> GetAll()
        {
            var peers = _db.GetCollection<Peer>(Constants.TBL_PEERS);
            
            peers.EnsureIndex(x => x.LastReach);
            
            return peers;
        }

        /// <summary>
        /// Get peer by network address/IP
        /// </summary>
        public Peer GetByAddress(string address)
        {
            var peers = GetAll();
            if (peers is null)
            {
                return null;
            }

            peers.EnsureIndex(x => x.Address);
            
            return peers.FindOne(x => x.Address == address);
        }
    }
}