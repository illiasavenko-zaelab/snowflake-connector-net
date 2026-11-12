using System.Collections.Concurrent;
using System.Diagnostics;

namespace MockSnowflake;

// Counts the requests that really arrived and how long the server took to answer each kind.
internal sealed class RequestLog
{
    private readonly ConcurrentDictionary<string, RequestStats> _statsByKind = new();
    private int _inFlight;

    public async Task<IResult> MeasureAsync(string kind, Func<Task<IResult>> handle)
    {
        Interlocked.Increment(ref _inFlight);
        var startedAt = Stopwatch.GetTimestamp();
        try
        {
            return await handle();
        }
        finally
        {
            var elapsedMs = (int)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            _statsByKind.AddOrUpdate(
                kind,
                new RequestStats(1, elapsedMs),
                (_, stats) => new RequestStats(stats.Count + 1, Math.Max(stats.SlowestMs, elapsedMs)));
            Interlocked.Decrement(ref _inFlight);
        }
    }

    public ServerStats Snapshot() => new(
        StatsOf(SnowflakeApi.Login),
        StatsOf(SnowflakeApi.Close),
        StatsOf(SnowflakeApi.Query),
        Volatile.Read(ref _inFlight));

    public void Reset() => _statsByKind.Clear();

    private RequestStats StatsOf(string kind) => _statsByKind.GetValueOrDefault(kind, new RequestStats(0, 0));
}

internal sealed record RequestStats(int Count, int SlowestMs);

internal sealed record ServerStats(RequestStats Login, RequestStats Close, RequestStats Query, int InFlight);
