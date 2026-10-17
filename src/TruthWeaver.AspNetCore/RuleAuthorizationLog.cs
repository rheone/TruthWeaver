namespace TruthWeaver.AspNetCore;

using Microsoft.Extensions.Logging;

/// <summary>Structured log events of the rule authorization handler.</summary>
internal static partial class RuleAuthorizationLog
{
    /// <summary>Logs a rule requirement that denied access.</summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="requirement">The name of the requirement.</param>
    /// <param name="result">The result of the rule.</param>
    /// <param name="faultCount">The number of faults in the decision.</param>
    [LoggerMessage(
        EventId = 1,
        EventName = "RuleRequirementDenied",
        Level = LogLevel.Information,
        Message = "Rule requirement {Requirement} denied access: the result was {Result}, with {FaultCount} fault(s)"
    )]
    public static partial void RequirementDenied(ILogger logger, string requirement, TruthValue result, int faultCount);
}
