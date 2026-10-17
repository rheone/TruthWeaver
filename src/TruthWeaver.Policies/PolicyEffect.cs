namespace TruthWeaver.Policies;

/// <summary>What a <see cref="Policy{TContext}"/> asks for when it applies.</summary>
public enum PolicyEffect
{
    /// <summary>The policy permits the request when it applies.</summary>
    Permit,

    /// <summary>The policy denies the request when it applies.</summary>
    Deny,
}
