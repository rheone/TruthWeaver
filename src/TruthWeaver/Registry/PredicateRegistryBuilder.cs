namespace TruthWeaver.Registry;

using TruthWeaver.Abstractions;
using TruthWeaver.Parsing;

/// <summary>
/// Builds a <see cref="PredicateRegistry{TContext}"/> via explicit registration — either a
/// class-based <see cref="IPredicate{TContext}"/> (resolved per-evaluation from
/// <see cref="IServiceProvider"/>) or a stateless lambda. No attribute or assembly scanning
/// (ADR-0004).
/// </summary>
/// <typeparam name="TContext">The application context type predicates read from.</typeparam>
public sealed class PredicateRegistryBuilder<TContext>
{
    private readonly Dictionary<string, PredicateDescriptor<TContext>> descriptorsByName = [];
    private readonly Dictionary<string, ICollectionBinding<TContext>> collectionsByName = [];

    /// <summary>Registers a class-based predicate, resolved from the per-evaluation <see cref="IServiceProvider"/>.</summary>
    /// <typeparam name="TPredicate">The predicate implementation type.</typeparam>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">A predicate with the same name (case-insensitive) is already registered, the name is a reserved DSL keyword, or the schema declares two arguments whose names differ only in case.</exception>
    public PredicateRegistryBuilder<TContext> Add<TPredicate>()
        where TPredicate : IPredicate<TContext>
    {
        PredicateSchema schema = TPredicate.Schema;
        this.AddDescriptor(schema.Name, new PredicateDescriptor<TContext>(schema, typeof(TPredicate)));
        return this;
    }

    /// <summary>Registers a stateless lambda predicate.</summary>
    /// <param name="schema">The predicate's schema.</param>
    /// <param name="evaluate">The stateless evaluation function.</param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentException">A predicate with the same name (case-insensitive) is already registered, the name is a reserved DSL keyword, or the schema declares two arguments whose names differ only in case.</exception>
    public PredicateRegistryBuilder<TContext> Add(
        PredicateSchema schema,
        Func<TContext, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate
    )
    {
        this.AddDescriptor(schema.Name, new PredicateDescriptor<TContext>(schema, evaluate));
        return this;
    }

    /// <summary>
    /// Registers a collection binding (ADR-0010 decision 3): a name, a typed selector from
    /// <typeparamref name="TContext"/> to the collection's elements, and a registry of element predicates over
    /// <see cref="Element{TOuter, TItem}"/> for a <c>FORALL</c>/<c>EXISTS</c> quantifier's body to resolve against.
    /// A nested collection registers on <paramref name="elements"/>, giving an outer chain like
    /// <c>Element&lt;Element&lt;Order, OrderLine&gt;, Tag&gt;</c>.
    /// </summary>
    /// <typeparam name="TItem">The collection's element type.</typeparam>
    /// <param name="name">
    /// The collection's name. Collection names are their own namespace, separate from predicate and data-source
    /// names (ADR-0010 decision 3), so this never collides with a predicate name registered on this same builder.
    /// </param>
    /// <param name="selector">
    /// Reads the collection from the outer context. Called at most once per evaluation per (binding, outer
    /// element) and its result materialized to a list (ADR-0010 decision 5): the same answer for the same input
    /// within one evaluation, the same contract a predicate's own selector already has. Returning
    /// <see langword="null"/> evaluates the quantifier as <c>Unknown</c> with no fault; throwing evaluates it as
    /// <c>Unknown</c> with one fault for the collection (ADR-0010 decision 4).
    /// </param>
    /// <param name="elements">Registers the element predicates a quantifier body over this collection may use.</param>
    /// <param name="elementSource">
    /// Projects one element to an <see cref="IDataSource"/> scoped to it (ADR-0010 decision 3), so a quantifier
    /// body may read the element untyped with <c>from(variable, "...")</c> as well as with the typed element
    /// predicates <paramref name="elements"/> registers. <see langword="null"/> (the default) when the element has
    /// no such projection; a body that still writes <c>from(variable, ...)</c> against this collection then gets
    /// the ordinary undeclared-data-source diagnostic, the same one ADR-0006 gives any other name nothing declared.
    /// </param>
    /// <returns>This builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="selector"/> or <paramref name="elements"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">A collection with the same name (case-insensitive) is already registered on this builder.</exception>
    public PredicateRegistryBuilder<TContext> AddCollection<TItem>(
        string name,
        Func<TContext, IEnumerable<TItem>?> selector,
        Action<PredicateRegistryBuilder<Element<TContext, TItem>>> elements,
        Func<TItem, IDataSource>? elementSource = null
    )
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(elements);

        PredicateRegistryBuilder<Element<TContext, TItem>> elementBuilder = new();
        elements(elementBuilder);
        PredicateRegistry<Element<TContext, TItem>> elementRegistry = elementBuilder.Build();

        CollectionBinding<TContext, TItem> binding = new(name, selector, elementRegistry, elementSource);
        string key = name.ToUpperInvariant();
        if (!this.collectionsByName.TryAdd(key, binding))
        {
            throw new ArgumentException($"A collection named '{name}' (case-insensitive) is already registered.", nameof(name));
        }

        return this;
    }

    /// <summary>Builds the immutable registry.</summary>
    /// <returns>The built registry.</returns>
    public PredicateRegistry<TContext> Build()
    {
        return new(this.descriptorsByName, this.collectionsByName);
    }

    /// <summary>
    /// Rejects a schema with two arguments whose names differ only in case. Rule text matches argument names ignoring case, so
    /// such a pair could not be told apart and a call that names either one would be ambiguous.
    /// </summary>
    private static void RejectCaseVariantArguments(string predicateName, PredicateSchema schema)
    {
        IGrouping<string, string>? clash = schema
            .Arguments.Select(a => a.Name)
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (clash is not null)
        {
            throw new ArgumentException(
                $"Predicate '{predicateName}' declares the arguments '{clash.First()}' and '{clash.Skip(1).First()}', which are the same name when case is ignored. Argument names are case-insensitive, so give them different names.",
                nameof(schema)
            );
        }
    }

    private void AddDescriptor(string name, PredicateDescriptor<TContext> descriptor)
    {
        if (DslParser.IsReservedWord(name))
        {
            throw new ArgumentException(
                $"A predicate cannot be named '{name}': the name is reserved, because rule text reads it as a keyword (the check ignores case). Choose another name.",
                nameof(name)
            );
        }

        RejectCaseVariantArguments(name, descriptor.Schema);
        string key = name.ToUpperInvariant();
        if (!this.descriptorsByName.TryAdd(key, descriptor))
        {
            throw new ArgumentException($"A predicate named '{name}' (case-insensitive) is already registered.", nameof(name));
        }
    }
}
