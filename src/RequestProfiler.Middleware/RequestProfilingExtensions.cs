using Microsoft.AspNetCore.Builder;

namespace RequestProfiler.Middleware;
public static class RequestProfilingExtensions
{
    public static IApplicationBuilder UseRequestProfiler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestProfilingMiddleware>();
    }
}

