using PoolExpiryRepro;

// Measures what the first request after the session pool's ExpirationTimeout costs.
// The main thread drives the scenario with blocking calls on purpose: it must not depend on
// the thread pool it is observing.
var options = ReproOptions.Parse(args);
using var driverLog = DriverDebugLog.Start(options.DriverLogFile);

var server = new MockServer(options.Server);
server.WaitUntilReachable();
server.ResetStats();

var adjustments = new ThreadPoolAdjustments();
BlockedWorkerThreads.Park(options.BlockedThreads);
Report.PrintSetup(options);

var scenario = new PoolExpiryScenario(options, server, adjustments);
Report.PrintWarmUp(scenario.WarmUp());
for (var cycle = 1; cycle <= options.Cycles; cycle++)
    Report.PrintCycle(scenario.RunExpiryCycle(cycle));
Report.PrintServerSide(server.GetStats());
