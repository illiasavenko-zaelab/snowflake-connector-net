namespace MockSnowflake;

// Fixed server-side response times in milliseconds, so that runs are comparable.
// Override with --LoginMs, --CloseMs, --QueryMs or environment variables of the same name.
internal sealed record Latencies(int LoginMs, int CloseMs, int QueryMs)
{
    public static Latencies From(IConfiguration configuration) => new(
        configuration.GetValue("LoginMs", 600),
        configuration.GetValue("CloseMs", 250),
        configuration.GetValue("QueryMs", 100));
}
