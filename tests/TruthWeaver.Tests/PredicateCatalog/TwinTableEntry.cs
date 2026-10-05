namespace TruthWeaver.Tests.PredicateCatalog;

/// <summary>One reviewed row of the twin table. A row accounts for every factory key it names.</summary>
internal abstract record TwinTableEntry
{
    /// <summary>Gets the factory keys this row accounts for.</summary>
    public abstract IEnumerable<string> Factories { get; }
}
