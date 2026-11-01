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
/// does not change after it is made. To change a rule, make a new library and a new compiler.
/// </remarks>
public sealed class RuleLibrary
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

    /// <summary>
    /// Computes the content hash of rule text: the SHA-256 hash of its UTF-8 bytes, as 64 upper-case hexadecimal digits.
    /// The rule store of <c>TruthWeaver.Extensions.Configuration</c> computes the same hash for the rules it holds.
    /// </summary>
    private static string ContentHash(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }
}
