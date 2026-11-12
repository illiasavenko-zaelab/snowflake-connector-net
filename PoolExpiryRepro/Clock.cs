using System.Diagnostics;

namespace PoolExpiryRepro;

// Time since the process started; the report and the driver log share it.
internal static class Clock
{
    private static readonly Stopwatch s_stopwatch = Stopwatch.StartNew();

    public static TimeSpan Elapsed => s_stopwatch.Elapsed;
}
