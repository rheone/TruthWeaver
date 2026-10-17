namespace TruthWeaver.Extensions.Configuration.Tests;

using Microsoft.Extensions.Logging;

/// <summary>A logger provider that keeps every entry, so a test can read what the store logged.</summary>
internal sealed class ListLoggerProvider : ILoggerProvider
{
    /// <summary>Gets the logged entries: level and message.</summary>
    public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = [];

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return new ListLogger(this);
    }

    /// <summary>Creates a typed logger that writes to <see cref="Entries"/>.</summary>
    /// <typeparam name="T">The category type.</typeparam>
    /// <returns>The logger.</returns>
    public ILogger<T> CreateLogger<T>()
    {
        return new TypedLogger<T>(new ListLogger(this));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Nothing to release: the entries stay readable after disposal.
    }

    private sealed class TypedLogger<T>(ILogger inner) : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return inner.BeginScope(state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return inner.IsEnabled(logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        )
        {
            inner.Log(logLevel, eventId, state, exception, formatter);
        }
    }

    private sealed class ListLogger(ListLoggerProvider provider) : ILogger
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
            provider.Entries.Enqueue((logLevel, formatter(state, exception)));
        }
    }
}
