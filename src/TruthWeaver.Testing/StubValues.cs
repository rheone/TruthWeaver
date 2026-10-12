namespace TruthWeaver.Testing;

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Stub values: one truth value for each term of a rule, in place of the predicates of a host. It is also the context type
/// of a rule that <see cref="StubRuleCompiler"/> compiles, so the stand-in predicates of that rule read their answers from it.
/// </summary>
/// <remarks>
/// <para>A key is one term in rule text, such as <c>isAdmin</c> or <c>hasRole(role: "admin")</c>. The key is stored as the
/// canonical text of the term, so <c>hasRole( role:"admin" )</c> and <c>hasRole(role: "admin")</c> are the same key. The
/// predicate and argument names keep the case they are written in, and a stand-in finds a value only by the same spelling as
/// the rule.</para>
/// <para>A key cannot hold a <c>from(...)</c> reference: a stand-in looks up the term with the value that the data source
/// gives, such as <c>inRegion(region: "EU")</c> for <c>inRegion(region: from("user", "$.region"))</c>. A term with no value is
/// <see cref="TruthValue.Unknown"/>.</para>
/// </remarks>
public sealed class StubValues
{
    // Compiles a key as rule text. It accepts any predicate and argument (an empty registry in lenient mode), because the
    // key only has to be one term, not a term of some host.
    private static readonly RuleCompiler<StubValues> TermCompiler = new(
        PredicateRegistry<StubValues>.CreateBuilder().Build(),
        new CompilerOptions(Mode: CompilationMode.Lenient)
    );

    private StubValues(FrozenDictionary<string, TruthValue> values)
    {
        this.Values = values;
    }

    /// <summary>Gets stub values with no entry, so every term is <see cref="TruthValue.Unknown"/>.</summary>
    public static StubValues Empty { get; } = new([]);

    /// <summary>Gets the value of each term, keyed by the canonical text of the term.</summary>
    public IReadOnlyDictionary<string, TruthValue> Values { get; }

    /// <summary>Creates stub values from terms in rule text and their values.</summary>
    /// <param name="values">Each term and its value.</param>
    /// <param name="stubs">The stub values when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="errors">One message for each key that is not exactly one term, has a <c>from(...)</c> reference or names a term a second time; empty on success.</param>
    /// <returns><see langword="true"/> when every key is usable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    public static bool TryCreate(
        IEnumerable<KeyValuePair<string, TruthValue>> values,
        [NotNullWhen(true)] out StubValues? stubs,
        out IReadOnlyList<string> errors
    )
    {
        ArgumentNullException.ThrowIfNull(values);

        List<string> problems = [];
        Dictionary<string, TruthValue> canonical = [with(StringComparer.Ordinal)];
        foreach ((string key, TruthValue value) in values)
        {
            if (!TryNormalizeTerm(key, out string? term, out string? error))
            {
                problems.Add(error);
            }
            else if (!canonical.TryAdd(term, value))
            {
                problems.Add($"The term '{key}' names the term '{term}' a second time.");
            }
        }

        errors = problems;
        stubs = problems.Count == 0 ? new StubValues(canonical.ToFrozenDictionary(StringComparer.Ordinal)) : null;
        return stubs is not null;
    }

    /// <summary>Creates stub values from terms in rule text and their values.</summary>
    /// <param name="values">Each term and its value.</param>
    /// <returns>The stub values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A key is not usable. The message lists each problem, as <see cref="TryCreate"/> gives them.</exception>
    public static StubValues Create(IEnumerable<KeyValuePair<string, TruthValue>> values)
    {
        return TryCreate(values, out StubValues? stubs, out IReadOnlyList<string> errors)
            ? stubs
            : throw new ArgumentException(string.Join("\n", errors), nameof(values));
    }

    /// <summary>Gives the canonical text of one term in rule text, the form of a key in <see cref="Values"/>.</summary>
    /// <param name="term">The term, such as <c>hasRole( role:"admin" )</c>.</param>
    /// <param name="canonical">The canonical text, such as <c>hasRole(role: "admin")</c>, when this method returns <see langword="true"/>.</param>
    /// <param name="error">What is wrong, when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="term"/> is exactly one term with no <c>from(...)</c> reference.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="term"/> is <see langword="null"/>.</exception>
    public static bool TryNormalizeTerm(
        string term,
        [NotNullWhen(true)] out string? canonical,
        [NotNullWhen(false)] out string? error
    )
    {
        ArgumentNullException.ThrowIfNull(term);

        if (
            !TermCompiler.TryCompile(term, out CompiledRule<StubValues>? rule, out _)
            || rule.Metrics is not { NodeCount: 1, DistinctTermCount: 1 }
        )
        {
            canonical = null;
            error = $"'{term}' is not exactly one term, such as isAdmin or hasRole(role: \"admin\").";
            return false;
        }

        // The JSON tree shows a from(...) argument as an object, and a literal as a value or a list.
        JsonObject arguments = JsonNode.Parse(rule.PrintJson(includeNotes: false))?["args"] as JsonObject ?? [];
        if (arguments.Any(argument => argument.Value is JsonObject))
        {
            canonical = null;
            error =
                $"'{term}' has a from(...) reference. Give the term with the value that the data gives, such as inRegion(region: \"EU\").";
            return false;
        }

        canonical = rule.CanonicalText;
        error = null;
        return true;
    }

    /// <summary>Gets the value of a term, or <see cref="TruthValue.Unknown"/> when the term has no entry.</summary>
    /// <param name="term">The canonical text of the term.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="term"/> is <see langword="null"/>.</exception>
    public TruthValue ValueOf(string term)
    {
        ArgumentNullException.ThrowIfNull(term);

        return this.Values.TryGetValue(term, out TruthValue value) ? value : TruthValue.Unknown;
    }
}
