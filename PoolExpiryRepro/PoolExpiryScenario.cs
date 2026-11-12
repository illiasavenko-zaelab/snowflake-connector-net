namespace PoolExpiryRepro;

// Fills the pool, lets every pooled session expire, then sends two requests and measures them.
internal sealed class PoolExpiryScenario(ReproOptions options, MockServer server, ThreadPoolAdjustments adjustments)
{
    private static readonly TimeSpan s_expiryMargin = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_secondRequestDelay = TimeSpan.FromMilliseconds(500);

    private readonly SnowflakeRequest _request = new(SnowflakeConnectionString.For(options));

    // The first request on an empty pool. It also makes the pool open MinPoolSize sessions.
    public TimeSpan WarmUp()
    {
        var elapsed = _request.StartAsync().GetAwaiter().GetResult();
        server.WaitUntilQuiet();
        return elapsed;
    }

    public CycleResult RunExpiryCycle(int cycle)
    {
        // The pool is idle here, so after this wait every pooled session is past ExpirationTimeout.
        Thread.Sleep(TimeSpan.FromSeconds(options.ExpirationTimeoutSeconds) + s_expiryMargin);

        var startedAt = Clock.Elapsed;
        var mark = adjustments.Mark();
        var probe = ThreadPoolDelayProbe.Start();

        var firstRequest = _request.StartAsync();
        Thread.Sleep(s_secondRequestDelay);
        var secondRequest = _request.StartAsync();
        Task.WaitAll(firstRequest, secondRequest);

        var longestThreadPoolDelay = probe.Stop();
        server.WaitUntilQuiet();

        return new CycleResult(
            cycle,
            startedAt,
            firstRequest.Result,
            secondRequest.Result,
            longestThreadPoolDelay,
            adjustments.SummarizeSince(mark));
    }
}

internal sealed record CycleResult(
    int Cycle,
    TimeSpan StartedAt,
    TimeSpan FirstRequest,
    TimeSpan SecondRequest,
    TimeSpan LongestThreadPoolDelay,
    string ThreadPoolAdjustments);
