namespace TruthWeaver.Registry;

/// <summary>
/// The context an element predicate of a collection binding evaluates against (ADR-0010 decision 3): the outer
/// context the collection came from, the element itself, and its position in the materialized list. A host
/// registers element predicates over this type with <see cref="PredicateRegistryBuilder{TContext}.AddCollection{TItem}"/>;
/// a nested collection registers on the element builder, so an outer chain is typed
/// (<c>Element&lt;Element&lt;Order, OrderLine&gt;, Tag&gt;</c>).
/// </summary>
/// <typeparam name="TOuter">The context type the collection was selected from.</typeparam>
/// <typeparam name="TItem">The collection's element type.</typeparam>
public sealed class Element<TOuter, TItem>
{
    internal Element(TOuter outer, TItem item, int index)
    {
        this.Outer = outer;
        this.Item = item;
        this.Index = index;
    }

    /// <summary>Gets the outer context the collection was selected from.</summary>
    public TOuter Outer { get; }

    /// <summary>Gets this element's own value.</summary>
    public TItem Item { get; }

    /// <summary>Gets this element's position in the collection as materialized for this evaluation (0-based).</summary>
    public int Index { get; }
}
