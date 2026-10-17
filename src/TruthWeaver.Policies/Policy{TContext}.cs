namespace TruthWeaver.Policies;

using TruthWeaver.Evaluation;

/// <summary>
/// One policy: a <see cref="PolicyEffect"/> that applies when its <see cref="Target"/> and its <see cref="Condition"/>
/// are both true. Immutable.
/// </summary>
/// <typeparam name="TContext">The context type of the rules.</typeparam>
public sealed class Policy<TContext>
{
    /// <summary>Initializes a new instance of the <see cref="Policy{TContext}"/> class.</summary>
    /// <param name="id">The identifier of the policy, unique in its set. It names the policy in a <see cref="PolicyResult"/>.</param>
    /// <param name="effect">What the policy asks for when it applies.</param>
    /// <param name="condition">The rule that must be <see cref="TruthValue.True"/> for the policy to apply.</param>
    /// <param name="target">
    /// A rule that selects the requests that the policy is for, or <see langword="null"/> (the default) for every request.
    /// When the target is <see cref="TruthValue.False"/> the policy is not applicable and the condition is not evaluated.
    /// </param>
    /// <param name="priority">The evaluation order: a higher value is evaluated first, and equal values keep their order in the set. The default is 0.</param>
    /// <exception cref="ArgumentException"><paramref name="id"/> is empty or white space, or <paramref name="effect"/> is not defined.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="condition"/> is <see langword="null"/>.</exception>
    public Policy(
        string id,
        PolicyEffect effect,
        CompiledRule<TContext> condition,
        CompiledRule<TContext>? target = null,
        int priority = 0
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(condition);
        if (!Enum.IsDefined(effect))
        {
            throw new ArgumentException("The effect is not a defined PolicyEffect.", nameof(effect));
        }

        this.Id = id;
        this.Effect = effect;
        this.Condition = condition;
        this.Target = target;
        this.Priority = priority;
    }

    /// <summary>Gets the identifier of the policy.</summary>
    public string Id { get; }

    /// <summary>Gets what the policy asks for when it applies.</summary>
    public PolicyEffect Effect { get; }

    /// <summary>Gets the rule that must be true for the policy to apply.</summary>
    public CompiledRule<TContext> Condition { get; }

    /// <summary>Gets the rule that selects the requests of the policy, or <see langword="null"/> for every request.</summary>
    public CompiledRule<TContext>? Target { get; }

    /// <summary>Gets the evaluation priority. A higher value is evaluated first.</summary>
    public int Priority { get; }
}
