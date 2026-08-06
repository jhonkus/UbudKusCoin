// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;
using System.Linq;
using LiteDB;
using UbudKusCoin.Grpc;
using UbudKusCoin.Others;
using UbudKusCoin.Services;

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
        public bool Add(Peer peer, out string message)
        {
            message = string.Empty;
            if (peer is null)
            {
                message = "Peer is required.";
                return false;
            }

            var existingPeer = GetByAddress(peer.Address);
            if (existingPeer is null)
            {
                var currentPeers = GetAll().FindAll().ToList();
                var maxPeers = PeerAdmissionPolicy.GetMaxKnownPeers();
                if (currentPeers.Count >= maxPeers)
                {
                    var now = Others.UkcUtils.GetTime();
                    var candidateScore = PeerAdmissionPolicy.Score(peer, now);
                    var worstScore = currentPeers.Min(existing => PeerAdmissionPolicy.Score(existing, now));
                    if (candidateScore <= worstScore)
                    {
                        message = $"Peer cap of {maxPeers} reached; candidate score {candidateScore} does not exceed current floor {worstScore}.";
                        NodeTelemetry.RecordPeerAdmission(false, "cap");
                        return false;
                    }
                }

                GetAll().Insert(peer);
                message = "Peer added.";
                NodeTelemetry.RecordPeerAdmission(true, "new");
                return true;
            }

            existingPeer.IsBootstrap = peer.IsBootstrap;
            existingPeer.IsCanreach = peer.IsCanreach;
            existingPeer.LastReach = peer.LastReach;
            existingPeer.TimeStamp = peer.TimeStamp;
            GetAll().Update(existingPeer);
            message = "Peer updated.";
            NodeTelemetry.RecordPeerAdmission(true, "update");
            return true;
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
            return peers.FindAll().FirstOrDefault(peer => PeerIdentityPolicy.AreSameEndpoint(peer.Address, address));
        }
    }
}
