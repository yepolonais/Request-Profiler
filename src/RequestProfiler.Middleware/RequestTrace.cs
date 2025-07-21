namespace RequestProfiler.Middleware;

public class RequestTrace
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public double DurationMs { get; set; }
    public long ResponseSize { get; set; }
    public string RequestHeaders { get; set; } = string.Empty;
    public string ResponseHeaders { get; set; } = string.Empty;
    public string RequestBody { get; set; } = string.Empty;
    public string ResponseBody { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
