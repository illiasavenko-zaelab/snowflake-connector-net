using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace PoolExpiryRepro;

// Records why the runtime changed the number of thread pool worker threads.
// "Starvation" is the slow path: one more thread only after queued work made no progress.
internal sealed class ThreadPoolAdjustments : EventListener
{
    private const string RuntimeEventSource = "Microsoft-Windows-DotNETRuntime";
    private const EventKeywords ThreadingKeyword = (EventKeywords)0x10000;
    private const string AdjustmentEvent = "ThreadPoolWorkerThreadAdjustmentAdjustment";

    private static readonly string[] s_reasons =
    [
        "Warmup", "Initializing", "RandomMove", "ClimbingMove", "ChangePoint",
        "Stabilizing", "Starvation", "ThreadTimedOut", "CooperativeBlocking",
    ];

    private readonly ConcurrentQueue<string> _reasons = new();

    // A position in the recorded history, to summarize from later.
    public int Mark() => _reasons.Count;

    public string SummarizeSince(int mark)
    {
        var counts = _reasons.Skip(mark)
            .GroupBy(reason => reason)
            .OrderByDescending(group => group.Count())
            .Select(group => $"{group.Key} x{group.Count()}");
        var summary = string.Join(", ", counts);
        return summary.Length == 0 ? "none" : summary;
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name == RuntimeEventSource)
            EnableEvents(eventSource, EventLevel.Informational, ThreadingKeyword);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        if (eventData.EventName != AdjustmentEvent || eventData.Payload == null || eventData.PayloadNames == null)
            return;

        var reason = Convert.ToInt32(eventData.Payload[eventData.PayloadNames.IndexOf("Reason")]);
        _reasons.Enqueue(reason < s_reasons.Length ? s_reasons[reason] : $"Reason{reason}");
    }
}
