namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Ticket 09: duplicate predicate name registration is rejected, case-insensitively, for both registration
/// overloads. Ticket 32: a predicate or collection name that is not a valid DSL identifier, or is a reserved
/// word, is rejected at registration too.
/// </summary>
public sealed class PredicateRegistryBuilderTests
{
    /// <summary>Registering two class based predicates under the same name throws.</summary>
    [Fact]
    public void Register_TwoClassPredicatesSameName_Throws_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add<ScopedFlagPredicate>();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.Add<ScopedFlagPredicate>());

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Registering two lambda predicates under the same name throws.</summary>
    [Fact]
    public void Register_TwoLambdaPredicatesSameName_Throws_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("alwaysTrue", value: true);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.AddConstant("alwaysTrue", value: false));

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>Registering the same name with different casing is treated as a collision.</summary>
    [Fact]
    public void Register_SameNameDifferentCasing_IsCollision_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("Foo", value: true);

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.AddConstant("foo", value: false));

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A predicate named like a DSL keyword is rejected at registration, whatever its case, and the message names the
    /// word. The rule text could never call such a predicate, because the parser reads the word as the keyword.
    /// </summary>
    [Theory]
    [InlineData("any")]
    [InlineData("Between")]
    [InlineData("IF")]
    [InlineData("exactly")]
    [InlineData("None")]
    [InlineData("T")]
    [InlineData("f")]
    [InlineData("U")]
    public void Registering_a_predicate_named_like_a_keyword_throws_naming_the_word_Test(string name)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.AddConstant(name, value: true));

        Assert.Contains($"'{name}'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("reserved", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A predicate named with something the DSL's identifier grammar forbids (a hyphen, a dot, a space, or an empty
    /// or digit-led name) is rejected at registration. Without this check, the name would compile from JSON/YAML,
    /// but <c>CompiledRule.PrintRuleText()</c> would emit it verbatim and re-parsing would split it into unrelated
    /// tokens (e.g. "has-role" re-reads as "has - role"), breaking RuleFormat's round-trip guarantee.
    /// </summary>
    [Theory]
    [InlineData("has-role")]
    [InlineData("has.role")]
    [InlineData("has role")]
    [InlineData("")]
    [InlineData("1role")]
    public void Registering_a_predicate_with_a_non_identifier_name_throws_Test(string name)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => builder.AddConstant(name, value: true));

        Assert.Contains("identifier", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A name that does match the DSL's identifier grammar is registered without error.</summary>
    [Fact]
    public void Registering_a_predicate_with_a_valid_identifier_name_succeeds_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("has_Role2", value: true);

        Assert.NotNull(builder.Build());
    }

    /// <summary>
    /// A collection named with something the DSL's identifier grammar forbids is rejected at registration, the same
    /// way a predicate name is: <c>AddCollection</c> previously had no name check at all.
    /// </summary>
    [Theory]
    [InlineData("has-items")]
    [InlineData("")]
    public void Registering_a_collection_with_a_non_identifier_name_throws_Test(string name)
    {
        PredicateRegistryBuilder<CollectionContext> builder = PredicateRegistry<CollectionContext>.CreateBuilder();

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            builder.AddCollection<TruthValue>(name, ctx => ctx.Items, _ => { })
        );

        Assert.Contains("identifier", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>A collection named like a DSL keyword is rejected at registration, the same way a predicate name is.</summary>
    [Theory]
    [InlineData("and")]
    [InlineData("FORALL")]
    public void Registering_a_collection_named_like_a_keyword_throws_naming_the_word_Test(string name)
    {
        PredicateRegistryBuilder<CollectionContext> builder = PredicateRegistry<CollectionContext>.CreateBuilder();

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            builder.AddCollection<TruthValue>(name, ctx => ctx.Items, _ => { })
        );

        Assert.Contains($"'{name}'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("reserved", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A collection named with a valid, non-reserved identifier is registered without error.</summary>
    [Fact]
    public void Registering_a_collection_with_a_valid_identifier_name_succeeds_Test()
    {
        PredicateRegistryBuilder<CollectionContext> builder = PredicateRegistry<CollectionContext>
            .CreateBuilder()
            .AddCollection<TruthValue>("items", ctx => ctx.Items, _ => { });

        Assert.NotNull(builder.Build());
    }

    /// <summary>
    /// Registering a lambda predicate with a null evaluation delegate throws <see cref="ArgumentNullException"/> at the
    /// call, so the wiring error surfaces at startup and not as a fault on every evaluation.
    /// </summary>
    [Fact]
    public void Add_NullEvaluate_ThrowsArgumentNullException_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        PredicateSchema schema = new("isReady", "Is Ready", "A test predicate.", []);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => builder.Add(schema, null!));

        Assert.Equal("evaluate", exception.ParamName);
    }

    /// <summary>Registering a lambda predicate with a null schema throws <see cref="ArgumentNullException"/> at the call.</summary>
    [Fact]
    public void Add_NullSchema_ThrowsArgumentNullException_Test()
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            builder.Add(null!, (_, _, _) => ValueTask.FromResult(TruthValue.True))
        );

        Assert.Equal("schema", exception.ParamName);
    }

    private sealed class CollectionContext
    {
        public List<TruthValue> Items { get; init; } = [];
    }
}
