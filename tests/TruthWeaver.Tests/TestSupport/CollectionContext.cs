namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Registry;

/// <summary>
/// An application context that holds the collections a quantifier ranges over. A <see langword="null"/> collection, at
/// the top level or inside a group, is the case the quantifier tests need to build.
/// </summary>
public sealed class CollectionContext
{
    private static readonly PredicateSchema ElementTrue = PredicateSchema.NoArguments(
        "elementTrue",
        "elementTrue",
        "True iff the element itself is True."
    );

    /// <summary>Gets the collection named <c>items</c>, or <see langword="null"/> for none.</summary>
    public IReadOnlyList<TruthValue>? Items { get; init; }

    /// <summary>Gets the collection named <c>groups</c>; each group holds the collection named <c>members</c>.</summary>
    public IReadOnlyList<IReadOnlyList<TruthValue>?>? Groups { get; init; }

    /// <summary>
    /// Creates a compiler for rules over <see cref="CollectionContext"/>. The root has the constant <c>a</c> and the
    /// collections <c>items</c> and <c>groups</c>; a group has the constant <c>groupOk</c> and the collection
    /// <c>members</c>; every element has the predicate <c>elementTrue</c>, which returns the element itself.
    /// </summary>
    /// <param name="options">The compiler options, or <see langword="null"/> for the defaults.</param>
    /// <returns>The compiler.</returns>
    public static RuleCompiler<CollectionContext> CreateCompiler(CompilerOptions? options = null)
    {
        PredicateRegistryBuilder<CollectionContext> builder = PredicateRegistry<CollectionContext>
            .CreateBuilder()
            .Add(PredicateSchema.NoArguments("a", "a", "Always true."), (_, _, _) => ValueTask.FromResult(TruthValue.True))
            .AddCollection(
                "items",
                context => context.Items,
                elements => elements.Add(ElementTrue, (element, _, _) => ValueTask.FromResult(element.Item))
            )
            .AddCollection(
                "groups",
                context => context.Groups,
                groupElements =>
                    groupElements
                        .Add(
                            PredicateSchema.NoArguments("groupOk", "groupOk", "Always true."),
                            (_, _, _) => ValueTask.FromResult(TruthValue.True)
                        )
                        .AddCollection(
                            "members",
                            group => group.Item,
                            memberElements =>
                                memberElements.Add(ElementTrue, (element, _, _) => ValueTask.FromResult(element.Item))
                        )
            );
        return new RuleCompiler<CollectionContext>(builder.Build(), options);
    }
}
