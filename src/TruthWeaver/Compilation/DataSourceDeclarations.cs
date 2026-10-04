namespace TruthWeaver.Compilation;

using System.Collections;

/// <summary>
/// The data source names a rule may use in <c>from("name", "query")</c>, declared to the compiler (ADR-0006
/// decision 4). The compiler never sees a source instance (sources arrive with each evaluation), so it checks only that
/// a name is declared; a name that is not is a <c>TRE0024</c> diagnostic, so a typo does not wait for the first evaluation.
/// Names are case-sensitive.
/// </summary>
public sealed class DataSourceDeclarations : IEnumerable<string>
{
    private readonly HashSet<string> names = new(StringComparer.Ordinal);

    /// <summary>Gets the declared names.</summary>
    public IReadOnlyCollection<string> Names => this.names;

    /// <summary>Declares a source name.</summary>
    /// <param name="name">The name, as written in <c>from("name", ...)</c>.</param>
    public void Add(string name)
    {
        this.names.Add(name);
    }

    /// <summary>Determines whether a source name is declared.</summary>
    /// <param name="name">The source name.</param>
    /// <returns><see langword="true"/> if <paramref name="name"/> was declared.</returns>
    public bool Contains(string name)
    {
        return this.names.Contains(name);
    }

    /// <inheritdoc />
    public IEnumerator<string> GetEnumerator()
    {
        return this.names.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
