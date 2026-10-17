namespace TruthWeaver.Extensions.Configuration;

using Microsoft.Extensions.Logging;

/// <summary>Source-generated log messages of the rule store.</summary>
internal static partial class RuleStoreLog
{
    /// <summary>Logs that a rule compiled and is now live.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="ruleName">The rule name.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Rule '{RuleName}' compiled and is live.")]
    public static partial void RuleLoaded(ILogger logger, string ruleName);

    /// <summary>Logs that a rule did not compile and the previous rule, if any, stays live.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="hasLastGoodRule">Whether a previous compiled rule stays live.</param>
    /// <param name="diagnostics">The formatted compiler diagnostics.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Rule '{RuleName}' did not compile. Last good rule kept: {HasLastGoodRule}. Diagnostics: {Diagnostics}"
    )]
    public static partial void RuleFailed(ILogger logger, string ruleName, bool hasLastGoodRule, string diagnostics);

    /// <summary>Logs that a rule is no longer in the configuration and was removed from the store.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="ruleName">The rule name.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Information,
        Message = "Rule '{RuleName}' is no longer configured and was removed."
    )]
    public static partial void RuleRemoved(ILogger logger, string ruleName);

    /// <summary>Logs that an evaluation asked for a rule that the store does not hold. The result is Unknown, which is not satisfied.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="ruleName">The rule name.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Rule '{RuleName}' is not in the store. The result is Unknown (fail closed)."
    )]
    public static partial void RuleMissing(ILogger logger, string ruleName);
}
