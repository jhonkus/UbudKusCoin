#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using UbudKusCoin.CometBft.Abci;
using UbudKusCoin.Grpc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UbudKusCoin.Services;

/// <summary>
/// CometBFT v0.38 ABCI socket transport. CometBFT opens separate connections
/// for its application channels, so every accepted connection is handled
/// independently and requests are processed in order per connection.
/// </summary>
public sealed class AbciSocketServer : BackgroundService
{
    private readonly int _port;
    private readonly ILogger<AbciSocketServer> _logger;
    private TcpListener? _listener;

    public AbciSocketServer(ILogger<AbciSocketServer> logger)
    {
        _logger = logger;
        _port = DotNetEnv.Env.GetInt("ABCI_SOCKET_PORT");
        if (_port == 0)
        {
            _port = 26658;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        NodeReadinessState.SetAbciSocketReady(true);
        _logger.LogInformation("ABCI socket server started on port {Port}.", _port);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            NodeReadinessState.SetAbciSocketReady(false);
            _logger.LogInformation("ABCI socket server stopped.");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _listener?.Stop();
        return base.StopAsync(cancellationToken);
    }

    private static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        await using (var stream = client.GetStream())
        {
            var service = new AbciServiceImpl();
            while (!cancellationToken.IsCancellationRequested)
            {
                var payload = await ReadFrameAsync(stream, cancellationToken);
                if (payload is null)
                {
                    return;
                }

                var request = Request.Parser.ParseFrom(payload);
                var response = await DispatchAsync(service, request);
                var encoded = response.ToByteArray();
                await WriteFrameAsync(stream, encoded, cancellationToken);
            }
        }
    }

    private static async Task<Response> DispatchAsync(AbciServiceImpl service, Request request)
    {
        return request.ValueCase switch
        {
            Request.ValueOneofCase.Echo => new Response { Echo = await service.Echo(request.Echo, null!) },
            Request.ValueOneofCase.Flush => new Response { Flush = await service.Flush(request.Flush, null!) },
            Request.ValueOneofCase.Info => new Response { Info = await service.Info(request.Info, null!) },
            Request.ValueOneofCase.CheckTx => new Response { CheckTx = await service.CheckTx(request.CheckTx, null!) },
            Request.ValueOneofCase.Query => new Response { Query = await service.Query(request.Query, null!) },
            Request.ValueOneofCase.Commit => new Response { Commit = await service.Commit(request.Commit, null!) },
            Request.ValueOneofCase.InitChain => new Response { InitChain = await service.InitChain(request.InitChain, null!) },
            Request.ValueOneofCase.ListSnapshots => new Response { ListSnapshots = await service.ListSnapshots(request.ListSnapshots, null!) },
            Request.ValueOneofCase.OfferSnapshot => new Response { OfferSnapshot = await service.OfferSnapshot(request.OfferSnapshot, null!) },
            Request.ValueOneofCase.LoadSnapshotChunk => new Response { LoadSnapshotChunk = await service.LoadSnapshotChunk(request.LoadSnapshotChunk, null!) },
            Request.ValueOneofCase.ApplySnapshotChunk => new Response { ApplySnapshotChunk = await service.ApplySnapshotChunk(request.ApplySnapshotChunk, null!) },
            Request.ValueOneofCase.PrepareProposal => new Response { PrepareProposal = await service.PrepareProposal(request.PrepareProposal, null!) },
            Request.ValueOneofCase.ProcessProposal => new Response { ProcessProposal = await service.ProcessProposal(request.ProcessProposal, null!) },
            Request.ValueOneofCase.ExtendVote => new Response { ExtendVote = await service.ExtendVote(request.ExtendVote, null!) },
            Request.ValueOneofCase.VerifyVoteExtension => new Response { VerifyVoteExtension = await service.VerifyVoteExtension(request.VerifyVoteExtension, null!) },
            Request.ValueOneofCase.FinalizeBlock => new Response { FinalizeBlock = await service.FinalizeBlock(request.FinalizeBlock, null!) },
            _ => new Response { Exception = new ResponseException { Error = "Empty ABCI request." } }
        };
    }

    private static async Task<byte[]?> ReadFrameAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var first = await ReadByteAsync(stream, cancellationToken);
        if (first < 0)
        {
            return null;
        }

        var length = await ReadVarintAsync(first, stream, cancellationToken);
        if (length > 16 * 1024 * 1024)
        {
            throw new InvalidDataException("ABCI frame exceeds the configured maximum.");
        }

        var payload = new byte[length];
        await ReadExactlyAsync(stream, payload, cancellationToken);
        return payload;
    }

    private static async Task WriteFrameAsync(NetworkStream stream, byte[] payload, CancellationToken cancellationToken)
    {
        var prefix = EncodeVarint((uint)payload.Length);
        await stream.WriteAsync(prefix, cancellationToken);
        await stream.WriteAsync(payload, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<uint> ReadVarintAsync(int first, NetworkStream stream, CancellationToken cancellationToken)
    {
        uint result = (uint)(first & 0x7F);
        var shift = 7;
        while ((first & 0x80) != 0)
        {
            first = await ReadByteAsync(stream, cancellationToken);
            if (first < 0 || shift > 28)
            {
                throw new InvalidDataException("Invalid ABCI frame length.");
            }

            result |= (uint)(first & 0x7F) << shift;
            shift += 7;
        }

        return result;
    }

    private static async Task<int> ReadByteAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        var read = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
        return read == 0 ? -1 : buffer[0];
    }

    private static byte[] EncodeVarint(uint value)
    {
        var bytes = new List<byte>();
        while (value >= 0x80)
        {
            bytes.Add((byte)(value | 0x80));
            value >>= 7;
        }

        bytes.Add((byte)value);
        return bytes.ToArray();
    }

    private static async Task ReadExactlyAsync(NetworkStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken);
            if (read == 0)
            {
                throw new EndOfStreamException("ABCI connection closed mid-frame.");
            }

            offset += read;
        }
    }
}
