# Session pool expiry stall in Snowflake.Data: reproduction

This shows what the first request costs after the sessions in the connection pool pass
`ExpirationTimeout`, in a process that sees one processor, such as a container limited to 0.5 CPU.

With `Snowflake.Data` 6.5.0 and `MinPoolSize=10` that request takes 3.4 to 3.7 s, and 17 to 19 s when
the application has one thread pool thread blocked. The server answers every login in 0.6 s and
every query in 0.1 s. Any other work in the process that needs a thread pool thread waits just as
long.

## What is here

| Project | Purpose |
|---|---|
| `MockSnowflake` | A stand-in for the Snowflake REST API with fixed response times: login 600 ms, session close 250 ms, query 100 ms. It counts what it received and how long it took to answer (`GET /stats`). |
| `PoolExpiryRepro` | A console client that runs the released `Snowflake.Data` package against the mock and measures. |

The client does this:

1. Sends one request, which makes the pool open `MinPoolSize` sessions.
2. Waits `ExpirationTimeout` (30 s here), so that every pooled session is expired.
3. Sends two requests 0.5 s apart and measures them. While they run it also measures how long an
   unrelated thread pool work item waits for a thread.
4. Repeats steps 2 and 3 three times.

A request is what a web application does: open a pooled connection with `await using`, run
`select 1`, dispose.

## Run it

A run takes 2 to 4 minutes. No Snowflake account is needed.

### With Docker

```bash
docker compose build
docker compose run --rm repro
docker compose run --rm repro --blocked-threads 1
docker compose down
```

`docker-compose.yml` limits the client to 0.5 CPU and 1 GiB.

### Without Docker

Needs the .NET 10 SDK. In one terminal:

```bash
dotnet run -c Release --project MockSnowflake
```

In another:

```bash
DOTNET_PROCESSOR_COUNT=1 dotnet run -c Release --project PoolExpiryRepro
DOTNET_PROCESSOR_COUNT=1 dotnet run -c Release --project PoolExpiryRepro -- --blocked-threads 1
```

`DOTNET_PROCESSOR_COUNT=1` makes the runtime size its thread pool the way it does in a container
with a CPU limit of 1 or less. The results are the same as with a real 0.5 CPU limit. In PowerShell
set `$env:DOTNET_PROCESSOR_COUNT = 1` first. This route was tested on Linux only.

## What you should see

Without a blocked thread:

```text
Snowflake.Data 6.5.0 on .NET 10.0.12, 1 processor(s), thread pool minimum 1 worker thread(s)
MinPoolSize=10, ExpirationTimeout=30 s, blocked threads=0, server=http://127.0.0.1:8088/

Warm-up request on an empty pool: 2.43 s

After each ExpirationTimeout:
cycle  started at  first request  second request  other work waited  thread pool adjustments
    1     36.78 s         3.35 s          2.85 s             3.20 s  CooperativeBlocking x19, ClimbingMove x1
    2     74.71 s         3.50 s          3.60 s             3.39 s  CooperativeBlocking x31, ClimbingMove x1
    3    112.83 s         3.74 s          3.24 s             3.59 s  CooperativeBlocking x30, ClimbingMove x2

Server side: 41 logins (slowest 603 ms), 31 closes (slowest 250 ms), 7 queries (slowest 101 ms)
```

With `--blocked-threads 1`:

```text
Snowflake.Data 6.5.0 on .NET 10.0.12, 1 processor(s), thread pool minimum 1 worker thread(s)
MinPoolSize=10, ExpirationTimeout=30 s, blocked threads=1, server=http://127.0.0.1:8088/

Warm-up request on an empty pool: 10.82 s

After each ExpirationTimeout:
cycle  started at  first request  second request  other work waited  thread pool adjustments
    1     45.19 s        17.47 s         17.57 s            17.30 s  Starvation x18
    2     98.19 s        17.11 s         17.21 s            16.96 s  Starvation x17
    3    150.84 s        19.11 s         19.21 s            18.96 s  Starvation x19

Server side: 46 logins (slowest 602 ms), 33 closes (slowest 253 ms), 7 queries (slowest 101 ms)
```

