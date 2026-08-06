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
using Microsoft.Extensions.Logging;
using UbudKusCoin.Services;
using UbudKusCoin.P2P;

namespace UbudKusCoin
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
                        var tlsCertificatePath = DotNetEnv.Env.GetString("API_TLS_CERT_PATH", string.Empty);
                        var tlsCertificatePassword = DotNetEnv.Env.GetString("API_TLS_CERT_PASSWORD", string.Empty);

                        options.ListenAnyIP(GRPC_WEB_PORT, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                            if (!string.IsNullOrWhiteSpace(tlsCertificatePath))
                            {
                                listenOptions.UseHttps(tlsCertificatePath, tlsCertificatePassword);
                            }
                        }); //webapi
                        options.ListenAnyIP(GRPC_PORT, listenOptions => listenOptions.Protocols = HttpProtocols.Http2); //grpc
                    });

                    // start
                    webBuilder.UseStartup<Startup>()
                        .ConfigureLogging((Action<WebHostBuilderContext, ILoggingBuilder>)((hostingContext, logging) =>
                        {
                            logging.ClearProviders();
                            logging.AddSimpleConsole(options =>
                            {
                                options.SingleLine = true;
                                options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                                options.IncludeScopes = true;
                            });
                            logging.SetMinimumLevel(LogLevel.Information);
                        }));
                    //===
                });
    }
}
