using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RequestProfiler.Middleware;
public class RequestProfilingMiddleware(RequestDelegate next, ILogger<RequestProfilingMiddleware> logger)
{
    private static readonly string[] ExcludedPaths = ["/health", "/healthz", "/ping"];

    public async Task InvokeAsync(HttpContext context)
    {
        // Filtrage des routes à exclure
        if (ExcludedPaths.Any(p => context.Request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }
        var stopwatch = Stopwatch.StartNew();

        // Lecture du body de la requête
        string requestBody = "";
        if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }
        }

        // Intercepter le body de la réponse
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        await next(context);

        stopwatch.Stop();

        // Lecture du body de la réponse
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        string responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        await responseBodyStream.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;

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

        logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Duration} ms, {ResponseSize} bytes | RequestHeaders: {RequestHeaders} | ResponseHeaders: {ResponseHeaders} | RequestBody: {RequestBody} | ResponseBody: {ResponseBody}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            durationMs,
            responseSize,
            requestHeaders,
            responseHeaders,
            requestBody,
            responseBody);
    }
}

