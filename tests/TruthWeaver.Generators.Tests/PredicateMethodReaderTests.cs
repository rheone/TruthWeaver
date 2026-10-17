namespace TruthWeaver.Generators.Tests;

using TruthWeaver.Generators.Tests.TestSupport;

/// <summary>Drives the generator over method shapes that the reader reads: return shapes, parameters, defaults and documentation.</summary>
public sealed class PredicateMethodReaderTests
{
    /// <summary>
    /// A method that returns <c>ValueTask&lt;TruthValue&gt;</c> is called as it is, and a method that returns
    /// <c>Task&lt;TruthValue&gt;</c> is wrapped; a cancellation token parameter takes the token of the call.
    /// </summary>
    [Fact]
    public void Generator_AsyncReturnShapesWithCancellationToken_EmitRegisterThatCompiles_Test()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                [Predicate("fromValueTask", "From value task", "Returns a value task.")]
                public static ValueTask<TruthValue> FromValueTask(Account account, CancellationToken token)
                {
                    return ValueTask.FromResult(TruthValue.True);
                }

                [Predicate("fromTask", "From task", "Returns a task.")]
                public static Task<TruthValue> FromTask(Account account, CancellationToken token)
                {
                    return Task.FromResult(TruthValue.True);
                }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilerErrors);
        Assert.Contains("FromValueTask(context, cancellationToken)", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("new global::System.Threading.Tasks.ValueTask<", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("FromTask(context, cancellationToken)", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>The generator adds <c>Register</c> to a record, a struct, a record struct and an interface with the matching keyword.</summary>
    /// <param name="declaration">The partial type declaration, with its keyword.</param>
    /// <param name="keyword">The keyword that the generated partial declaration uses.</param>
    [Theory]
    [InlineData("public partial record Predicates", "partial record Predicates")]
    [InlineData("public partial struct Predicates", "partial struct Predicates")]
    [InlineData("public partial record struct Predicates", "partial record struct Predicates")]
    [InlineData("public partial interface Predicates", "partial interface Predicates")]
    public void Generator_DeclaringTypeKind_EmitsPartialDeclarationWithTheSameKeyword_Test(string declaration, string keyword)
    {
        string source = $$"""
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            {{declaration}}
            {
                [Predicate("isActive", "Is active", "Is the account active?")]
                public static TruthValue IsActive(Account account) { return TruthValue.True; }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilerErrors);
        Assert.Contains(keyword, run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>Scalar and list arguments without a default are required and read through the getter of their kind.</summary>
    [Fact]
    public void Generator_RequiredArgumentsOfEveryKind_EmitSchemaAndGetters_Test()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                [Predicate("everyKind", "Every kind", "Takes every kind of argument.")]
                public static TruthValue EveryKind(
                    Account account,
                    string text,
                    long whole,
                    decimal fraction,
                    bool flag,
                    DateTimeOffset moment,
                    Guid id,
                    IReadOnlyList<string> names,
                    IReadOnlyList<long> counts,
                    IReadOnlyList<decimal> fractions,
                    IReadOnlyList<bool> flags,
                    IReadOnlyList<DateTimeOffset> moments,
                    IReadOnlyList<Guid> ids)
                {
                    return TruthValue.True;
                }

                [Predicate("withDefaults", "With defaults", "Takes arguments with defaults.")]
                public static TruthValue WithDefaults(
                    Account account,
                    string text = "a\"b",
                    long whole = 5,
                    decimal fraction = 1.5m,
                    bool flag = true,
                    bool off = false)
                {
                    return TruthValue.True;
                }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilerErrors);
        Assert.Contains("GetInt64Array(\"counts\")", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("GetBool(\"flag\")", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("LiteralKind.GuidArray, true, null)", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("OfString(\"a\\\"b\")", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("OfInt64(5L)", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("OfDecimal(1.5m)", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("OfBoolean(true)", run.GeneratedSource, StringComparison.Ordinal);
        Assert.Contains("OfBoolean(false)", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>The <c>param</c> documentation of a method becomes the description of its argument, with references flattened to text.</summary>
    [Fact]
    public void Generator_ParamDocumentation_BecomesTheArgumentDescription_Test()
    {
        const string source = """
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                /// <summary>Checks the orders.</summary>
                /// <param name="account">The account.</param>
                /// <param name="minimum">
                /// The smallest count, as a <see cref="long"/>; use
                /// <see langword="true"/> for any, and <paramref name="minimum"/> or <c>zero</c> otherwise.
                /// </param>
                [Predicate("hasOrders", "Has orders", "Has the account enough orders?")]
                public static TruthValue HasOrders(Account account, long minimum) { return TruthValue.True; }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilerErrors);
        Assert.Contains(
            "\"The smallest count, as a Int64; use true for any, and minimum or zero otherwise.\"",
            run.GeneratedSource,
            StringComparison.Ordinal
        );
    }

    /// <summary>Documentation that is not well-formed XML gives the arguments empty descriptions and no generator diagnostic.</summary>
    [Fact]
    public void Generator_MalformedDocumentation_GivesEmptyDescriptions_Test()
    {
        const string source = """
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                /// <summary>Checks the orders.
                /// <param name="minimum">The smallest count.
                [Predicate("hasOrders", "Has orders", "Has the account enough orders?")]
                public static TruthValue HasOrders(Account account, long minimum) { return TruthValue.True; }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilerErrors);
        Assert.Contains("PredicateArgumentSchema(\"minimum\", \"\"", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// A context type that is generic over an internal type is not public, so the generated <c>Register</c> overload is
    /// internal and the generated code compiles.
    /// </summary>
    [Fact]
    public void Generator_ContextGenericOverInternalType_EmitsInternalRegister_Test()
    {
        const string source = """
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Box<T>(T Value);

            internal sealed record Secret;

            public static partial class BoxPredicates
            {
                [Predicate("isFull", "Is full", "Is the box full?")]
                internal static TruthValue IsFull(Box<Secret> box) { return TruthValue.True; }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilerErrors);
        Assert.Contains(
            "internal static global::TruthWeaver.Registry.PredicateRegistryBuilder<",
            run.GeneratedSource,
            StringComparison.Ordinal
        );
    }
}
