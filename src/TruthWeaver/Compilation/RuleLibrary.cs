namespace TruthWeaver.Compilation;

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

/// <summary>
/// An immutable set of named rules in rule text. Rule text names a library rule with <c>rule("name")</c>, and the
/// compiler expands the library rule in place of the reference at compile time. Give the library to the compiler in
/// <see cref="CompilerOptions.Library"/>.
/// </summary>
/// <remarks>
/// A library rule is a complete rule. It takes no parameters, and it has no predicate registry and no data sources of its
/// own: the compiler resolves its predicate names and data source names against the compile that references it. A library
/// does not change after it is made. To change a rule, make a new library and a new compiler. Two libraries are equal
/// when they hold the same names (compared without case) with the same rule text, in any order, so
/// <see cref="CompilerOptions"/> that hold equal libraries are equal.
/// </remarks>
public sealed class RuleLibrary : IEquatable<RuleLibrary>
{
    private readonly Dictionary<string, LibraryRule> rules;

    /// <summary>Initializes a new instance of the <see cref="RuleLibrary"/> class.</summary>
    /// <param name="rules">
    /// The rules, as pairs of a name and its rule text. Names are compared without case, like configuration keys.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="rules"/> or a rule text is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A name is empty or white space, or two rules have the same name.</exception>
    public RuleLibrary(IEnumerable<KeyValuePair<string, string>> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        this.rules = [with(StringComparer.OrdinalIgnoreCase)];
        foreach ((string name, string text) in rules)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A library rule name is empty or white space.", nameof(rules));
            }

            if (text is null)
            {
                throw new ArgumentNullException(nameof(rules), $"The text of library rule '{name}' is null.");
            }

            if (!this.rules.TryAdd(name, new LibraryRule(name, text, ContentHash(text))))
            {
                throw new ArgumentException($"The library has two rules named '{name}'.", nameof(rules));
            }
        }
    }

    /// <summary>Gets an empty library.</summary>
    public static RuleLibrary Empty { get; } = new([]);

    /// <summary>Gets the names of the rules in the library.</summary>
    public IReadOnlyCollection<string> Names => this.rules.Keys;

    /// <summary>Gets the number of rules in the library.</summary>
    public int Count => this.rules.Count;

    /// <summary>Gets the library rule with a name.</summary>
    /// <param name="name">The rule name. The match ignores case.</param>
    /// <param name="rule">The library rule, or <see langword="null"/> when the library holds no rule with that name.</param>
    /// <returns><see langword="true"/> when the library holds the rule.</returns>
    public bool TryGet(string name, [NotNullWhen(true)] out LibraryRule? rule)
    {
        ArgumentNullException.ThrowIfNull(name);
        return this.rules.TryGetValue(name, out rule);
    }

    /// <summary>Determines whether another library holds the same rules.</summary>
    /// <param name="other">The library to compare with.</param>
    /// <returns>
    /// <see langword="true"/> when both libraries hold the same names, compared without case, and the same rule text for
    /// each name, compared exactly. The order in which the rules were given does not matter. The name case that the library
    /// was given does not matter either.
    /// </returns>
    public bool Equals([NotNullWhen(true)] RuleLibrary? other)
    {
        if (other is null || this.rules.Count != other.rules.Count)
        {
            return false;
        }

        foreach ((string name, LibraryRule rule) in this.rules)
        {
            if (
                !other.rules.TryGetValue(name, out LibraryRule? otherRule)
                || !string.Equals(rule.Text, otherRule.Text, StringComparison.Ordinal)
            )
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc cref="Equals(RuleLibrary)"/>
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as RuleLibrary);
    }

    /// <summary>Gets a hash code that agrees with <see cref="Equals(RuleLibrary)"/>.</summary>
    /// <returns>A hash code that does not depend on the order or the name case of the rules.</returns>
    public override int GetHashCode()
    {
        // A sum is order independent, as the equality is. Each rule contributes its case-insensitive name and its text.
        int sum = 0;
        foreach ((string name, LibraryRule rule) in this.rules)
        {
            sum += HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(name), rule.Text);
        }

        return sum;
    }

    /// <summary>
    /// Computes the content hash of rule text: the SHA-256 hash of its UTF-8 bytes, as 64 upper-case hexadecimal digits.
    /// Every CRLF and every lone CR is read as LF first, so the hash does not depend on the line endings of the file the
    /// text came from. Nothing else in the text changes (no trimming).
    /// </summary>
    private static string ContentHash(string text)
    {
        string normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
