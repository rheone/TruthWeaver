namespace TruthWeaver.Analysis;

/// <summary>The verdict of <see cref="RuleImplication.Implies{TContext}"/>.</summary>
public enum RuleImplicationOutcome
{
    /// <summary>Every assignment of the terms that makes the first rule <c>True</c> also makes the second rule <c>True</c>.</summary>
    Holds,

    /// <summary>
    /// Some assignment makes the first rule <c>True</c> and the second rule not <c>True</c>; see
    /// <see cref="RuleImplicationResult.CounterExample"/>.
    /// </summary>
    DoesNotHold,

    /// <summary>The check was not run because the rules have more distinct terms than the configured cap.</summary>
    Undecided,
}
