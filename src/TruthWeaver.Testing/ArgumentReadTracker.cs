namespace TruthWeaver.Testing;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Abstractions;

/// <summary>
/// The argument dictionary behind a harness <see cref="PredicateArguments"/>. It records every name the predicate looks
/// up, found or not, so the schema conformance check can see unread and undeclared arguments without a change to
/// <see cref="PredicateArguments"/>.
/// </summary>
/// <param name="values">The argument values for one call.</param>
/// <param name="reads">The set that collects the looked-up names. One set is shared by every call of a run.</param>
internal sealed class ArgumentReadTracker(IReadOnlyDictionary<string, LiteralValue> values, ISet<string> reads)
    : IReadOnlyDictionary<string, LiteralValue>
{
    /// <inheritdoc />
    public int Count => values.Count;

    /// <inheritdoc />
    public IEnumerable<string> Keys => values.Keys;

    /// <inheritdoc />
    public IEnumerable<LiteralValue> Values => values.Values;

    /// <inheritdoc />
    public LiteralValue this[string key]
    {
        get
        {
            reads.Add(key);
            return values[key];
        }
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        reads.Add(key);
        return values.ContainsKey(key);
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out LiteralValue value)
    {
        reads.Add(key);
        return values.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, LiteralValue>> GetEnumerator()
    {
        return values.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
