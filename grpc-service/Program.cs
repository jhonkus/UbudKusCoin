    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
using DB;
using Main;
using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.Extensions.Hosting;

    namespace grpcservice
    {
        public class Program
        {
            public static void Main(string[] args)
            {

            // Initilize db
            DbAccess.Initialize();

            /**
             * remove all record in all table
             * uncomment this if you want
            **/
            // DbAccess.ClearDB();

            // Make blockchain
            _ = new Blockchain();


            CreateHostBuilder(args).Build().Run();
            }

            // Additional configuration is required to successfully run gRPC on macOS.
            // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
            //public static IHostBuilder CreateHostBuilder(string[] args) =>
            //    Host.CreateDefaultBuilder(args)
            //        .ConfigureWebHostDefaults(webBuilder =>
            //        {
            //            webBuilder.UseStartup<Startup>();
            //        }


            //        );


            public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                  {
                      webBuilder.ConfigureKestrel(options =>
                      {
                                // Setup a HTTP/2 endpoint without TLS.
                                options.ListenLocalhost(5002, o => o.Protocols =
                                    HttpProtocols.Http2);
                      });
                  }
                  webBuilder.UseStartup<Startup>();
              });



        }
    }
