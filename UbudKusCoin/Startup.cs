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
using System.Globalization;
using System.Linq;
using UbudKusCoin.Core.Types;
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
                    NodeTelemetry.RecordReadinessCheck(true, "consensus-endpoint");
                    var status = await ServicePool.ConsensusEngine.GetStatusAsync(context.RequestAborted);
                    context.Response.StatusCode = status.Healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(status, context.RequestAborted);
                });
                endpoints.MapGet("/health/ready", async context =>
                {
                    var snapshot = NodeReadinessState.Snapshot();
                    NodeTelemetry.RecordReadinessCheck(snapshot.Ready, "ready-endpoint");
                    context.Response.StatusCode = snapshot.Ready
                        ? StatusCodes.Status200OK
                        : StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsJsonAsync(snapshot, context.RequestAborted);
                });
                endpoints.MapGet("/api/v1/network", async context =>
                {
                    var application = ServicePool.ApplicationStateMachine;
                    if (application is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                        return;
                    }

                    var state = application.State;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        chainId = state.ChainId,
                        height = state.Height.ToString(CultureInfo.InvariantCulture),
                        minRelayFeeBaseUnits = FeePolicy.MinRelayFee.BaseUnits.ToString(CultureInfo.InvariantCulture)
                    }, context.RequestAborted);
                });
                endpoints.MapGet("/api/v1/accounts/{address}", async context =>
                {
                    var application = ServicePool.ApplicationStateMachine;
                    var addressText = context.Request.RouteValues["address"]?.ToString();
                    if (application is null || !Address.TryParse(addressText ?? string.Empty, out var address)
                        || address.Version != ChainInfo.AddressVersion(application.State.ChainId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        return;
                    }

                    var account = application.State.GetAccount(address);
                    if (account is null)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    await context.Response.WriteAsJsonAsync(new
                    {
                        address = address.Encoded,
                        balanceBaseUnits = account.Balance.BaseUnits.ToString(CultureInfo.InvariantCulture),
                        nonce = account.Nonce.ToString(CultureInfo.InvariantCulture),
                        height = application.State.Height.ToString(CultureInfo.InvariantCulture)
                    }, context.RequestAborted);
                });
                endpoints.MapGet("/api/v1/accounts/{address}/transactions", async context =>
                {
                    var application = ServicePool.ApplicationStateMachine;
                    var addressText = context.Request.RouteValues["address"]?.ToString();
                    if (application is null || !Address.TryParse(addressText ?? string.Empty, out var address)
                        || address.Version != ChainInfo.AddressVersion(application.State.ChainId))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        return;
                    }

                    var limit = Math.Clamp(
                        int.TryParse(context.Request.Query["limit"], out var requestedLimit) ? requestedLimit : 50,
                        1,
                        100);
                    var transactions = ServicePool.CanonicalNodeService.Chain
                        .GetCanonicalBlocks(0)
                        .SelectMany(block => block.Txs.Select(transaction => new
                        {
                            transaction,
                            block.Height,
                            block.TimeStamp
                        }))
                        .Where(item => item.transaction.From.Encoded == address.Encoded
                            || item.transaction.To.Encoded == address.Encoded)
                        .OrderByDescending(item => item.Height)
                        .ThenByDescending(item => item.transaction.Nonce)
                        .Take(limit)
                        .Select(item => new
                        {
                            txId = item.transaction.ComputeIdHex(),
                            height = item.Height.ToString(CultureInfo.InvariantCulture),
                            timeStamp = item.TimeStamp.ToString(CultureInfo.InvariantCulture),
                            from = item.transaction.From.Encoded,
                            to = item.transaction.To.Encoded,
                            amountBaseUnits = item.transaction.Amount.BaseUnits.ToString(CultureInfo.InvariantCulture),
                            feeBaseUnits = item.transaction.Fee.BaseUnits.ToString(CultureInfo.InvariantCulture),
                            nonce = item.transaction.Nonce.ToString(CultureInfo.InvariantCulture)
                        })
                        .ToArray();

                    await context.Response.WriteAsJsonAsync(transactions, context.RequestAborted);
                });
                endpoints.MapGet("/api/v1/transactions/{txId}", async context =>
                {
                    var txId = context.Request.RouteValues["txId"]?.ToString()?.Trim().ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(txId) || txId.Length != 64
                        || txId.Any(character => !Uri.IsHexDigit(character)))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        return;
                    }

                    var canonical = ServicePool.CanonicalNodeService?.Chain
                        .GetCanonicalBlocks(0)
                        .SelectMany(block => block.Txs.Select(transaction => new { transaction, block.Height }))
                        .FirstOrDefault(item => item.transaction.ComputeIdHex() == txId);
                    if (canonical is not null)
                    {
                        TransactionStatusRegistry.MarkConfirmed(txId, canonical.Height);
                        await context.Response.WriteAsJsonAsync(new
                        {
                            txId,
                            status = "confirmed",
                            message = "Transaction committed in the canonical chain.",
                            height = canonical.Height.ToString(CultureInfo.InvariantCulture)
                        }, context.RequestAborted);
                        return;
                    }

                    if (!TransactionStatusRegistry.TryGet(txId, out var status))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

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
