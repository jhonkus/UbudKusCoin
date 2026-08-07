// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;

namespace UbudKusCoin.DB
{
    /// <summary>
    /// Peer database, for add, update list of peers. Peers are keyed by their
    /// normalized network address.
    /// </summary>
    public class PeerDb
    {
        private readonly LmdbStore _store;

        public PeerDb(LmdbStore store)
        {
            _store = store;
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

            if (!PeerIdentityPolicy.TryNormalizeEndpoint(peer.Address, out var normalized, out message))
            {
                return false;
            }

            var existingPeer = GetByAddress(peer.Address);
            if (existingPeer is null)
            {
                var currentPeers = GetAll();
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

                _store.Put(Key(peer.Address), LmdbSerializer.ToBytes(peer));
                message = "Peer added.";
                NodeTelemetry.RecordPeerAdmission(true, "new");
                return true;
            }

            existingPeer.IsBootstrap = peer.IsBootstrap;
            existingPeer.IsCanreach = peer.IsCanreach;
            existingPeer.LastReach = peer.LastReach;
            existingPeer.TimeStamp = peer.TimeStamp;
            _store.Put(Key(existingPeer.Address), LmdbSerializer.ToBytes(existingPeer));
            message = "Peer updated.";
            NodeTelemetry.RecordPeerAdmission(true, "update");
            return true;
        }

        /// <summary>
        /// Get list of peer, page number and number of row per page
        /// </summary>
        public List<Peer> GetRange(int pageNumber, int resultPerPage)
        {
            return GetAll()
                .OrderByDescending(x => x.LastReach)
                .Skip((pageNumber - 1) * resultPerPage)
                .Take(resultPerPage)
                .ToList();
        }

        /// <summary>
        /// Get all peer
        /// </summary>
        public List<Peer> GetAll()
        {
            return _store.Scan(Array.Empty<byte>())
                .Select(pair => LmdbSerializer.FromBytes<Peer>(pair.Value))
                .ToList();
        }

        /// <summary>
        /// Get peer by network address/IP
        /// </summary>
        public Peer GetByAddress(string address)
        {
            if (_store.TryGet(Key(address), out var bytes))
            {
                return LmdbSerializer.FromBytes<Peer>(bytes);
            }

            // Backward-compatible scan for records stored with a previously
            // unnormalized key.
            return GetAll().FirstOrDefault(peer => PeerIdentityPolicy.AreSameEndpoint(peer.Address, address));
        }

        private static byte[] Key(string address)
        {
            if (PeerIdentityPolicy.TryNormalizeEndpoint(address, out var normalized, out _))
            {
                return Encoding.UTF8.GetBytes(normalized);
            }

            return Encoding.UTF8.GetBytes(address ?? string.Empty);
        }
    }
}