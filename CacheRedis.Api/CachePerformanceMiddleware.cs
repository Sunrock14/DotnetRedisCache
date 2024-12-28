using System.Diagnostics;

namespace CacheRedis.Api;

public class CachePerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CachePerformanceMiddleware> _logger;

    public CachePerformanceMiddleware(RequestDelegate next, ILogger<CachePerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        _logger.LogInformation($"Request to {context.Request.Path} took {stopwatch.ElapsedMilliseconds}ms");
    }
}
