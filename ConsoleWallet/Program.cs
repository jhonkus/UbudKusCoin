// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using Grpc.Net.Client;

namespace UbudKusCoin.ConsoleWallet
{
    class Program
    {
        static void Main()
        {
            DotNetEnv.Env.Load();
            DotNetEnv.Env.TraversePath().Load();

            var NODE_ADDRESS = DotNetEnv.Env.GetString("NODE_ADDRESS");
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            GrpcChannel channel = GrpcChannel.ForAddress(NODE_ADDRESS);
            _ = new ConsoleAppWallet(channel);
        }

    }


}
