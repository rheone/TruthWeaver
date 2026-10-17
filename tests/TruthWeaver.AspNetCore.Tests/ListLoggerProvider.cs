namespace TruthWeaver.AspNetCore.Tests;

using Microsoft.Extensions.Logging;

/// <summary>A logger provider that keeps every entry, so a test can read what the handler logged.</summary>
internal sealed class ListLoggerProvider : ILoggerProvider
{
    /// <summary>Gets the logged entries: category, level, event name and message.</summary>
    public ConcurrentQueue<(string Category, LogLevel Level, string? EventName, string Message)> Entries { get; } = [];

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return new ListLogger(this, categoryName);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to release: the entries stay readable after the container is disposed.
    }

    private sealed class ListLogger(ListLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            provider.Entries.Enqueue((category, logLevel, eventId.Name, formatter(state, exception)));
        }
    }
}
