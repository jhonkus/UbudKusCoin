using System.Runtime.InteropServices;
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

            // Initilize db
            DbAccess.Initialize();

            /**
             * remove all record in all table
             * uncomment this
            **/
            // DbAccess.ClearDB();



            // Make blockchain
            _ = new Blockchain();
            IHost host = CreateHostBuilder(args).Build();
            host.Services.UseScheduler(scheduler => {
                // Easy peasy 👇
                scheduler
                    .Schedule<BlockJob>()
                    .EveryTenSeconds();
            });
            host.Run();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
          .ConfigureWebHostDefaults(webBuilder =>
          {
              
              if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
              {
                  
                  webBuilder.ConfigureKestrel(options =>
                  {
                      // Setup a HTTP/2 endpoint without TLS.
                      options.ListenLocalhost(8080, o => o.Protocols =
                          HttpProtocols.Http2);
                  });
              }
              webBuilder.UseStartup<Startup>();

          });
    }
}
