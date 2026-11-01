namespace TruthWeaver.Logging;

using Microsoft.Extensions.Logging;
using TruthWeaver.Evaluation;

/// <summary>Structured log events for absorbed evaluation faults.</summary>
internal static partial class EvaluationLog
{
    /// <summary>Logs one absorbed fault of a predicate, a collection selector or an element-evaluation limit.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="origin">What faulted; it leads the rendered message.</param>
    /// <param name="term">The faulting term's canonical identity text.</param>
    /// <param name="message">The exception's message.</param>
    /// <param name="exception">The exception that caused the fault.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "{Origin} fault for term {Term}: {Message}",
        SkipEnabledCheck = false
    )]
    public static partial void Faulted(ILogger logger, FaultOrigin origin, string term, string message, Exception exception);
}
