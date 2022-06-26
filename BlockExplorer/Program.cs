// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using Grpc.Net.Client;

namespace UbudKusCoin.BlockExplorer
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverAddress = "http://localhost:5002";
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress(serverAddress);
            _ = new BlockExplorer(channel);
        }
    }
}