// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
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
using Microsoft.Extensions.Logging;

namespace UbudKusCoin
{
    public class Program
    {
        public static void Main(string[] args)
        {

            DotNetEnv.Env.Load();
            DotNetEnv.Env.TraversePath().Load();

            ServicePool.Add(
                new WalletService(),
                new DbService(),
                new FacadeService(),
                new MintingService(),
                new P2PService()
            );
            ServicePool.Start();


            // grpc
            IHost host = CreateHostBuilder(args).Build();
            host.Run();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
           .UseSystemd()
          .ConfigureWebHostDefaults(webBuilder =>
          {
              webBuilder.ConfigureKestrel(options =>
              {

                  var GRPC_WEB_PORT = DotNetEnv.Env.GetInt("GRPC_WEB_PORT");
                  var GRPC_PORT = DotNetEnv.Env.GetInt("GRPC_PORT");

                  options.ListenAnyIP(GRPC_WEB_PORT, listenOptions => listenOptions.Protocols = HttpProtocols.Http1AndHttp2); //webapi
                  options.ListenAnyIP(GRPC_PORT, listenOptions => listenOptions.Protocols = HttpProtocols.Http2); //grpc
              });

              // start
              webBuilder.UseStartup<Startup>()
            //   .ConfigureLogging(loggingBuilder => loggingBuilder.ClearProviders());

            .ConfigureLogging((Action<WebHostBuilderContext, ILoggingBuilder>)((hostingContext, logging) =>
            {
                // logging.AddConfiguration((IConfiguration)hostingContext.Configuration.GetSection("Logging"));
                // logging.AddConsole();
                // logging.AddDebug();
                // logging.AddEventSourceLogger();
                logging.ClearProviders();

            }));

              //===
          });


    }
}
