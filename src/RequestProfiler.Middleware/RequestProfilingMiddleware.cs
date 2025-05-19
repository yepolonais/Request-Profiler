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

        context.Response.Headers["X-Request-Duration-ms"] = durationMs.ToString("F2");
        var method = context.Request.Method;
        var path = context.Request.Path;
        var statusCode = context.Response.StatusCode;

        logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Duration} ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            durationMs);
    }
}

