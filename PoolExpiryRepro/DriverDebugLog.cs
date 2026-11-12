using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;

namespace PoolExpiryRepro;

// Receives the driver's DEBUG log. Lines are kept in memory and written when the run ends,
// so that logging adds no file I/O to the measurement.
internal sealed class DriverDebugLog : ILogger, IDisposable
{
    private readonly string _file;
    private readonly ConcurrentQueue<string> _lines = new();

    private DriverDebugLog(string file)
    {
        _file = file;
    }

    public static DriverDebugLog? Start(string? file)
    {
        if (file == null)
            return null;

        var log = new DriverDebugLog(file);
        SnowflakeDbLoggerConfig.SetCustomLogger(log);
        return log;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var line = $"{Clock.Elapsed.TotalSeconds,8:F3} s  thread {Environment.CurrentManagedThreadId,3}  {logLevel,-11}  {formatter(state, exception)}";
        _lines.Enqueue(exception == null ? line : $"{line}  {exception.GetType().Name}: {exception.Message}");
    }

    public void Dispose()
    {
        SnowflakeDbLoggerConfig.ResetCustomLogger();
        File.WriteAllLines(_file, _lines);
    }
}
