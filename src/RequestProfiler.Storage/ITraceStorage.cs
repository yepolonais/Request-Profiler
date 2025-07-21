namespace RequestProfiler.Storage;

public interface ITraceStorage
{
    Task StoreTraceAsync(string correlationId, object trace);
    Task<string?> GetTraceAsync(string correlationId);
    Task<IEnumerable<string>> GetAllTracesAsync(int max = 100);
}
