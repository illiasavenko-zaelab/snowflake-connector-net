namespace PoolExpiryRepro;

// Keeps thread pool threads blocked for the lifetime of the process, the way application code does
// when it waits on something other than a Task: a lock, a synchronous read, Thread.Sleep.
// The runtime does not count such a thread as blocked, so the pool has already grown past its
// minimum when the driver's burst arrives, and each further thread is added about one second apart.
internal static class BlockedWorkerThreads
{
    public static void Park(int count)
    {
        for (var i = 0; i < count; i++)
            Task.Run(() => Thread.Sleep(Timeout.Infinite));
    }
}
