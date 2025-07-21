using StackExchange.Redis;
using System.Text.Json;

namespace RequestProfiler.Storage;

public class RedisTraceStorage : ITraceStorage
{
    private readonly IDatabase _db;
    private readonly string _keyPrefix;

    public RedisTraceStorage(IConnectionMultiplexer redis, string keyPrefix = "requestprofiler:trace:")
    {
        _db = redis.GetDatabase();
        _keyPrefix = keyPrefix;
    }

    public async Task StoreTraceAsync(string correlationId, object trace)
    {
        var json = JsonSerializer.Serialize(trace);
        await _db.StringSetAsync(_keyPrefix + correlationId, json, TimeSpan.FromHours(1));
    }

    public async Task<string?> GetTraceAsync(string correlationId)
    {
        var value = await _db.StringGetAsync(_keyPrefix + correlationId);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<IEnumerable<string>> GetAllTracesAsync(int max = 100)
    {
        var server = _db.Multiplexer.GetServer(_db.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: _keyPrefix + "*").Take(max);
        var traces = new List<string>();
        foreach (var key in keys)
        {
            var value = await _db.StringGetAsync(key);
            if (value.HasValue)
                traces.Add(value);
        }
        return traces;
    }
}
