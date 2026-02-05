using System.Collections.Concurrent;

namespace Arriba.Web.Services;

public interface ILogCollector
{
    void AddLog(LogEntry entry);
    IEnumerable<LogEntry> GetRecentLogs(int count = 100);
}

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}

public class InMemoryLogCollector : ILogCollector
{
    private readonly ConcurrentQueue<LogEntry> _logs = new();
    private const int MaxLogCount = 500;

    public void AddLog(LogEntry entry)
    {
        _logs.Enqueue(entry);
        
        // Keep only the most recent logs
        while (_logs.Count > MaxLogCount)
        {
            _logs.TryDequeue(out _);
        }
    }

    public IEnumerable<LogEntry> GetRecentLogs(int count = 100)
    {
        var snapshot = _logs.ToArray();
        return snapshot.TakeLast(Math.Min(count, snapshot.Length));
    }
}
