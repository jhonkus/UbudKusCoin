// Created by I Putu Kusuma Negara
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
using System.Net.Http;
using System.Threading.Tasks;
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

                    var indexer = ServicePool.IndexerStore;
                    if (indexer is not null)
                    {
                        var indexedTxs = indexer.GetTransactionsForAddress(address.Encoded, limit);
                        var responseItems = indexedTxs.Select(item => new
                        {
                            txId = item.TxId,
                            height = item.Height.ToString(CultureInfo.InvariantCulture),
                            timeStamp = item.TimeStamp.ToString(CultureInfo.InvariantCulture),
                            from = item.From,
                            to = item.To,
                            amountBaseUnits = item.AmountBaseUnits.ToString(CultureInfo.InvariantCulture),
                            feeBaseUnits = item.FeeBaseUnits.ToString(CultureInfo.InvariantCulture),
                            nonce = item.Nonce.ToString(CultureInfo.InvariantCulture)
                        }).ToArray();

                        await context.Response.WriteAsJsonAsync(responseItems, context.RequestAborted);
                        return;
                    }

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

                    var indexer = ServicePool.IndexerStore;
                    var indexedTx = indexer?.GetTransactionById(txId);
                    if (indexedTx is not null)
                    {
                        TransactionStatusRegistry.MarkConfirmed(txId, indexedTx.Height);
                        await context.Response.WriteAsJsonAsync(new
                        {
                            txId,
                            status = "confirmed",
                            message = "Transaction committed in the canonical chain.",
                            height = indexedTx.Height.ToString(CultureInfo.InvariantCulture)
                        }, context.RequestAborted);
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
                endpoints.MapGet("/faucet", async context =>
                {
                    context.Response.ContentType = "text/html";
                    var isCaptchaEnabled = !string.IsNullOrWhiteSpace(DotNetEnv.Env.GetString("FAUCET_CAPTCHA_SECRET", string.Empty));
                    var faucetAddress = FaucetService.GetFaucetAddress();
                    var claimAmount = FaucetService.ClaimAmount;

                    var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>UbudKusCoin Testnet Faucet</title>
    <link href=""https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@300;400;500;600;700&display=swap"" rel=""stylesheet"">
    <style>
        :root {{
            --bg-color: #0b0f19;
            --card-bg: rgba(255, 255, 255, 0.03);
            --card-border: rgba(255, 255, 255, 0.08);
            --primary: #6366f1;
            --primary-glow: rgba(99, 102, 241, 0.15);
            --text: #f3f4f6;
            --text-secondary: #9ca3af;
            --success: #10b981;
            --error: #ef4444;
        }}

        * {{
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Plus Jakarta Sans', sans-serif;
        }}

        body {{
            background-color: var(--bg-color);
            color: var(--text);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow-x: hidden;
            position: relative;
        }}

        /* Glow effects */
        body::before, body::after {{
            content: '';
            position: absolute;
            width: 300px;
            height: 300px;
            border-radius: 50%;
            background: var(--primary);
            filter: blur(120px);
            opacity: 0.15;
            z-index: 0;
        }}
        body::before {{ top: 10%; left: 15%; }}
        body::after {{ bottom: 10%; right: 15%; }}

        .container {{
            width: 100%;
            max-width: 520px;
            padding: 24px;
            z-index: 1;
        }}

        .faucet-card {{
            background: var(--card-bg);
            border: 1px solid var(--card-border);
            backdrop-filter: blur(20px);
            -webkit-backdrop-filter: blur(20px);
            border-radius: 24px;
            padding: 40px 32px;
            box-shadow: 0 20px 40px rgba(0, 0, 0, 0.3);
            text-align: center;
        }}

        .logo {{
            font-size: 28px;
            font-weight: 700;
            background: linear-gradient(135deg, #a78bfa, #6366f1);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            margin-bottom: 8px;
        }}

        .tagline {{
            color: var(--text-secondary);
            font-size: 14px;
            margin-bottom: 32px;
        }}

        .info-pill {{
            background: rgba(255, 255, 255, 0.02);
            border: 1px solid var(--card-border);
            border-radius: 12px;
            padding: 12px 16px;
            margin-bottom: 24px;
            font-size: 13px;
            text-align: left;
        }}

        .info-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 6px;
        }}
        .info-row:last-child {{ margin-bottom: 0; }}
        .info-label {{ color: var(--text-secondary); }}
        .info-value {{ font-weight: 600; color: #fff; font-family: monospace; word-break: break-all; }}

        .input-group {{
            text-align: left;
            margin-bottom: 24px;
        }}

        .input-label {{
            font-size: 13px;
            font-weight: 600;
            margin-bottom: 8px;
            display: block;
            color: var(--text-secondary);
        }}

        .input-field {{
            width: 100%;
            background: rgba(255, 255, 255, 0.03);
            border: 1px solid var(--card-border);
            border-radius: 12px;
            padding: 14px 18px;
            color: #fff;
            font-size: 14px;
            transition: all 0.3s ease;
            outline: none;
        }}

        .input-field:focus {{
            border-color: var(--primary);
            box-shadow: 0 0 12px var(--primary-glow);
        }}

        .btn-submit {{
            width: 100%;
            background: linear-gradient(135deg, #7c3aed, #4f46e5);
            border: none;
            border-radius: 12px;
            padding: 16px;
            color: #fff;
            font-size: 15px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s ease;
            box-shadow: 0 4px 12px rgba(99, 102, 241, 0.3);
            display: flex;
            align-items: center;
            justify-content: center;
        }}

        .btn-submit:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 16px rgba(99, 102, 241, 0.4);
        }}

        .btn-submit:active {{
            transform: translateY(0);
        }}

        .btn-submit:disabled {{
            opacity: 0.6;
            cursor: not-allowed;
            transform: none;
            box-shadow: none;
        }}

        .alert {{
            border-radius: 12px;
            padding: 16px;
            margin-top: 24px;
            font-size: 14px;
            display: none;
            text-align: left;
            line-height: 1.5;
        }}

        .alert-success {{
            background: rgba(16, 185, 129, 0.1);
            border: 1px solid rgba(16, 185, 129, 0.2);
            color: #34d399;
        }}

        .alert-error {{
            background: rgba(239, 68, 68, 0.1);
            border: 1px solid rgba(239, 68, 68, 0.2);
            color: #f87171;
        }}

        /* Spinner */
        .spinner {{
            width: 20px;
            height: 20px;
            border: 2px solid rgba(255, 255, 255, 0.3);
            border-radius: 50%;
            border-top-color: #fff;
            animation: spin 0.8s linear infinite;
            margin-right: 8px;
            display: none;
        }}

        @keyframes spin {{
            to {{ transform: rotate(360deg); }}
        }}
    </style>
    {(isCaptchaEnabled ? @"<script src=""https://challenges.cloudflare.com/turnstile/v0/api.js"" async defer></script>" : "")}
</head>
<body>
    <div class=""container"">
        <div class=""faucet-card"">
            <h1 class=""logo"">UbudKusCoin</h1>
            <p class=""tagline"">Public Testnet Faucet</p>

            <div class=""info-pill"">
                <div class=""info-row"">
                    <span class=""info-label"">Faucet Wallet:</span>
                    <span class=""info-value"">{faucetAddress}</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Claim Size:</span>
                    <span class=""info-value"">{claimAmount} UKC</span>
                </div>
                <div class=""info-row"">
                    <span class=""info-label"">Interval:</span>
                    <span class=""info-value"">Once every 24 hours</span>
                </div>
            </div>

            <form id=""faucetForm"">
                <div class=""input-group"">
                    <label class=""input-label"" for=""address"">Recipient Address</label>
                    <input class=""input-field"" type=""text"" id=""address"" required placeholder=""e.g. U..."" autocomplete=""off"">
                </div>

                {(!isCaptchaEnabled ? @"
                <div class=""input-group"" id=""math-container"">
                    <label class=""input-label"" for=""math-ans"" id=""math-label"">Verification</label>
                    <input class=""input-field"" type=""number"" id=""math-ans"" required placeholder=""Enter the sum"">
                </div>" : @"
                <div class=""input-group"" style=""display: flex; justify-content: center; margin-bottom: 24px;"">
                    <div class=""cf-turnstile"" data-sitekey=""1x00000000000000000000AA""></div>
                </div>")}

                <button class=""btn-submit"" type=""submit"" id=""btnSubmit"">
                    <div class=""spinner"" id=""spinner""></div>
                    <span id=""btnText"">Claim Free UKC</span>
                </button>
            </form>

            <div class=""alert alert-success"" id=""alertSuccess""></div>
            <div class=""alert alert-error"" id=""alertError""></div>
        </div>
    </div>

    <script>
        const isCaptchaEnabled = {(isCaptchaEnabled ? "true" : "false")};
        let numA = 0;
        let numB = 0;

        function generateMathPuzzle() {{
            if (isCaptchaEnabled) return;
            numA = Math.floor(Math.random() * 9) + 1;
            numB = Math.floor(Math.random() * 9) + 1;
            document.getElementById('math-label').textContent = `Verification: What is ${{numA}} + ${{numB}}?`;
            document.getElementById('math-ans').value = '';
        }}

        if (!isCaptchaEnabled) {{
            generateMathPuzzle();
        }}

        document.getElementById('faucetForm').addEventListener('submit', async (e) => {{
            e.preventDefault();
            
            const address = document.getElementById('address').value.trim();
            const btnSubmit = document.getElementById('btnSubmit');
            const btnText = document.getElementById('btnText');
            const spinner = document.getElementById('spinner');
            const alertSuccess = document.getElementById('alertSuccess');
            const alertError = document.getElementById('alertError');

            alertSuccess.style.display = 'none';
            alertError.style.display = 'none';

            let payload = {{ address: address }};

            if (isCaptchaEnabled) {{
                const turnstileResponse = document.querySelector('[name=""cf-turnstile-response""]')?.value;
                if (!turnstileResponse) {{
                    alertError.textContent = 'Please complete the Turnstile Captcha verification.';
                    alertError.style.display = 'block';
                    return;
                }}
                payload.token = turnstileResponse;
            }} else {{
                const answer = parseInt(document.getElementById('math-ans').value);
                if (isNaN(answer)) return;
                payload.token = 'faucet-math-verified';
                payload.mathA = numA;
                payload.mathB = numB;
                payload.mathAns = answer;
            }}

            // Loading state
            btnSubmit.disabled = true;
            spinner.style.display = 'block';
            btnText.textContent = 'Processing Claim...';

            try {{
                const response = await fetch('/api/v1/faucet/claim', {{
                    method: 'POST',
                    headers: {{
                        'Content-Type': 'application/json'
                    }},
                    body: JSON.stringify(payload)
                }});

                const data = await response.json();

                if (response.ok && data.success) {{
                    alertSuccess.innerHTML = `<strong>Success!</strong> Distributed ${{data.amount}} UKC to your wallet.<br><span style=""font-size: 12px; opacity: 0.8;"">Tx ID: ${{data.txId}}</span>`;
                    alertSuccess.style.display = 'block';
                    document.getElementById('address').value = '';
                }} else {{
                    alertError.textContent = data.error || 'Faucet claim failed. Please try again.';
                    alertError.style.display = 'block';
                }}
            }} catch (err) {{
                alertError.textContent = 'Network or connection error occurred.';
                alertError.style.display = 'block';
            }} finally {{
                btnSubmit.disabled = false;
                spinner.style.display = 'none';
                btnText.textContent = 'Claim Free UKC';
                if (!isCaptchaEnabled) {{
                    generateMathPuzzle();
                }}
            }}
        }});
    </script>
</body>
</html>";
                    await context.Response.WriteAsync(html, context.RequestAborted);
                });
                endpoints.MapPost("/api/v1/faucet/claim", async context =>
                {
                    try
                    {
                        var body = await context.Request.ReadFromJsonAsync<FaucetClaimRequest>(context.RequestAborted);
                        if (body == null || string.IsNullOrWhiteSpace(body.Address))
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsJsonAsync(new { success = false, error = "Recipient address is required." }, context.RequestAborted);
                            return;
                        }

                        // Math challenge check when Cloudflare Turnstile is disabled
                        var captchaSecret = DotNetEnv.Env.GetString("FAUCET_CAPTCHA_SECRET", string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(captchaSecret))
                        {
                            if (!body.MathA.HasValue || !body.MathB.HasValue || !body.MathAns.HasValue ||
                                body.MathAns.Value != (body.MathA.Value + body.MathB.Value))
                            {
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                await context.Response.WriteAsJsonAsync(new { success = false, error = "Verification answer is incorrect." }, context.RequestAborted);
                                return;
                            }
                        }

                        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                        var result = await FaucetService.ClaimAsync(body.Address, clientIp, body.Token, context.RequestAborted);

                        if (result.Success)
                        {
                            context.Response.StatusCode = StatusCodes.Status200OK;
                            await context.Response.WriteAsJsonAsync(new
                            {
                                success = true,
                                txId = result.TxId,
                                amount = result.Amount
                            }, context.RequestAborted);
                        }
                        else
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsJsonAsync(new { success = false, error = result.Error }, context.RequestAborted);
                        }
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsJsonAsync(new { success = false, error = ex.Message }, context.RequestAborted);
                    }
                });
                endpoints.MapPost("/api/v1/transactions", async context =>
                {
                    try
                    {
                        var body = await context.Request.ReadFromJsonAsync<SubmitTxRequest>(context.RequestAborted);
                        if (body == null || string.IsNullOrWhiteSpace(body.Hex))
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsJsonAsync(new { error = "Missing transaction hex." }, context.RequestAborted);
                            return;
                        }

                        var txBytes = Convert.FromHexString(body.Hex);
                        if (!TransactionCodec.TryDecode(txBytes, out var transaction, out var decodeError))
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsJsonAsync(new { error = $"Invalid transaction format: {decodeError}" }, context.RequestAborted);
                            return;
                        }

                        var consensusOptions = ConsensusEngineOptions.FromEnvironment();
                        if (consensusOptions.Mode == ConsensusEngineMode.Development)
                        {
                            // In development mode, we add directly to the database and broadcast
                            var grpcTx = CanonicalExplorerMapper.ToTransaction(transaction!, 0, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                            ServicePool.DbService.PoolTransactionsDb.Add(grpcTx);
                            SafeTask.Run(() => ServicePool.P2PService.BroadcastTransaction(grpcTx), "REST Submit Transaction P2P Broadcast");
                        }
                        else
                        {
                            var cometRpcUrl = DotNetEnv.Env.GetString("COMETBFT_RPC_URL", "http://localhost:26657");
                            using var client = new HttpClient();
                            var response = await client.GetAsync($"{cometRpcUrl}/broadcast_tx_sync?tx=0x{body.Hex}", context.RequestAborted);
                            if (!response.IsSuccessStatusCode)
                            {
                                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                                await context.Response.WriteAsJsonAsync(new { error = "CometBFT broadcast failed." }, context.RequestAborted);
                                return;
                            }
                        }

                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            txId = transaction!.ComputeIdHex(),
                            status = "pending",
                            message = "Transaction broadcasted to consensus network successfully."
                        }, context.RequestAborted);
                    }
                    catch (Exception ex)
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsJsonAsync(new { error = ex.Message }, context.RequestAborted);
                    }
                });
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync(
                        "Communication with gRPC endpoints" +
                        " must be made through a gRPC client.");
                });
            });
        }

        private class FaucetClaimRequest
        {
            public string Address { get; set; } = "";
            public string Token { get; set; } = "";
            public int? MathA { get; set; }
            public int? MathB { get; set; }
            public int? MathAns { get; set; }
        }

        private class SubmitTxRequest
        {
            public string Hex { get; set; } = "";
        }
    }
}
