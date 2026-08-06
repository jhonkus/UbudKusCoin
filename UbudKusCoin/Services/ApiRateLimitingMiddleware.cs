using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace UbudKusCoin.Services;

public sealed class ApiRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ConcurrentDictionary<string, Window> _windows = new();
    private readonly int _limit;
    private readonly int _protectedPort;

    public ApiRateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
        _limit = Math.Max(1, DotNetEnv.Env.GetInt("API_RATE_LIMIT_PER_MINUTE", 120));
        _protectedPort = DotNetEnv.Env.GetInt("GRPC_WEB_PORT");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Connection.LocalPort != _protectedPort
            || context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var address = context.Connection.RemoteIpAddress?.ToString() ?? IPAddress.None.ToString();
        var now = DateTimeOffset.UtcNow;
        var window = _windows.AddOrUpdate(
            address,
            _ => new Window(now, 1),
            (_, current) => current.Start.AddMinutes(1) <= now
                ? new Window(now, 1)
                : new Window(current.Start, current.Count + 1));

        context.Response.Headers["X-RateLimit-Limit"] = _limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, _limit - window.Count).ToString();
        if (window.Count > _limit)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = "60";
            await context.Response.WriteAsJsonAsync(new { status = "error", message = "Rate limit exceeded." });
            return;
        }

        await _next(context);
    }

    private sealed record Window(DateTimeOffset Start, int Count);
}
