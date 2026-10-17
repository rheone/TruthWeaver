namespace TruthWeaver.Policies;

/// <summary>What one policy answered for a context.</summary>
/// <param name="PolicyId">The identifier of the policy.</param>
/// <param name="Effect">What the policy asks for when it applies.</param>
/// <param name="Applies">
/// Whether the policy applies: the Strong Kleene AND of its target and its condition.
/// <see cref="TruthValue.True"/> applies, <see cref="TruthValue.False"/> does not and <see cref="TruthValue.Unknown"/>
/// is not known.
/// </param>
public sealed record PolicyResult(string PolicyId, PolicyEffect Effect, TruthValue Applies);
