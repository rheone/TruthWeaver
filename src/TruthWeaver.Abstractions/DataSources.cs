namespace TruthWeaver.Abstractions;

using System.Collections;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The named <see cref="IDataSource"/> instances supplied to one evaluation (ADR-0006 decision 3). Names are
/// case-sensitive and must match the names declared to the compiler. Build one per request; it is not thread-safe
/// to modify while an evaluation is reading it.
/// </summary>
public sealed class DataSources : IEnumerable<KeyValuePair<string, IDataSource>>
{
    private readonly Dictionary<string, IDataSource> sources = new(StringComparer.Ordinal);

    /// <summary>Gets the number of sources.</summary>
    public int Count => this.sources.Count;

    /// <summary>Gets or sets the source registered under <paramref name="name"/>.</summary>
    /// <param name="name">The source name, as written in <c>from("name", ...)</c>.</param>
    /// <returns>The source.</returns>
    /// <exception cref="KeyNotFoundException">Getting a name that was not supplied.</exception>
    public IDataSource this[string name]
    {
        get => this.sources[name];
        set => this.sources[name] = value;
    }

    /// <summary>Adds a source.</summary>
    /// <param name="name">The source name.</param>
    /// <param name="source">The source.</param>
    /// <exception cref="ArgumentException">A source is already registered under <paramref name="name"/>.</exception>
    public void Add(string name, IDataSource source)
    {
        this.sources.Add(name, source);
    }

    /// <summary>Looks up a source by name.</summary>
    /// <param name="name">The source name.</param>
    /// <param name="source">The source, when found.</param>
    /// <returns><see langword="true"/> if a source is registered under <paramref name="name"/>.</returns>
    public bool TryGet(string name, [NotNullWhen(true)] out IDataSource? source)
    {
        return this.sources.TryGetValue(name, out source);
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, IDataSource>> GetEnumerator()
    {
        return this.sources.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
