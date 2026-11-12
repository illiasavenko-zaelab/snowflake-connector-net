using System.Reflection;
using System.Runtime.InteropServices;
using Snowflake.Data.Client;

namespace PoolExpiryRepro;

internal static class Report
{
    public static void PrintSetup(ReproOptions options)
    {
        ThreadPool.GetMinThreads(out var minWorkerThreads, out _);
        Console.WriteLine($"Snowflake.Data {DriverVersion()} on {RuntimeInformation.FrameworkDescription}, " +
                          $"{Environment.ProcessorCount} processor(s), thread pool minimum {minWorkerThreads} worker thread(s)");
        Console.WriteLine($"MinPoolSize={options.MinPoolSize}, ExpirationTimeout={options.ExpirationTimeoutSeconds} s, " +
                          $"blocked threads={options.BlockedThreads}, server={options.Server}");
        Console.WriteLine();
    }

    public static void PrintWarmUp(TimeSpan firstRequest)
    {
        Console.WriteLine($"Warm-up request on an empty pool: {Seconds(firstRequest)}");
        Console.WriteLine();
        Console.WriteLine("After each ExpirationTimeout:");
        Console.WriteLine("cycle  started at  first request  second request  other work waited  thread pool adjustments");
    }

    public static void PrintCycle(CycleResult result)
    {
        Console.WriteLine($"{result.Cycle,5}  {Seconds(result.StartedAt),10}  {Seconds(result.FirstRequest),13}  " +
                          $"{Seconds(result.SecondRequest),14}  {Seconds(result.LongestThreadPoolDelay),17}  {result.ThreadPoolAdjustments}");
    }

    public static void PrintServerSide(ServerStats stats)
    {
        Console.WriteLine();
        Console.WriteLine($"Server side: {stats.Login.Count} logins (slowest {stats.Login.SlowestMs} ms), " +
                          $"{stats.Close.Count} closes (slowest {stats.Close.SlowestMs} ms), " +
                          $"{stats.Query.Count} queries (slowest {stats.Query.SlowestMs} ms)");
    }

    private static string Seconds(TimeSpan time) => $"{time.TotalSeconds:F2} s";

    private static string DriverVersion()
    {
        var assembly = typeof(SnowflakeDbConnection).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return (informational ?? assembly.GetName().Version?.ToString() ?? "unknown").Split('+')[0];
    }
}
