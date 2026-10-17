namespace TruthWeaver.Registry;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Where predicate implementations are registered under a name, with their argument schema
/// (CONTEXT.md). Immutable once built; registration is exclusively through
/// <see cref="PredicateRegistryBuilder{TContext}"/> — there is no attribute or assembly-scanning
/// discovery path (ADR-0004).
/// </summary>
/// <typeparam name="TContext">The application context type predicates in this registry read from.</typeparam>
public sealed class PredicateRegistry<TContext>
{
    private static readonly IReadOnlyDictionary<string, ICollectionBinding<TContext>> EmptyCollections =
        new Dictionary<string, ICollectionBinding<TContext>>();

    private readonly IReadOnlyDictionary<string, PredicateDescriptor<TContext>> descriptorsByName;
    private readonly IReadOnlyDictionary<string, ICollectionBinding<TContext>> collectionsByName;

    internal PredicateRegistry(
        IReadOnlyDictionary<string, PredicateDescriptor<TContext>> descriptorsByName,
        IReadOnlyDictionary<string, ICollectionBinding<TContext>>? collectionsByName = null
    )
    {
        this.descriptorsByName = descriptorsByName;
        this.collectionsByName = collectionsByName ?? EmptyCollections;
    }

    /// <summary>
    /// Gets the schema of every registered predicate, in no guaranteed order. Use it to list what a host offers
    /// (for example in a rule-authoring UI); <see cref="PredicateSchema.Name"/> is the registered casing.
    /// </summary>
    public IEnumerable<PredicateSchema> Schemas => this.descriptorsByName.Values.Select(d => d.Schema);

    /// <summary>Gets the registered predicate names as they were registered, the candidates for a "did you mean" suggestion.</summary>
    internal IEnumerable<string> Names => this.descriptorsByName.Values.Select(d => d.Schema.Name);

    /// <summary>Gets the registered collection names as they were registered, the candidates for a "did you mean" suggestion.</summary>
    internal IEnumerable<string> CollectionNames => this.collectionsByName.Values.Select(c => c.Name);

    /// <summary>Creates a builder for constructing a new registry.</summary>
    /// <returns>A new, empty builder.</returns>
    public static PredicateRegistryBuilder<TContext> CreateBuilder()
    {
        return new();
    }

    /// <summary>
    /// Looks up a predicate's schema by name, case-insensitively — the label/description source for a
    /// <see cref="Ast.TermExpression"/> node, since a term itself carries only its resolved identity,
    /// not the descriptive metadata authored on its predicate's registration.
    /// </summary>
    /// <param name="name">The predicate name as written in rule text.</param>
    /// <param name="schema">The matching schema, if found.</param>
    /// <returns><see langword="true"/> if a predicate with this name (case-insensitive) is registered.</returns>
    public bool TryGetSchema(string name, [NotNullWhen(true)] out PredicateSchema? schema)
    {
        if (this.TryGet(name, out PredicateDescriptor<TContext>? descriptor))
        {
            schema = descriptor.Schema;
            return true;
        }

        schema = null;
        return false;
    }

    /// <summary>Looks up a predicate by name, case-insensitively.</summary>
    /// <param name="name">The predicate name as written in rule text.</param>
    /// <param name="descriptor">The matching descriptor, if found.</param>
    /// <returns><see langword="true"/> if a predicate with this name (case-insensitive) is registered.</returns>
    internal bool TryGet(string name, [NotNullWhen(true)] out PredicateDescriptor<TContext>? descriptor)
    {
        return this.descriptorsByName.TryGetValue(name.ToUpperInvariant(), out descriptor);
    }

    /// <summary>
    /// Looks up a collection binding by name, case-insensitively (ADR-0010 decision 3: collection names are a
    /// namespace of their own, separate from predicate and data-source names).
    /// </summary>
    /// <param name="name">The collection name as written at a quantifier's head.</param>
    /// <param name="binding">The matching binding, if found.</param>
    /// <returns><see langword="true"/> if a collection with this name (case-insensitive) is registered.</returns>
    internal bool TryGetCollection(string name, [NotNullWhen(true)] out ICollectionBinding<TContext>? binding)
    {
        return this.collectionsByName.TryGetValue(name.ToUpperInvariant(), out binding);
    }
}
