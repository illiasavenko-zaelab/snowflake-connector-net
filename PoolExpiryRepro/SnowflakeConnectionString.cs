namespace PoolExpiryRepro;

internal static class SnowflakeConnectionString
{
    // Plain password authentication against the mock; pooling is on by default for it.
    public static string For(ReproOptions options) =>
        $"account=repro;host={options.Server.Host};port={options.Server.Port};scheme={options.Server.Scheme};" +
        "user=repro;password=repro;db=DB;schema=SCH;warehouse=WH;role=R;" +
        $"minPoolSize={options.MinPoolSize};maxPoolSize={options.MaxPoolSize};" +
        $"expirationTimeout={options.ExpirationTimeoutSeconds};";
}
