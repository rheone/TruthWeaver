namespace TruthWeaver.Tests.PredicateCatalog;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Predicates;
using TruthWeaver.Registry;

/// <summary>
/// Records the trade-off of the host-wide <see cref="NullBehavior"/> default at rule level. With a null age,
/// <c>NOT (age &gt; 18)</c> is Unknown by default, so a fail-closed decision does not pass, and it is True when the host sets
/// <see cref="NullBehavior.False"/>, because a missing age then counts as "not over 18".
/// </summary>
public sealed class NullBehaviorScopeRuleTests
{
    /// <summary>By default, a null age makes <c>NOT (age &gt; 18)</c> Unknown and the decision is not satisfied.</summary>
    [Fact]
    public async Task EvaluateAsync_NotOfComparisonWithNullAgeAndNoScope_IsUnknownAndNotSatisfied_Test()
    {
        CompiledRule<Person> rule = Compile(static () => null);

        Decision decision = await rule.EvaluateAsync(
            new Person(null),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.Empty(decision.Faults);
        Assert.False(decision.IsSatisfied);
    }

    /// <summary>When the host sets <see cref="NullBehavior.False"/>, a null age makes <c>NOT (age &gt; 18)</c> True.</summary>
    [Fact]
    public async Task EvaluateAsync_NotOfComparisonWithNullAgeAndFalseScope_IsTrue_Test()
    {
        CompiledRule<Person> rule = Compile(static () => NullBehaviorScope.Begin(NullBehavior.False));

        Decision decision = await rule.EvaluateAsync(
            new Person(null),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
        Assert.True(decision.IsSatisfied);
    }

    /// <summary>A scope for another family leaves the numeric comparison at its Unknown default.</summary>
    [Fact]
    public async Task EvaluateAsync_FalseScopeForAnotherFamily_KeepsNumericUnknown_Test()
    {
        CompiledRule<Person> rule = Compile(static () => NullBehaviorScope.Begin(PredicateFamily.String, NullBehavior.False));

        Decision decision = await rule.EvaluateAsync(
            new Person(null),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
    }

    /// <summary>Compiles the rule, registering the predicate inside the scope that <paramref name="openScope"/> opens and closing the scope afterwards.</summary>
    private static CompiledRule<Person> Compile(Func<NullBehaviorScope?> openScope)
    {
        PredicateRegistryBuilder<Person> builder = PredicateRegistry<Person>.CreateBuilder();
        using (NullBehaviorScope? scope = openScope())
        {
            (PredicateSchema schema, Func<Person, PredicateArguments, CancellationToken, ValueTask<TruthValue>> evaluate) =
                NumericPredicates.IsGreaterThan<Person>("ageAbove", person => person.Age);
            builder.Add(schema, evaluate);
        }

        CompilationResult<Person> result = new RuleCompiler<Person>(builder.Build()).Compile("NOT ageAbove(value: 18)");
        Assert.True(result.Succeeded);
        return result.CompiledRule!;
    }

    private sealed record Person(long? Age);
}
