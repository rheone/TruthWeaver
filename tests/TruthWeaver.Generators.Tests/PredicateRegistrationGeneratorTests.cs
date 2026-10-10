namespace TruthWeaver.Generators.Tests;

using Microsoft.CodeAnalysis;
using TruthWeaver.Generators.Tests.TestSupport;

/// <summary>Drives the generator in memory over consuming source and checks what it emits and reports.</summary>
public sealed class PredicateRegistrationGeneratorTests
{
    /// <summary>
    /// A partial type with one marked zero-argument predicate method compiles with no error, and the generator emits a
    /// <c>Register</c> method for the type.
    /// </summary>
    [Fact]
    public void Generator_PartialTypeWithMarkedMethod_EmitsRegisterThatCompiles_Test()
    {
        const string source = """
            namespace Shop;

            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                [Predicate("isActive", "Is active", "Is the account active?")]
                public static TruthValue IsActive(Account account)
                {
                    return account.Active ? TruthValue.True : TruthValue.False;
                }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilerErrors);
        Assert.Contains("Register(", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// A parameter whose type has no literal kind (here <see cref="int"/>, where the kind is <see cref="long"/>) is
    /// error TWG001 at the parameter, and the method is not registered.
    /// </summary>
    [Fact]
    public void Generator_UnsupportedParameterType_ReportsTwg001_Test()
    {
        const string source = """
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(int Orders);

            public static partial class AccountPredicates
            {
                [Predicate("hasOrders", "Has orders", "Has the account at least the given orders?")]
                public static TruthValue HasOrders(Account account, int minimum)
                {
                    return account.Orders >= minimum ? TruthValue.True : TruthValue.False;
                }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("TWG001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("minimum", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain("hasOrders", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// Two methods of one type with the same predicate name, ignoring case, are error TWG002 at the second method. The
    /// registry matches names without case, so the generated registration would throw at run time.
    /// </summary>
    [Fact]
    public void Generator_DuplicatePredicateName_ReportsTwg002_Test()
    {
        const string source = """
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                [Predicate("isActive", "Is active", "Is the account active?")]
                public static TruthValue IsActive(Account account)
                {
                    return TruthValue.True;
                }

                [Predicate("ISACTIVE", "Is active", "Is the account active?")]
                public static TruthValue IsActiveAgain(Account account)
                {
                    return TruthValue.True;
                }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("TWG002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("IsActiveAgain", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    /// <summary>
    /// A predicate named like a DSL keyword, in any case, is error TWG007 at the method, and the name is not registered.
    /// </summary>
    [Theory]
    [InlineData("any")]
    [InlineData("Between")]
    public void Generator_ReservedPredicateName_ReportsTwg007_Test(string name)
    {
        string source = $$"""
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                [Predicate("{{name}}", "Keyword", "A name the rule text reads as a keyword.")]
                public static TruthValue Keyword(Account account)
                {
                    return TruthValue.True;
                }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("TWG007", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains($"'{name}'", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    /// <summary>
    /// A method that returns something other than <c>TruthValue</c>, <c>ValueTask&lt;TruthValue&gt;</c> or
    /// <c>Task&lt;TruthValue&gt;</c> (here <see cref="bool"/>) is error TWG003, and the method is not registered.
    /// </summary>
    [Fact]
    public void Generator_WrongReturnType_ReportsTwg003_Test()
    {
        const string source = """
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                [Predicate("isActive", "Is active", "Is the account active?")]
                public static bool IsActive(Account account)
                {
                    return account.Active;
                }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("TWG003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("bool", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.DoesNotContain("isActive", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// A method the generated delegate cannot call (an instance method, a generic method, a method with no context
    /// parameter, or a method with a <see langword="ref"/> parameter) is error TWG004, and the method is not registered.
    /// </summary>
    /// <param name="method">The marked method declaration.</param>
    [Theory]
    [InlineData("public TruthValue IsActive(Account account) { return TruthValue.True; }")]
    [InlineData("public static TruthValue IsActive<T>(T account) { return TruthValue.True; }")]
    [InlineData("public static TruthValue IsActive() { return TruthValue.True; }")]
    [InlineData("public static TruthValue IsActive(ref Account account) { return TruthValue.True; }")]
    public void Generator_MethodShapeTheDelegateCannotCall_ReportsTwg004_Test(string method)
    {
        string source = $$"""
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public partial class AccountPredicates
            {
                [Predicate("isActive", "Is active", "Is the account active?")]
                {{method}}
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("TWG004", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.DoesNotContain("isActive", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// The generator adds <c>Register</c> to the declaring type, so a declaring type, or a type around it, that is not
    /// partial or is generic is error TWG005, and nothing is generated for it.
    /// </summary>
    /// <param name="opening">The type declarations that open around the predicate method.</param>
    /// <param name="closing">The closing braces.</param>
    [Theory]
    [InlineData("public static class AccountPredicates {", "}")]
    [InlineData("public static partial class AccountPredicates<T> {", "}")]
    [InlineData("public class Outer { public static partial class AccountPredicates {", "} }")]
    public void Generator_DeclaringTypeNotPartialOrGeneric_ReportsTwg005_Test(string opening, string closing)
    {
        string source = $$"""
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            {{opening}}
                [Predicate("isActive", "Is active", "Is the account active?")]
                public static TruthValue IsActive(Account account) { return TruthValue.True; }
            {{closing}}
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("TWG005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.DoesNotContain("isActive", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// A default value with no literal form (<see langword="null"/>, or <see langword="default"/> of a struct) cannot be
    /// a schema default, so it is error TWG006 at the parameter, and the method is not registered.
    /// </summary>
    /// <param name="parameter">The optional parameter declaration.</param>
    [Theory]
    [InlineData("string? region = null")]
    [InlineData("System.Guid id = default")]
    [InlineData("System.Collections.Generic.IReadOnlyList<string>? tags = null")]
    public void Generator_DefaultValueWithNoLiteralForm_ReportsTwg006_Test(string parameter)
    {
        string source = $$"""
            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            public static partial class AccountPredicates
            {
                [Predicate("isActive", "Is active", "Is the account active?")]
                public static TruthValue IsActive(Account account, {{parameter}}) { return TruthValue.True; }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Diagnostic diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("TWG006", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.DoesNotContain("isActive", run.GeneratedSource, StringComparison.Ordinal);
    }

    /// <summary>
    /// Predicates of two context types in one nested partial type get one <c>Register</c> overload per context type. The
    /// overload for an internal context type is internal, so the generated code compiles with no accessibility error.
    /// </summary>
    [Fact]
    public void Generator_TwoContextTypesInNestedType_EmitsOneRegisterPerContextThatCompiles_Test()
    {
        const string source = """
            namespace Shop;

            using TruthWeaver.Abstractions;
            using TruthWeaver.Generators;

            public sealed record Account(bool Active);

            internal sealed record Order(long Lines);

            public static partial class Rules
            {
                public static partial class Predicates
                {
                    [Predicate("isActive", "Is active", "Is the account active?")]
                    public static TruthValue IsActive(Account account) { return TruthValue.True; }

                    [Predicate("hasLines", "Has lines", "Has the order at least the given lines?")]
                    internal static TruthValue HasLines(Order order, long minimum) { return TruthValue.True; }
                }
            }
            """;

        GeneratorRun run = GeneratorHarness.Run(source);

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.CompilerErrors);
        Assert.Contains(
            "public static global::TruthWeaver.Registry.PredicateRegistryBuilder<global::Shop.Account> Register(",
            run.GeneratedSource,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "internal static global::TruthWeaver.Registry.PredicateRegistryBuilder<global::Shop.Order> Register(",
            run.GeneratedSource,
            StringComparison.Ordinal
        );
    }
}
