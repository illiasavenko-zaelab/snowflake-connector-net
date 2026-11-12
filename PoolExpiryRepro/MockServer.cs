using System.Text.Json;

namespace PoolExpiryRepro;

// The client's view of the mock server: what the driver really sent and how fast it was answered.
// Uses synchronous HTTP calls, so that it works while the thread pool is stalled.
internal sealed class MockServer(Uri address)
{
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan s_quietPeriod = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromMilliseconds(100);

    private readonly HttpClient _http = new() { BaseAddress = address, Timeout = TimeSpan.FromSeconds(5) };

    public ServerStats GetStats()
    {
        using var response = _http.Send(new HttpRequestMessage(HttpMethod.Get, "/stats"));
        response.EnsureSuccessStatusCode();
        using var body = response.Content.ReadAsStream();
        return JsonSerializer.Deserialize<ServerStats>(body, s_json)!;
    }

    // Starts the counters from zero, so that the report covers this run only.
    public void ResetStats()
    {
        using var response = _http.Send(new HttpRequestMessage(HttpMethod.Post, "/stats/reset"));
        response.EnsureSuccessStatusCode();
    }

    public void WaitUntilReachable()
    {
        var deadline = Clock.Elapsed + TimeSpan.FromSeconds(30);
        while (true)
        {
            try
            {
                GetStats();
                return;
            }
            catch (HttpRequestException) when (Clock.Elapsed < deadline)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }
        }
    }

    // The driver has finished its background work once the server has seen nothing new for a while.
    public void WaitUntilQuiet()
    {
        var last = GetStats();
        var quietSince = Clock.Elapsed;
        while (Clock.Elapsed - quietSince < s_quietPeriod)
        {
            Thread.Sleep(s_pollInterval);
            var current = GetStats();
            if (current != last || current.InFlight > 0)
            {
                last = current;
                quietSince = Clock.Elapsed;
            }
        }
    }
}

internal sealed record RequestStats(int Count, int SlowestMs);

internal sealed record ServerStats(RequestStats Login, RequestStats Close, RequestStats Query, int InFlight);
