﻿using System.Runtime.InteropServices;
using Coravel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using UbudKusCoin.Sceduler;

namespace Main
{
    public class Program
    {
        public static void Main(string[] args)
        {

            // blockchain
            _ = new Blockchain();

            // grpc
            IHost host = CreateHostBuilder(args).Build();
            host.Services.UseScheduler(scheduler =>
            {
                scheduler.Schedule<BlockJob>()
                    .EveryMinute();
            });
            host.Run();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
           .UseSystemd()
          .ConfigureWebHostDefaults(webBuilder =>
          {

              // if macos
              //   if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
              //   {

              //       webBuilder.ConfigureKestrel(options =>
              //       {
              //           // Setup a HTTP/2 endpoint without TLS.
              //           options.ListenLocalhost(5002, o => o.Protocols =
              //               HttpProtocols.Http2);
              //       });
              //   }

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
