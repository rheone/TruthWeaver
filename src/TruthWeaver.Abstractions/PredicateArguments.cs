namespace TruthWeaver.Abstractions;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// A small, non-generic accessor a predicate uses to read its own arguments inside
/// <see cref="IPredicate{TContext}.EvaluateAsync"/>. Argument presence and type are validated
/// against the predicate's <see cref="PredicateSchema"/> at compile time (ADR-0002), so a missing or
/// mistyped argument is a compile diagnostic, never a runtime failure here — these accessors throw
/// only if a predicate asks for an argument name or type its own schema never declared, which is an
/// authoring bug in the predicate itself, not a rule-authoring error.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="PredicateArguments"/> class.</remarks>
/// <param name="values">The argument values, keyed by name (ordinal, case-sensitive — argument names in a schema are exact).</param>
public sealed class PredicateArguments(IReadOnlyDictionary<string, LiteralValue> values)
{
    private readonly IReadOnlyDictionary<string, LiteralValue> values = values;

    /// <summary>Gets an empty argument set, for zero-argument terms.</summary>
    public static PredicateArguments Empty { get; } = new(new Dictionary<string, LiteralValue>());

    /// <summary>Gets a <see cref="string"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The string value.</returns>
    public string GetString(string name)
    {
        return this.Get(name, LiteralKind.String).AsString();
    }

    /// <summary>Gets a <see cref="long"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The integer value.</returns>
    public long GetInt64(string name)
    {
        return this.Get(name, LiteralKind.Int64).AsInt64();
    }

    /// <summary>Gets a <see cref="decimal"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The decimal value.</returns>
    public decimal GetDecimal(string name)
    {
        return this.Get(name, LiteralKind.Decimal).AsDecimal();
    }

    /// <summary>Gets a <see cref="bool"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The boolean value.</returns>
    public bool GetBool(string name)
    {
        return this.Get(name, LiteralKind.Boolean).AsBoolean();
    }

    /// <summary>Gets a <see cref="DateTimeOffset"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The date/time value.</returns>
    public DateTimeOffset GetDateTimeOffset(string name)
    {
        return this.Get(name, LiteralKind.DateTimeOffset).AsDateTimeOffset();
    }

    /// <summary>Gets a <see cref="Guid"/> argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The GUID value.</returns>
    public Guid GetGuid(string name)
    {
        return this.Get(name, LiteralKind.Guid).AsGuid();
    }

    /// <summary>Gets a <see cref="string"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<string> GetStringArray(string name)
    {
        return [.. this.Get(name, LiteralKind.StringArray).AsArray().Select(v => v.AsString())];
    }

    /// <summary>Gets a <see cref="long"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<long> GetInt64Array(string name)
    {
        return [.. this.Get(name, LiteralKind.Int64Array).AsArray().Select(v => v.AsInt64())];
    }

    /// <summary>Gets a <see cref="decimal"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<decimal> GetDecimalArray(string name)
    {
        return [.. this.Get(name, LiteralKind.DecimalArray).AsArray().Select(v => v.AsDecimal())];
    }

    /// <summary>Gets a <see cref="bool"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<bool> GetBoolArray(string name)
    {
        return [.. this.Get(name, LiteralKind.BooleanArray).AsArray().Select(v => v.AsBoolean())];
    }

    /// <summary>Gets a <see cref="DateTimeOffset"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<DateTimeOffset> GetDateTimeOffsetArray(string name)
    {
        return [.. this.Get(name, LiteralKind.DateTimeOffsetArray).AsArray().Select(v => v.AsDateTimeOffset())];
    }

    /// <summary>Gets a <see cref="Guid"/> array argument.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The array elements.</returns>
    public IReadOnlyList<Guid> GetGuidArray(string name)
    {
        return [.. this.Get(name, LiteralKind.GuidArray).AsArray().Select(v => v.AsGuid())];
    }

    /// <summary>Gets the raw literal for an argument when it was supplied.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The literal when found; otherwise the default.</param>
    /// <returns><see langword="true"/> when an argument named <paramref name="name"/> was supplied. Unlike <see cref="GetRaw"/>, never throws.</returns>
    public bool TryGetRaw(string name, out LiteralValue value)
    {
        return this.values.TryGetValue(name, out value);
    }

