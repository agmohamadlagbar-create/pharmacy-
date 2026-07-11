using System.Collections.Concurrent;

namespace OnlineStore.Api.Services.Agents;

public class AiUsageTracker
{
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _providerCounters = new();
    private readonly ConcurrentBag<UsageRecord> _usageLog = new();
    private readonly ConcurrentBag<FallbackRecord> _fallbackLog = new();

    public bool IsAboveThreshold(string providerName, int thresholdPerMinute)
    {
        if (_providerCounters.TryGetValue(providerName, out var counter))
            return counter.Count > thresholdPerMinute;
        return false;
    }

    public void RecordRequest(string provider, string model, int tokens, long latencyMs)
    {
        var counter = _providerCounters.GetOrAdd(provider, _ => new SlidingWindowCounter());
        counter.Increment();

        _usageLog.Add(new UsageRecord
        {
            Provider = provider,
            Model = model,
            Tokens = tokens,
            LatencyMs = latencyMs,
            Timestamp = DateTime.UtcNow
        });
    }

    public void RecordFallback(string fromProvider, string toProvider)
    {
        _fallbackLog.Add(new FallbackRecord
        {
            FromProvider = fromProvider,
            ToProvider = toProvider,
            Timestamp = DateTime.UtcNow
        });
    }

    public AiUsageSummary GetSummary()
    {
        var now = DateTime.UtcNow;
        return new AiUsageSummary
        {
            TotalRequests = _usageLog.Count,
            TotalFallbacks = _fallbackLog.Count,
            RecentRequests = _usageLog.Count(r => (now - r.Timestamp).TotalMinutes < 5),
            ProviderCounts = _usageLog.GroupBy(r => r.Provider).ToDictionary(g => g.Key, g => g.Count()),
            RecentFallbacks = _fallbackLog.Where(r => (now - r.Timestamp).TotalMinutes < 5).Select(r => $"{r.FromProvider}→{r.ToProvider}").ToList()
        };
    }
}

public class SlidingWindowCounter
{
    private long _count;
    private DateTime _windowStart = DateTime.UtcNow;

    public long Count => Interlocked.Read(ref _count);

    public void Increment()
    {
        var now = DateTime.UtcNow;
        if ((now - _windowStart).TotalMinutes >= 1)
        {
            Interlocked.Exchange(ref _count, 0);
            _windowStart = now;
        }
        Interlocked.Increment(ref _count);
    }
}

public class UsageRecord
{
    public string Provider { get; set; } = "";
    public string Model { get; set; } = "";
    public int Tokens { get; set; }
    public long LatencyMs { get; set; }
    public DateTime Timestamp { get; set; }
}

public class FallbackRecord
{
    public string FromProvider { get; set; } = "";
    public string ToProvider { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class AiUsageSummary
{
    public int TotalRequests { get; set; }
    public int TotalFallbacks { get; set; }
    public int RecentRequests { get; set; }
    public Dictionary<string, int> ProviderCounts { get; set; } = new();
    public List<string> RecentFallbacks { get; set; } = new();
}
