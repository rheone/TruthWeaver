namespace TruthWeaver.Policies;

/// <summary>
/// How a <see cref="PolicySet{TContext}"/> turns the results of its policies into one outcome. Every algorithm treats a
/// policy that is <see cref="TruthValue.Unknown"/> as a policy that may apply: it never becomes a silent permit
/// (ADR-0015).
/// </summary>
public enum CombiningAlgorithm
{
    /// <summary>
    /// A definite deny wins. Without one, a policy that is Unknown and would deny makes the set
    /// <see cref="PolicyOutcome.Indeterminate"/>. Only then does a permit win.
    /// </summary>
    DenyOverrides,

    /// <summary>
    /// A definite permit wins. Without one, a policy that is Unknown and would permit makes the set
    /// <see cref="PolicyOutcome.Indeterminate"/>. Only then does a deny win.
    /// </summary>
    PermitOverrides,

    /// <summary>
    /// The first policy, in priority order, that is not <see cref="PolicyOutcome.NotApplicable"/> decides. A policy that
    /// is Unknown stops the walk with <see cref="PolicyOutcome.Indeterminate"/>, because a later policy could be
    /// shadowed by it.
    /// </summary>
    FirstApplicable,
}
