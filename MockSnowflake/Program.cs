using MockSnowflake;

// A stand-in for the Snowflake REST API with fixed response times. Whatever a client waits
// beyond those times is spent inside the client process, not on the server or the network.
var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.ClearProviders();
if (string.IsNullOrEmpty(builder.Configuration["urls"]))
    builder.WebHost.UseUrls("http://127.0.0.1:8088");

var latencies = Latencies.From(builder.Configuration);
var requestLog = new RequestLog();

var app = builder.Build();
app.MapSnowflakeApi(latencies, requestLog);
app.MapGet("/stats", () => Results.Json(requestLog.Snapshot()));
app.MapPost("/stats/reset", () =>
{
    requestLog.Reset();
    return Results.NoContent();
});

Console.WriteLine($"Mock Snowflake started: {latencies}");
app.Run();
