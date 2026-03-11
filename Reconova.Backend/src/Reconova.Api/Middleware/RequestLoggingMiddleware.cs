using System.Diagnostics;

namespace Reconova.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var requestPath = context.Request.Path;
        var method = context.Request.Method;

        try
        {
            await _next(context);
            sw.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                method, requestPath, context.Response.StatusCode, sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            _logger.LogError(
                "HTTP {Method} {Path} failed after {ElapsedMs}ms",
                method, requestPath, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
