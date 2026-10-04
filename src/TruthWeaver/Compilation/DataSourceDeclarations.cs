namespace TruthWeaver.Compilation;

using System.Collections;
using TruthWeaver.Abstractions;

/// <summary>
/// The data source names a rule may use in <c>from("name", "query")</c>, declared to the compiler (ADR-0006
/// decision 4). The compiler never sees a source instance (sources arrive with each evaluation), so it checks only that
/// a name is declared; a name that is not is a <c>TRE0024</c> diagnostic, so a typo does not wait for the first evaluation.
/// A name may also be declared with an <see cref="IQueryValidator"/>, which turns a malformed query into a <c>TRE0025</c>
/// diagnostic; a name declared without one is not syntax-checked. Names are case-sensitive.
/// </summary>
public sealed class DataSourceDeclarations : IEnumerable<string>
{
    private readonly Dictionary<string, IQueryValidator?> declared = new(StringComparer.Ordinal);

    /// <summary>Gets the declared names.</summary>
    public IReadOnlyCollection<string> Names => this.declared.Keys;

    /// <summary>
    /// Gets or sets the query validator a name was declared with. Setting declares the name (or replaces its validator);
    /// setting <see langword="null"/> declares it without one.
    /// </summary>
    /// <param name="name">The name, as written in <c>from("name", ...)</c>.</param>
    /// <returns>The validator, or <see langword="null"/> when the name was declared without one or was never declared.</returns>
    public IQueryValidator? this[string name]
    {
        get => this.declared.GetValueOrDefault(name);
        set => this.declared[name] = value;
    }

    /// <summary>
    /// Declares a source name without a query validator. A validator the name already has is kept, so a bare declaration never
    /// weakens a checked one.
    /// </summary>
    /// <param name="name">The name, as written in <c>from("name", ...)</c>.</param>
    public void Add(string name)
    {
        this.declared.TryAdd(name, null);
    }

    /// <summary>Declares a source name whose queries are syntax-checked at compile time.</summary>
    /// <param name="name">The name, as written in <c>from("name", ...)</c>.</param>
    /// <param name="validator">The validator for the source's query dialect. Declaring the name again replaces its validator.</param>
    public void Add(string name, IQueryValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        this.declared[name] = validator;
    }

    /// <summary>Determines whether a source name is declared.</summary>
    /// <param name="name">The source name.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was declared.</returns>
    public bool Contains(string name)
    {
        return this.declared.ContainsKey(name);
    }

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator()
    {
        return this.declared.Keys.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
