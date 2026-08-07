using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UbudKusCoin.Core.Types;

namespace UbudKusCoin.Sdk;

public sealed class UbudKusCoinClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string? _authToken;

    public UbudKusCoinClient(string baseUrl, string? authToken = null, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/");
        _authToken = authToken;

        if (!string.IsNullOrWhiteSpace(_authToken))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _authToken);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<NetworkInfo> GetNetworkAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<NetworkResponseModel>(
            "api/v1/network", cancellationToken);
        if (response == null) throw new InvalidOperationException("Empty response from network API.");

        return new NetworkInfo(
            uint.Parse(response.ChainId),
            long.Parse(response.Height),
            long.Parse(response.MinRelayFeeBaseUnits));
    }

    public async Task<AccountInfo> GetAccountAsync(string address, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<AccountResponseModel>(
            $"api/v1/accounts/{address}", cancellationToken);
        if (response == null) throw new InvalidOperationException("Empty response from account API.");

        return new AccountInfo(
            response.Address,
            long.Parse(response.BalanceBaseUnits),
            ulong.Parse(response.Nonce),
            long.Parse(response.Height));
    }

    public async Task<TxStatusInfo> GetTransactionStatusAsync(string txId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<TxStatusResponseModel>(
            $"api/v1/transactions/{txId}", cancellationToken);
        if (response == null) throw new InvalidOperationException("Empty response from transaction status API.");

        return new TxStatusInfo(
            response.TxId,
            response.Status,
            response.Message,
            string.IsNullOrWhiteSpace(response.Height) ? null : long.Parse(response.Height));
    }

    /// <summary>
    /// Encodes a signed transaction using TransactionCodec and POSTs the hex representation to the node.
    /// </summary>
    public async Task<TxStatusInfo> SubmitTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        var txBytes = TransactionCodec.Encode(transaction);
        var txHex = Convert.ToHexStringLower(txBytes);

        var requestBody = new SubmitTxRequest { Hex = txHex };
        using var response = await _httpClient.PostAsJsonAsync("api/v1/transactions", requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TxStatusResponseModel>(cancellationToken: cancellationToken);
        if (result == null) throw new InvalidOperationException("Empty response from submit transaction API.");

        return new TxStatusInfo(
            result.TxId,
            result.Status,
            result.Message,
            string.IsNullOrWhiteSpace(result.Height) ? null : long.Parse(result.Height));
    }

    // JSON DTOs
    private class NetworkResponseModel
    {
        [JsonPropertyName("chainId")] public string ChainId { get; set; } = "";
        [JsonPropertyName("height")] public string Height { get; set; } = "";
        [JsonPropertyName("minRelayFeeBaseUnits")] public string MinRelayFeeBaseUnits { get; set; } = "";
    }

    private class AccountResponseModel
    {
        [JsonPropertyName("address")] public string Address { get; set; } = "";
        [JsonPropertyName("balanceBaseUnits")] public string BalanceBaseUnits { get; set; } = "";
        [JsonPropertyName("nonce")] public string Nonce { get; set; } = "";
        [JsonPropertyName("height")] public string Height { get; set; } = "";
    }

    private class TxStatusResponseModel
    {
        [JsonPropertyName("txId")] public string TxId { get; set; } = "";
        [JsonPropertyName("status")] public string Status { get; set; } = "";
        [JsonPropertyName("message")] public string Message { get; set; } = "";
        [JsonPropertyName("height")] public string Height { get; set; } = "";
    }

    private class SubmitTxRequest
    {
        [JsonPropertyName("hex")] public string Hex { get; set; } = "";
    }
}

public record NetworkInfo(uint ChainId, long Height, long MinRelayFeeBaseUnits);
public record AccountInfo(string Address, long BalanceBaseUnits, ulong Nonce, long Height);
public record TxStatusInfo(string TxId, string Status, string Message, long? Height);
