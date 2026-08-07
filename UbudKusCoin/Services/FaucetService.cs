using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NBitcoin;
using UbudKusCoin.Core.Signing;
using UbudKusCoin.Core.Types;
using UbudKusCoin.Grpc;
using CoreTransaction = UbudKusCoin.Core.Types.Transaction;
using CoreMoney = UbudKusCoin.Core.Types.Money;

namespace UbudKusCoin.Services;

public static class FaucetService
{
    private static readonly ConcurrentDictionary<string, DateTime> IPClaims = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, DateTime> AddressClaims = new(StringComparer.OrdinalIgnoreCase);

    private static readonly decimal ClaimAmountCoins = decimal.TryParse(DotNetEnv.Env.GetString("FAUCET_CLAIM_AMOUNT", "10.0"), out var amt) ? amt : 10.0m;
    private static readonly TimeSpan ClaimInterval = TimeSpan.FromHours(24);

    public static decimal ClaimAmount => ClaimAmountCoins;

    public static void ResetRateLimits()
    {
        IPClaims.Clear();
        AddressClaims.Clear();
    }

    public static string GetFaucetAddress()
    {
        var privateKeyHex = DotNetEnv.Env.GetString("FAUCET_PRIVATE_KEY", string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(privateKeyHex))
        {
            var key = new Key(Convert.FromHexString(privateKeyHex));
            return Address.FromPublicKey(Address.TestnetVersion, key.PubKey.ToBytes()).Encoded;
        }

        if (ServicePool.WalletService != null)
        {
            return ServicePool.WalletService.GetAddress();
        }

        var defaultKey = new Key(new byte[32] { 0xff, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        return Address.FromPublicKey(Address.TestnetVersion, defaultKey.PubKey.ToBytes()).Encoded;
    }

    public static async Task<FaucetClaimResult> ClaimAsync(string recipientAddress, string ipAddress, string captchaToken, CancellationToken cancellationToken)
    {
        // 1. Basic address validation
        if (!Address.TryParse(recipientAddress, out var toAddress))
        {
            return FaucetClaimResult.Fail("Invalid UbudKusCoin address format.");
        }

        // 2. Rate limit check
        var now = DateTime.UtcNow;
        if (IPClaims.TryGetValue(ipAddress, out var lastIpClaim) && (now - lastIpClaim) < ClaimInterval)
        {
            var timeLeft = ClaimInterval - (now - lastIpClaim);
            return FaucetClaimResult.Fail($"IP address rate limited. Please try again in {timeLeft.Hours}h {timeLeft.Minutes}m.");
        }

        if (AddressClaims.TryGetValue(recipientAddress, out var lastAddrClaim) && (now - lastAddrClaim) < ClaimInterval)
        {
            var timeLeft = ClaimInterval - (now - lastAddrClaim);
            return FaucetClaimResult.Fail($"Wallet address rate limited. Please try again in {timeLeft.Hours}h {timeLeft.Minutes}m.");
        }

        // 3. Captcha verification
        var captchaSecret = DotNetEnv.Env.GetString("FAUCET_CAPTCHA_SECRET", string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(captchaSecret))
        {
            var isCaptchaValid = await VerifyCaptchaAsync(captchaSecret, captchaToken, ipAddress, cancellationToken);
            if (!isCaptchaValid)
            {
                return FaucetClaimResult.Fail("Captcha verification failed. Please try again.");
            }
        }
        else
        {
            // Simple fallback check if no secret configured
            if (string.IsNullOrWhiteSpace(captchaToken) || captchaToken != "faucet-math-verified")
            {
                return FaucetClaimResult.Fail("Verification required.");
            }
        }

        // 4. Retrieve faucet wallet credentials
        byte[] privateKeyBytes;
        byte[] publicKeyBytes;
        string fromAddressEncoded;

        var privateKeyHex = DotNetEnv.Env.GetString("FAUCET_PRIVATE_KEY", string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(privateKeyHex))
        {
            var key = new Key(Convert.FromHexString(privateKeyHex));
            privateKeyBytes = key.ToBytes();
            publicKeyBytes = key.PubKey.ToBytes();
            fromAddressEncoded = Address.FromPublicKey(Address.TestnetVersion, publicKeyBytes).Encoded;
        }
        else
        {
            if (ServicePool.WalletService != null)
            {
                var keyPair = ServicePool.WalletService.GetKeyPair();
                privateKeyBytes = keyPair.PrivateKey.PrivateKey.ToBytes();
                publicKeyBytes = keyPair.PublicKey.PubKey.ToBytes();
                fromAddressEncoded = ServicePool.WalletService.GetAddress();
            }
            else
            {
                var defaultKey = new Key(new byte[32] { 0xff, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
                privateKeyBytes = defaultKey.ToBytes();
                publicKeyBytes = defaultKey.PubKey.ToBytes();
                fromAddressEncoded = Address.FromPublicKey(Address.TestnetVersion, publicKeyBytes).Encoded;
            }
        }

        if (fromAddressEncoded == recipientAddress)
        {
            return FaucetClaimResult.Fail("Faucet cannot send coins to itself.");
        }

        // 5. Construct and sign transfer transaction
        var application = ServicePool.ApplicationStateMachine;
        if (application == null)
        {
            return FaucetClaimResult.Fail("Blockchain application state is currently unavailable.");
        }

        var faucetAccount = application.State.GetAccount(Address.Parse(fromAddressEncoded));
        if (faucetAccount == null || faucetAccount.Balance < CoreMoney.FromCoins(ClaimAmountCoins + 0.001m))
        {
            return FaucetClaimResult.Fail("Faucet wallet has insufficient balance.");
        }

        var tx = new CoreTransaction
        {
            Version = ChainInfo.TxVersion,
            ChainId = application.State.ChainId,
            Kind = TransactionKind.Transfer,
            From = Address.Parse(fromAddressEncoded),
            To = toAddress,
            Amount = CoreMoney.FromCoins(ClaimAmountCoins),
            Fee = application.State.BaseFee,
            Nonce = faucetAccount.Nonce + 1,
            PubKey = publicKeyBytes
        };
        tx.Signature = TransactionSigner.Sign(tx, privateKeyBytes);

        // 6. Broadcast transaction to consensus network
        var txBytes = TransactionCodec.Encode(tx);
        var txHex = Convert.ToHexStringLower(txBytes);

        try
        {
            var consensusOptions = ConsensusEngineOptions.FromEnvironment();
            if (consensusOptions.Mode == ConsensusEngineMode.Development)
            {
                var grpcTx = CanonicalExplorerMapper.ToTransaction(tx, 0, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                if (ServicePool.DbService?.PoolTransactionsDb != null)
                {
                    ServicePool.DbService.PoolTransactionsDb.Add(grpcTx);
                }
                if (ServicePool.P2PService != null)
                {
                    SafeTask.Run(() => ServicePool.P2PService.BroadcastTransaction(grpcTx), "Faucet Claim P2P Broadcast");
                }
            }
            else
            {
                var cometRpcUrl = DotNetEnv.Env.GetString("COMETBFT_RPC_URL", "http://localhost:26657");
                using var client = new HttpClient();
                var response = await client.GetAsync($"{cometRpcUrl}/broadcast_tx_sync?tx=0x{txHex}", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return FaucetClaimResult.Fail("Failed to broadcast transaction to consensus nodes.");
                }
            }
        }
        catch (Exception ex)
        {
            return FaucetClaimResult.Fail($"Network broadcast error: {ex.Message}");
        }

        // 7. Update rate limits on success
        IPClaims[ipAddress] = now;
        AddressClaims[recipientAddress] = now;

        return FaucetClaimResult.SuccessResult(tx.ComputeIdHex(), ClaimAmountCoins);
    }

    private static async Task<bool> VerifyCaptchaAsync(string secret, string token, string ip, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient();
            var values = new Dictionary<string, string>
            {
                { "secret", secret },
                { "response", token },
                { "remoteip", ip }
            };

            var content = new FormUrlEncodedContent(values);
            // Verify Turnstile / reCAPTCHA
            var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // Fallback to Google reCAPTCHA
                response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content, cancellationToken);
            }

            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<CaptchaVerifyResponse>(cancellationToken: cancellationToken);
            return result != null && result.Success;
        }
        catch
        {
            return false;
        }
    }

    private class CaptchaVerifyResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
    }
}

public sealed class FaucetClaimResult
{
    public bool Success { get; }
    public string? Error { get; }
    public string? TxId { get; }
    public decimal Amount { get; }

    private FaucetClaimResult(bool success, string? error, string? txId, decimal amount)
    {
        Success = success;
        Error = error;
        TxId = txId;
        Amount = amount;
    }

    public static FaucetClaimResult Fail(string error) => new(false, error, null, 0);
    public static FaucetClaimResult SuccessResult(string txId, decimal amount) => new(true, null, txId, amount);
}
