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
        if (IsExcludedPath(context.Request.Path))
        {
            await next(context);
            return;
        }

        string correlationId = EnsureCorrelationId(context);
        var stopwatch = Stopwatch.StartNew();
        string requestBody = await ReadRequestBodyAsync(context);
        var (originalBodyStream, responseBodyStream) = InterceptResponseBody(context);

        await next(context);

        stopwatch.Stop();
        string responseBody = await ReadResponseBodyAsync(context);
        RestoreResponseBody(context, originalBodyStream, responseBodyStream);

        double durationMs = stopwatch.Elapsed.TotalMilliseconds;
        long responseSize = GetResponseSize(context);
        AddProfilingHeaders(context, durationMs, responseSize, correlationId);

        string method = context.Request.Method;
        string path = context.Request.Path;
        int statusCode = context.Response.StatusCode;
        string requestHeaders = GetHeadersString(context.Request.Headers);
        string responseHeaders = GetHeadersString(context.Response.Headers);

        logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {Duration} ms, {ResponseSize} bytes | CorrelationId: {CorrelationId} | RequestHeaders: {RequestHeaders} | ResponseHeaders: {ResponseHeaders} | RequestBody: {RequestBody} | ResponseBody: {ResponseBody}",
            method,
            path,
            statusCode,
            durationMs,
            responseSize,
            correlationId,
            requestHeaders,
            responseHeaders,
            requestBody,
            responseBody);
    }

    private static bool IsExcludedPath(PathString path)
        => ExcludedPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    private static string EnsureCorrelationId(HttpContext context)
    {
        const string CorrelationIdHeader = "X-Correlation-Id";
        string? correlationId = context.Request.Headers.TryGetValue(CorrelationIdHeader, out var values) ? values.FirstOrDefault() : null;
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[CorrelationIdHeader] = correlationId;
        }
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        return correlationId;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        if (context.Request.ContentLength > 0 && context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            string body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            return body;
        }
        return string.Empty;
    }

    private static (Stream originalBodyStream, MemoryStream responseBodyStream) InterceptResponseBody(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        return (originalBodyStream, responseBodyStream);
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        string body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private static async void RestoreResponseBody(HttpContext context, Stream originalBodyStream, MemoryStream responseBodyStream)
    {
        await responseBodyStream.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }

    private static long GetResponseSize(HttpContext context)
        => context.Response.Body.CanSeek ? context.Response.Body.Length : 0;

    private static void AddProfilingHeaders(HttpContext context, double durationMs, long responseSize, string correlationId)
    {
        context.Response.Headers["X-Request-Duration-ms"] = durationMs.ToString("F2");
        context.Response.Headers["X-Response-Size"] = responseSize.ToString();
        context.Response.Headers["X-Correlation-Id"] = correlationId;
    }

    private static string GetHeadersString(IHeaderDictionary headers)
        => string.Join(", ", headers.Select(h => $"{h.Key}: {h.Value}"));
}

