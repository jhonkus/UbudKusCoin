using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using UbudKusCoin.Services;
using UbudKusCoin.DB;


namespace UbudKusCoin
{
    public class Program
    {
        public static void Main(string[] args)
        {


            ServicePool.Add(
                new DbService("uksc"),
                new FacadeService(),
                new MintingService()
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
