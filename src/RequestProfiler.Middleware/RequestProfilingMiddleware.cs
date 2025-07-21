using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RequestProfiler.Middleware;
public class RequestProfilingMiddleware(RequestDelegate next, ILogger<RequestProfilingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        var durationMs = stopwatch.Elapsed.TotalMilliseconds;

        // Calcul de la taille de la réponse
        long responseSize = 0;
        if (context.Response.Body.CanSeek)
        {
            responseSize = context.Response.Body.Length;
        }

        context.Response.Headers["X-Request-Duration-ms"] = durationMs.ToString("F2");
        context.Response.Headers["X-Response-Size"] = responseSize.ToString();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var statusCode = context.Response.StatusCode;

        // Log des headers de la requête
        var requestHeaders = string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
        // Log des headers de la réponse
        var responseHeaders = string.Join(", ", context.Response.Headers.Select(h => $"{h.Key}: {h.Value}"));

        logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Duration} ms, {ResponseSize} bytes | RequestHeaders: {RequestHeaders} | ResponseHeaders: {ResponseHeaders}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            durationMs,
            responseSize,
            requestHeaders,
            responseHeaders);
    }
}

