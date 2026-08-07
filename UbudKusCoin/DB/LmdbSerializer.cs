// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using Google.Protobuf;

namespace UbudKusCoin.DB
{
    /// <summary>
    /// Serialization helpers for LMDB storage. Entries are protobuf messages
    /// so every record is encoded as a compact, schema-versioned byte array.
    /// </summary>
    internal static class LmdbSerializer
    {
        public static byte[] ToBytes(IMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return message.ToByteArray();
        }

        public static T FromBytes<T>(byte[] bytes) where T : IMessage<T>, new()
        {
            ArgumentNullException.ThrowIfNull(bytes);
            return new MessageParser<T>(() => new T()).ParseFrom(bytes);
        }

        /// <summary>
        /// Big-endian 8-byte encoding so numeric keys keep the natural
        /// ordering of positive integers inside the LMDB B+Tree.
        /// </summary>
        public static byte[] ToOrderedBytes(long value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return bytes;
        }

        public static long FromOrderedBytes(byte[] bytes, int offset = 0)
        {
            if (BitConverter.IsLittleEndian)
            {
                var trimmed = new byte[8];
                Array.Copy(bytes, offset, trimmed, 0, 8);
                Array.Reverse(trimmed);
                return BitConverter.ToInt64(trimmed, 0);
            }

            return BitConverter.ToInt64(bytes, offset);
        }
    }
}