// Created by I Putu Kusuma Negara
// markbrain2013[at]gmail.com
// 
// Ubudkuscoin is free software distributed under the MIT software license,
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using UbudKusCoin.Grpc;
using UbudKusCoin.Services;

namespace UbudKusCoin
{
    public class Startup
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(options =>
            {
                options.MaxReceiveMessageSize = 1 * 1024 * 1024;
                options.MaxSendMessageSize = 4 * 1024 * 1024;
            });
            services.AddHostedService<AbciSocketServer>();
            services.AddHostedService<ConsensusReadinessMonitor>();
            var corsOrigins = DotNetEnv.Env.GetString("API_CORS_ORIGINS", string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            services.AddCors(o => o.AddPolicy("ApiCors", builder =>
            {
                builder.WithExposedHeaders(
                    "Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
                if (corsOrigins.Length > 0)
                {
                    builder.WithOrigins(corsOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
            }));
        }

        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseRouting();
            app.UseMiddleware<ApiRateLimitingMiddleware>();
            // add support grpc call from web app, Must be added between UseRouting and UseEndpoints
            app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
            app.UseCors();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<AccountServiceImpl>().RequireCors("ApiCors");
                endpoints.MapGrpcService<BlockServiceImpl>().RequireCors("ApiCors");
                endpoints.MapGrpcService<CanonicalBlockServiceImpl>().RequireCors("ApiCors");
                endpoints.MapGrpcService<PeerServiceImpl>().RequireCors("ApiCors");
                endpoints.MapGrpcService<StakeServiceImpl>().RequireCors("ApiCors");
                endpoints.MapGrpcService<TransactionServiceImpl>().RequireCors("ApiCors");
                endpoints.MapGrpcService<AbciServiceImpl>();
                endpoints.MapGrpcService<ReadModelServiceImpl>().RequireCors("ApiCors");
                endpoints.MapGet("/health/consensus", async context =>
                {
                    var status = await ServicePool.ConsensusEngine.GetStatusAsync(context.RequestAborted);
                    context.Response.StatusCode = status.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(status, context.RequestAborted);
                });
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync(
                        "Communication with gRPC endpoints" +
                        " must be made through a gRPC client.");
                });
            });
        }
    }
}
