// Created by I Putu Kusuma Negara. markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using UbudKusCoin.Services;
using UbudKusCoin.P2P;


namespace UbudKusCoin
{
    public class Program
    {
        public static void Main(string[] args)
        {

            DotNetEnv.Env.Load();
            DotNetEnv.Env.TraversePath().Load();
            var nodeaddress = DotNetEnv.Env.GetString("NODE_ADDRESS");
            var knownpeers = DotNetEnv.Env.GetString("KNOWN_PEERS");
            var passphrase = DotNetEnv.Env.GetString("NODE_PASSPHRASE");
            // Console.WriteLine(knownpeers);
            Console.WriteLine(passphrase);


            ServicePool.Add(
                new WalletService(passphrase),
                new DbService("uksc"),
                new FacadeService(),
                new MintingService(),
                new P2PService(nodeaddress),
                new EventService()
            );
            ServicePool.Start();


            // grpc
            IHost host = CreateHostBuilder(args).Build();
            // host.Services.UseScheduler(scheduler =>
            // {
            //     scheduler.Schedule<SomeJobs>()
            //         .EveryThirtySeconds();
            // });
            host.Run();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
           .UseSystemd()
          .ConfigureWebHostDefaults(webBuilder =>
          {
              webBuilder.ConfigureKestrel(options =>
              {
                  options.ListenAnyIP(5001, listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2); //webapi
                  options.ListenAnyIP(5002, listenOptions => listenOptions.Protocols = HttpProtocols.Http2); //grpc
              });

              // start
              webBuilder.UseStartup<Startup>();
          });


    }
}
