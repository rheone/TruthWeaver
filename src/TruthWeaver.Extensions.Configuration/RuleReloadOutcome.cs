namespace TruthWeaver.Extensions.Configuration;

/// <summary>What a reload of the configuration did to one named rule.</summary>
public enum RuleReloadOutcome
{
    /// <summary>The rule is new, or its text changed, and the new compiled rule is live.</summary>
    Loaded,

    /// <summary>The rule text did not change, so the compiled rule in the cache stays live and nothing is compiled.</summary>
    Unchanged,

    /// <summary>The rule text did not compile. The last good compiled rule stays live; if there is none, the name is missing.</summary>
    Failed,

    /// <summary>The configuration no longer holds the rule, so it is not in the store.</summary>
    Removed,
}
