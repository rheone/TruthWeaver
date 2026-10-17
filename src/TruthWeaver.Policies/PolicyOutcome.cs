namespace TruthWeaver.Policies;

/// <summary>The outcome of a <see cref="PolicySet{TContext}"/> for one context.</summary>
public enum PolicyOutcome
{
    /// <summary>A policy that permits applies and the algorithm lets it win.</summary>
    Permit,

    /// <summary>A policy that denies applies and the algorithm lets it win.</summary>
    Deny,

    /// <summary>No policy applies, or the target of the set does not match.</summary>
    NotApplicable,

    /// <summary>Whether a policy applies is <see cref="TruthValue.Unknown"/>, and the answer could change the outcome.</summary>
    Indeterminate,
}
