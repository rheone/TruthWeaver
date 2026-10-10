namespace TruthWeaver.Predicates;

using System.Text.RegularExpressions;

/// <summary>
/// A process-wide cache of compiled <see cref="Regex"/> instances for one set of <see cref="RegexOptions"/>, with a
/// fixed upper bound on the number of cached patterns.
/// </summary>
/// <remarks>
/// A pattern that a data source supplies is not known at compile time, so a host that evaluates many distinct patterns
/// would grow an unbounded cache without limit. When the cache holds <see cref="Capacity"/> patterns, the next new pattern
/// clears it first. That keeps the cost of the bound to one dictionary clear and keeps hot patterns cheap, because they are
/// compiled again once. Concurrent callers may overshoot the bound by at most the number of concurrent callers.
/// </remarks>
/// <param name="options">The options every cached pattern is compiled with.</param>
/// <param name="timeout">The match timeout of every cached pattern.</param>
/// <param name="capacity">The number of patterns the cache holds before it clears itself. At least one.</param>
internal sealed class RegexPatternCache(RegexOptions options, TimeSpan timeout, int capacity)
{
    private readonly ConcurrentDictionary<string, Regex> entries = new(StringComparer.Ordinal);

    /// <summary>Gets the number of patterns the cache holds before it clears itself.</summary>
    public int Capacity { get; } = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));

    /// <summary>Gets the number of patterns the cache holds now.</summary>
    public int Count => this.entries.Count;

    /// <summary>
    /// Checks that a pattern is a valid regular expression under this cache's options, without caching it. Compile-time
    /// validation uses this, so a rule that is compiled but never evaluated takes no cache slot.
    /// </summary>
    /// <param name="pattern">The regular-expression pattern.</param>
    /// <exception cref="ArgumentException">The pattern is not a valid regular expression.</exception>
    public void Validate(string pattern)
    {
        _ = new Regex(pattern, options, timeout);
    }

    /// <summary>Gets the compiled <see cref="Regex"/> for a pattern, compiling and caching it on first use.</summary>
    /// <param name="pattern">The regular-expression pattern, exactly as supplied in rule text.</param>
    /// <returns>The compiled <see cref="Regex"/>.</returns>
    /// <exception cref="ArgumentException">The pattern is not a valid regular expression. Nothing is cached.</exception>
    public Regex Get(string pattern)
    {
        if (this.entries.TryGetValue(pattern, out Regex? cached))
        {
            return cached;
        }

        // The constructor throws for an invalid pattern, so an invalid pattern never takes a slot.
        Regex created = new(pattern, options, timeout);
        if (this.entries.Count >= this.Capacity)
        {
            this.entries.Clear();
        }

        this.entries[pattern] = created;
        return created;
    }
}
