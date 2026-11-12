using System.Diagnostics;

namespace PoolExpiryRepro;

// While running, queues a trivial work item every 50 ms and records how long it waited for a
// thread. That wait is what every other request in the same process experiences.
internal sealed class ThreadPoolDelayProbe
{
    private static readonly TimeSpan s_interval = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan s_drainTimeout = TimeSpan.FromSeconds(30);

    private readonly Thread _thread;
    private volatile bool _stopRequested;
    private long _longestDelayTicks;
    private int _pending;

    private ThreadPoolDelayProbe()
    {
        _thread = new Thread(QueueWorkItemsUntilStopped) { IsBackground = true, Name = "thread-pool-delay-probe" };
    }

    public static ThreadPoolDelayProbe Start()
    {
        var probe = new ThreadPoolDelayProbe();
        probe._thread.Start();
        return probe;
    }

    // Stops queuing, waits for the queued items to run and returns the longest wait seen.
    public TimeSpan Stop()
    {
        _stopRequested = true;
        _thread.Join();

        var deadline = Clock.Elapsed + s_drainTimeout;
        while (Volatile.Read(ref _pending) > 0 && Clock.Elapsed < deadline)
            Thread.Sleep(TimeSpan.FromMilliseconds(10));

        return TimeSpan.FromSeconds((double)Interlocked.Read(ref _longestDelayTicks) / Stopwatch.Frequency);
    }

    private void QueueWorkItemsUntilStopped()
    {
        while (!_stopRequested)
        {
            var queuedAt = Stopwatch.GetTimestamp();
            Interlocked.Increment(ref _pending);
            ThreadPool.UnsafeQueueUserWorkItem(_ => RecordDelay(queuedAt), null);
            Thread.Sleep(s_interval);
        }
    }

    private void RecordDelay(long queuedAt)
    {
        var delay = Stopwatch.GetTimestamp() - queuedAt;
        long longest;
        while (delay > (longest = Interlocked.Read(ref _longestDelayTicks)) &&
               Interlocked.CompareExchange(ref _longestDelayTicks, delay, longest) != longest)
        {
        }

        Interlocked.Decrement(ref _pending);
    }
}
