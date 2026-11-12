namespace PoolExpiryRepro;

internal sealed record ReproOptions
{
    public const string Usage =
        "Options: --server <url> --min-pool-size <n> --max-pool-size <n> --expiration-timeout <seconds> " +
        "--cycles <n> --blocked-threads <n> --driver-log <file>";

    public Uri Server { get; private init; } = new(Environment.GetEnvironmentVariable("MOCK_SNOWFLAKE_URL") ?? "http://127.0.0.1:8088");

    public int MinPoolSize { get; private init; } = 10;

    public int MaxPoolSize { get; private init; } = 20;

    public int ExpirationTimeoutSeconds { get; private init; } = 30;

    // How many times the pool is left to expire and then hit by a request.
    public int Cycles { get; private init; } = 3;

    // See BlockedWorkerThreads.
    public int BlockedThreads { get; private init; }

    // When set, the driver's DEBUG log is written to this file.
    public string? DriverLogFile { get; private init; }

    public static ReproOptions Parse(string[] args)
    {
        var options = new ReproOptions();
        for (var i = 0; i < args.Length; i += 2)
        {
            var name = args[i];
            var value = i + 1 < args.Length ? args[i + 1] : throw new ArgumentException($"Missing value for {name}. {Usage}");
            options = name switch
            {
                "--server" => options with { Server = new Uri(value) },
                "--min-pool-size" => options with { MinPoolSize = int.Parse(value) },
                "--max-pool-size" => options with { MaxPoolSize = int.Parse(value) },
                "--expiration-timeout" => options with { ExpirationTimeoutSeconds = int.Parse(value) },
                "--cycles" => options with { Cycles = int.Parse(value) },
                "--blocked-threads" => options with { BlockedThreads = int.Parse(value) },
                "--driver-log" => options with { DriverLogFile = value },
                _ => throw new ArgumentException($"Unknown option {name}. {Usage}"),
            };
        }

        return options;
    }
}
