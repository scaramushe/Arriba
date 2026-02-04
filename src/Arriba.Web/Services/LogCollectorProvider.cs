using Microsoft.Extensions.Logging;

namespace Arriba.Web.Services;

public class LogCollectorProvider : ILoggerProvider
{
    private readonly ILogCollector _logCollector;

    public LogCollectorProvider(ILogCollector logCollector)
    {
        _logCollector = logCollector;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new LogCollectorLogger(categoryName, _logCollector);
    }

    public void Dispose()
    {
    }
}

public class LogCollectorLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ILogCollector _logCollector;

    public LogCollectorLogger(string categoryName, ILogCollector logCollector)
    {
        _categoryName = categoryName;
        _logCollector = logCollector;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        
        _logCollector.AddLog(new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel.ToString(),
            Message = $"[{_categoryName}] {message}",
            Exception = exception?.ToString()
        });
    }
}
