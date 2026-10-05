namespace TruthWeaver.Predicates;

using TruthWeaver.Abstractions;

/// <summary>
/// Describes one scalar value kind to the shared predicate builders: the literal kinds of its single
/// and array arguments, how to read them from <see cref="PredicateArguments"/>, and the noun used in schema text.
/// </summary>
/// <typeparam name="T">The value type of the kind.</typeparam>
/// <param name="Kind">The literal kind of a single-value argument.</param>
/// <param name="ArrayKind">The literal kind of a candidate-array argument.</param>
/// <param name="Noun">The noun used in schema descriptions, for example <c>integer</c>.</param>
/// <param name="Get">Reads a single-value argument.</param>
/// <param name="GetArray">Reads a candidate-array argument.</param>
/// <param name="Format">
/// Formats a value for an exception message, or <see langword="null"/> to use the value's default string form.
/// </param>
internal sealed record ScalarKind<T>(
    LiteralKind Kind,
    LiteralKind ArrayKind,
    string Noun,
    Func<PredicateArguments, string, T> Get,
    Func<PredicateArguments, string, IReadOnlyList<T>> GetArray,
    Func<T, string>? Format = null
)
    where T : struct, IComparable<T>, IEquatable<T>;