The columns:

- `first request` is the request that finds the expired pool. With a server that takes 0.6 s to log
  in and 0.1 s to run the query, 0.7 s is the cost of the work itself.
- `second request` arrives 0.5 s later.
- `other work waited` is the longest time a trivial, unrelated work item waited for a thread pool
  thread while the two requests ran.
- `thread pool adjustments` says why the runtime added worker threads. `Starvation` is the slow
  path, about one thread per second.
- `Server side` has the slowest answers as measured by the mock, so the time is lost in the client.

## What `--blocked-threads` stands for

`--blocked-threads 1` keeps one thread pool thread blocked in `Thread.Sleep` for the whole run. It
stands for application code that blocks on something other than a `Task`: a lock, a synchronous
read, a blocking collection.

The runtime adds threads quickly for threads it knows are blocked on a `Task` (the
`CooperativeBlocking` adjustments above), but only up to the pool's minimum plus the number of such
threads. A thread blocked any other way is not counted, and the pool has already grown by one
because of it. The quick path therefore always finds the thread count high enough and adds nothing,
and each further thread comes from the starvation check, about one second apart.

## Testing a driver build from source

To run the same measurement against a checkout of the connector, for example a fix:

```bash
DOTNET_PROCESSOR_COUNT=1 dotnet run -c Release --project PoolExpiryRepro \
  --property:SnowflakeDataProject=/path/to/snowflake-connector-net/Snowflake.Data/Snowflake.Data.csproj \
  --property:EnableWindowsTargeting=true -- --blocked-threads 1
```

`EnableWindowsTargeting` is needed on Linux and macOS because the connector also targets Windows.
In Docker, `SNOWFLAKE_DATA_VERSION=<version> docker compose build` selects another released version.

## Options

| Option | Default | Meaning |
|---|---|---|
| `--server <url>` | `http://127.0.0.1:8088` | Where the mock listens. |
| `--min-pool-size <n>` | 10 | `MinPoolSize` in the connection string. |
| `--max-pool-size <n>` | 20 | `MaxPoolSize` in the connection string. |
| `--expiration-timeout <seconds>` | 30 | `ExpirationTimeout` in the connection string. |
| `--cycles <n>` | 3 | How many times the pool is left to expire and measured. |
| `--blocked-threads <n>` | 0 | Thread pool threads kept blocked for the whole run. |
| `--driver-log <file>` | off | Writes the driver's DEBUG log to this file when the run ends. |

The mock takes `--LoginMs`, `--CloseMs` and `--QueryMs`.

## Source layout

`MockSnowflake`

| File | Responsibility |
|---|---|
| `Program.cs` | Starts the web server. |
| `SnowflakeApi.cs` | The login, close and query endpoints. |
| `Responses.cs` | Response bodies with the fields the driver reads. |
| `Latencies.cs` | The fixed response times. |
| `RequestLog.cs` | Counts requests and the slowest answer per kind. |

`PoolExpiryRepro`

| File | Responsibility |
|---|---|
| `Program.cs` | Wires the parts together and runs the cycles. |
| `ReproOptions.cs` | Command line options. |
| `SnowflakeConnectionString.cs` | The connection string for the mock. |
| `SnowflakeRequest.cs` | One request: open, query, dispose. |
| `PoolExpiryScenario.cs` | Warm-up, wait for expiry, measure. |
| `ThreadPoolDelayProbe.cs` | How long unrelated work waits for a thread. |
| `ThreadPoolAdjustments.cs` | Listens to the runtime's thread pool events. |
| `BlockedWorkerThreads.cs` | The `--blocked-threads` option. |
| `MockServer.cs` | Reads the mock's `/stats` and waits until the driver has gone quiet. |
| `DriverDebugLog.cs` | Collects the driver's DEBUG log. |
| `Report.cs` | Prints the results. |
| `Clock.cs` | Time since the process started, shared by the report and the log. |
