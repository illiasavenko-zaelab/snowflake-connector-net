using System.Diagnostics;
using Snowflake.Data.Client;

namespace PoolExpiryRepro;

// One application request: take a pooled connection, run a query, give the connection back.
internal sealed class SnowflakeRequest(string connectionString)
{
    // Starts the request on the thread pool, as a web server would. The result is the time from
    // the moment the request was queued, so waiting for a thread counts.
    public Task<TimeSpan> StartAsync()
    {
        var queuedAt = Stopwatch.GetTimestamp();
        return Task.Run(async () =>
        {
            await RunAsync().ConfigureAwait(false);
            return Stopwatch.GetElapsedTime(queuedAt);
        });
    }

    private async Task RunAsync()
    {
        await using var connection = new SnowflakeDbConnection(connectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = "select 1";
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
        }
    }
}
