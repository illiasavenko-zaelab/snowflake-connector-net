namespace MockSnowflake;

// The three endpoints the driver needs to log in, run a query and close a session.
internal static class SnowflakeApi
{
    public const string Login = "login";
    public const string Close = "close";
    public const string Query = "query";

    public static void MapSnowflakeApi(this WebApplication app, Latencies latencies, RequestLog requestLog)
    {
        app.MapPost("/session/v1/login-request", () => Answer(Login, latencies.LoginMs, Responses.Login));
        app.MapPost("/session", () => Answer(Close, latencies.CloseMs, Responses.Ok)); // POST /session?delete=true
        app.MapPost("/queries/v1/query-request", () => Answer(Query, latencies.QueryMs, Responses.SelectOne));

        app.MapFallback((HttpContext context) =>
        {
            Console.WriteLine($"Unexpected request: {context.Request.Method} {context.Request.Path}");
            return Results.Json(Responses.Ok());
        });

        Task<IResult> Answer(string kind, int delayMs, Func<object> body) =>
            requestLog.MeasureAsync(kind, async () =>
            {
                await Task.Delay(delayMs);
                return Results.Json(body());
            });
    }
}