    /// <summary>Gets a <see cref="string"/> argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The value when found; otherwise the default.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.String"/>. Unlike the throwing getter, never throws, so it suits optional or mixed-kind arguments.</returns>
    public bool TryGetString(string name, [NotNullWhen(true)] out string? value)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal))
        {
            return literal.TryAsString(out value);
        }

        value = default;
        return false;
    }

    /// <summary>Gets a <see cref="long"/> argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The value when found; otherwise the default.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.Int64"/>. Unlike the throwing getter, never throws, so it suits optional or mixed-kind arguments.</returns>
    public bool TryGetInt64(string name, out long value)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal))
        {
            return literal.TryAsInt64(out value);
        }

        value = default;
        return false;
    }

    /// <summary>Gets a <see cref="decimal"/> argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The value when found; otherwise the default.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.Decimal"/>. Unlike the throwing getter, never throws, so it suits optional or mixed-kind arguments.</returns>
    public bool TryGetDecimal(string name, out decimal value)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal))
        {
            return literal.TryAsDecimal(out value);
        }

        value = default;
        return false;
    }

    /// <summary>Gets a <see cref="bool"/> argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The value when found; otherwise the default.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.Boolean"/>. Unlike the throwing getter, never throws, so it suits optional or mixed-kind arguments.</returns>
    public bool TryGetBool(string name, out bool value)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal))
        {
            return literal.TryAsBoolean(out value);
        }

        value = default;
        return false;
    }

    /// <summary>Gets a <see cref="DateTimeOffset"/> argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The value when found; otherwise the default.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.DateTimeOffset"/>. Unlike the throwing getter, never throws, so it suits optional or mixed-kind arguments.</returns>
    public bool TryGetDateTimeOffset(string name, out DateTimeOffset value)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal))
        {
            return literal.TryAsDateTimeOffset(out value);
        }

        value = default;
        return false;
    }

    /// <summary>Gets a <see cref="Guid"/> argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="value">The value when found; otherwise the default.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.Guid"/>. Unlike the throwing getter, never throws, so it suits optional or mixed-kind arguments.</returns>
    public bool TryGetGuid(string name, out Guid value)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal))
        {
            return literal.TryAsGuid(out value);
        }

        value = Guid.Empty;
        return false;
    }

    /// <summary>Gets a <see cref="string"/> array argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="values">The array elements when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.StringArray"/>. Unlike the throwing getter, never throws.</returns>
    public bool TryGetStringArray(string name, [NotNullWhen(true)] out IReadOnlyList<string>? values)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal) && literal.Kind == LiteralKind.StringArray)
        {
            values = this.GetStringArray(name);
            return true;
        }

        values = null;
        return false;
    }

    /// <summary>Gets a <see cref="long"/> array argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="values">The array elements when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.Int64Array"/>. Unlike the throwing getter, never throws.</returns>
    public bool TryGetInt64Array(string name, [NotNullWhen(true)] out IReadOnlyList<long>? values)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal) && literal.Kind == LiteralKind.Int64Array)
        {
            values = this.GetInt64Array(name);
            return true;
        }

        values = null;
        return false;
    }

    /// <summary>Gets a <see cref="decimal"/> array argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="values">The array elements when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.DecimalArray"/>. Unlike the throwing getter, never throws.</returns>
    public bool TryGetDecimalArray(string name, [NotNullWhen(true)] out IReadOnlyList<decimal>? values)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal) && literal.Kind == LiteralKind.DecimalArray)
        {
            values = this.GetDecimalArray(name);
            return true;
        }

        values = null;
        return false;
    }

    /// <summary>Gets a <see cref="bool"/> array argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="values">The array elements when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.BooleanArray"/>. Unlike the throwing getter, never throws.</returns>
    public bool TryGetBoolArray(string name, [NotNullWhen(true)] out IReadOnlyList<bool>? values)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal) && literal.Kind == LiteralKind.BooleanArray)
        {
            values = this.GetBoolArray(name);
            return true;
        }

        values = null;
        return false;
    }

    /// <summary>Gets a <see cref="DateTimeOffset"/> array argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="values">The array elements when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.DateTimeOffsetArray"/>. Unlike the throwing getter, never throws.</returns>
    public bool TryGetDateTimeOffsetArray(string name, [NotNullWhen(true)] out IReadOnlyList<DateTimeOffset>? values)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal) && literal.Kind == LiteralKind.DateTimeOffsetArray)
        {
            values = this.GetDateTimeOffsetArray(name);
            return true;
        }

        values = null;
        return false;
    }

    /// <summary>Gets a <see cref="Guid"/> array argument when it is present and of that kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <param name="values">The array elements when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the argument exists and has kind <see cref="LiteralKind.GuidArray"/>. Unlike the throwing getter, never throws.</returns>
    public bool TryGetGuidArray(string name, [NotNullWhen(true)] out IReadOnlyList<Guid>? values)
    {
        if (this.values.TryGetValue(name, out LiteralValue literal) && literal.Kind == LiteralKind.GuidArray)
        {
            values = this.GetGuidArray(name);
            return true;
        }

        values = null;
        return false;
    }

    /// <summary>Gets the raw literal value for an argument, regardless of kind.</summary>
    /// <param name="name">The argument name.</param>
    /// <returns>The literal value.</returns>
    /// <exception cref="KeyNotFoundException">No argument named <paramref name="name"/> was supplied.</exception>
    public LiteralValue GetRaw(string name)
    {
        return this.values.TryGetValue(name, out LiteralValue value)
            ? value
            : throw new KeyNotFoundException($"No argument named '{name}' was supplied to this term.");
    }

    private LiteralValue Get(string name, LiteralKind expectedKind)
    {
        LiteralValue value = this.GetRaw(name);
        if (value.Kind != expectedKind)
        {
            throw new InvalidOperationException(
                $"Argument '{name}' is of kind '{value.Kind}', not the requested '{expectedKind}'."
            );
        }

        return value;
    }
}
