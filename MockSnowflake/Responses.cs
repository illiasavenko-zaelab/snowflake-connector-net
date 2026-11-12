namespace MockSnowflake;

// Response bodies with just the fields the driver reads.
internal static class Responses
{
    private static long s_lastSessionId = 1000;

    private static readonly object[] s_sessionParameters =
    [
        new { name = "CLIENT_SESSION_KEEP_ALIVE", value = (object)false },
        new { name = "CLIENT_TELEMETRY_ENABLED", value = (object)false },
        new { name = "CLIENT_PREFETCH_THREADS", value = (object)4 },
        new { name = "CLIENT_RESULT_CHUNK_SIZE", value = (object)16 },
        new { name = "QUERY_CONTEXT_CACHE_SIZE", value = (object)5 },
        new { name = "AUTOCOMMIT", value = (object)true },
    ];

    public static object Login()
    {
        var sessionId = Interlocked.Increment(ref s_lastSessionId);
        return Success(new
        {
            sessionId,
            token = $"session-token-{sessionId}",
            masterToken = $"master-token-{sessionId}",
            validityInSeconds = 3600,
            masterValidityInSeconds = 14400,
            serverVersion = "9.99.0",
            parameters = s_sessionParameters,
            sessionInfo = new { databaseName = "DB", schemaName = "SCH", warehouseName = "WH", roleName = "R" },
        });
    }

    public static object SelectOne() => Success(new
    {
        queryId = Guid.NewGuid().ToString(),
        rowtype = new[] { new { name = "1", type = "FIXED", nullable = false, precision = 1, scale = 0, length = (int?)null } },
        rowset = new[] { new[] { "1" } },
        total = 1,
        returned = 1,
        parameters = Array.Empty<object>(),
        queryResultFormat = "json",
        statementTypeId = 4096,
        finalDatabaseName = "DB",
        finalSchemaName = "SCH",
        finalWarehouseName = "WH",
        finalRoleName = "R",
    });

    public static object Ok() => Success(null);

    private static object Success(object? data) => new { data, code = (string?)null, message = (string?)null, success = true };
}
