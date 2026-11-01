namespace TruthWeaver.Tests;

using TruthWeaver.Building;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The array (<c>params</c>) form and the sequence form of every <see cref="RuleBuilder"/> operator give the same
/// compiled rule, or the same diagnostics, so the argument type never changes the meaning of a rule.
/// </summary>
public sealed class RuleBuilderOverloadConsistencyTests
{
    /// <summary>The array form and the list form of an operator with an identity compile to the same rule for any operand count.</summary>
    [Theory]
    [InlineData("And", 0)]
    [InlineData("And", 1)]
    [InlineData("And", 2)]
    [InlineData("And", 3)]
    [InlineData("Or", 0)]
    [InlineData("Or", 1)]
    [InlineData("Or", 3)]
    [InlineData("Any", 0)]
    [InlineData("Any", 1)]
    [InlineData("Any", 3)]
    [InlineData("All", 0)]
    [InlineData("All", 1)]
    [InlineData("All", 3)]
    [InlineData("None", 0)]
    [InlineData("None", 1)]
    [InlineData("None", 3)]
    public void Compile_OperatorWithAnIdentity_ArrayAndListGiveTheSameRule_Test(string op, int count)
    {
        CompilationResult<RuleTestContext> fromArray = Build(op, count, asList: false).Compile(CreateCompiler());
        CompilationResult<RuleTestContext> fromList = Build(op, count, asList: true).Compile(CreateCompiler());

        Assert.True(fromArray.Succeeded);
        Assert.Equal(fromList.CompiledRule!.CanonicalText, fromArray.CompiledRule!.CanonicalText);
    }

    /// <summary>An empty array of an operator with an identity is the identity constant; one operand is itself.</summary>
    [Theory]
    [InlineData("And", "True")]
    [InlineData("All", "True")]
    [InlineData("None", "True")]
    [InlineData("Or", "False")]
    [InlineData("Any", "False")]
    public void Compile_EmptyArrayOfAnOperatorWithAnIdentity_IsTheIdentityConstant_Test(string op, string expected)
    {
        CompilationResult<RuleTestContext> result = Build(op, 0, asList: false).Compile(CreateCompiler());

        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>One operand in an array is that operand, and under <c>None</c> its negation.</summary>
    [Theory]
    [InlineData("And", "isManager")]
    [InlineData("Or", "isManager")]
    [InlineData("Any", "isManager")]
    [InlineData("All", "isManager")]
    [InlineData("None", "NOT isManager")]
    public void Compile_OneOperandInAnArray_FoldsToTheOperand_Test(string op, string expected)
    {
        CompilationResult<RuleTestContext> result = Build(op, 1, asList: false).Compile(CreateCompiler());

        Assert.Equal(expected, result.CompiledRule!.CanonicalText);
    }

    /// <summary>An operator with no identity gives the same diagnostics, or the same rule, from an array and from a list.</summary>
    [Theory]
    [InlineData("Parity", 0)]
    [InlineData("Parity", 1)]
    [InlineData("Parity", 2)]
    [InlineData("ExactlyOne", 0)]
    [InlineData("ExactlyOne", 1)]
    [InlineData("ExactlyOne", 3)]
    [InlineData("Coalesce", 3)]
    [InlineData("AtLeast", 0)]
    [InlineData("AtLeast", 1)]
    [InlineData("AtLeast", 3)]
    [InlineData("AtMost", 0)]
    [InlineData("AtMost", 1)]
    [InlineData("AtMost", 3)]
    [InlineData("GreaterThan", 0)]
    [InlineData("GreaterThan", 1)]
    [InlineData("GreaterThan", 3)]
    [InlineData("LessThan", 0)]
    [InlineData("LessThan", 1)]
    [InlineData("LessThan", 3)]
    [InlineData("Exactly", 0)]
    [InlineData("Exactly", 1)]
    [InlineData("Exactly", 3)]
    [InlineData("Between", 0)]
    [InlineData("Between", 1)]
    [InlineData("Between", 3)]
    public void Compile_OperatorWithNoIdentity_ArrayAndListGiveTheSameResult_Test(string op, int count)
    {
        CompilationResult<RuleTestContext> fromArray = Build(op, count, asList: false).Compile(CreateCompiler());
        CompilationResult<RuleTestContext> fromList = Build(op, count, asList: true).Compile(CreateCompiler());

        Assert.Equal(fromArray.Succeeded, fromList.Succeeded);
        Assert.Equal(fromArray.CompiledRule?.CanonicalText, fromList.CompiledRule?.CanonicalText);
        Assert.Equal(fromArray.Diagnostics.Select(d => d.Code), fromList.Diagnostics.Select(d => d.Code));
    }

    /// <summary>
    /// <c>Coalesce</c> folds a short call the same way from params, an array and a list: <c>Unknown</c> is its identity, so
    /// zero operands are the constant <c>Unknown</c> and one operand is itself.
    /// </summary>
    [Theory]
    [InlineData(0, "Unknown")]
    [InlineData(1, "isManager")]
    public void Compile_ShortCallForCoalesce_FoldsFromParamsArrayAndList_Test(int count, string expected)
    {
        CompilationResult<RuleTestContext> fromList = Build("Coalesce", count, asList: true).Compile(CreateCompiler());
        CompilationResult<RuleTestContext> fromArray = Build("Coalesce", count, asList: false).Compile(CreateCompiler());
        CompilationResult<RuleTestContext> fromParams = (
            count == 0 ? RuleBuilder.Coalesce() : RuleBuilder.Coalesce(RuleBuilder.Predicate("isManager"))
        ).Compile(CreateCompiler());

        Assert.Equal(expected, fromList.CompiledRule!.CanonicalText);
        Assert.Equal(expected, fromArray.CompiledRule!.CanonicalText);
        Assert.Equal(expected, fromParams.CompiledRule!.CanonicalText);
    }

    /// <summary>A short list for an operator with no identity is a compile diagnostic, not a folded rule.</summary>
    [Theory]
    [InlineData("Parity", 1)]
    [InlineData("ExactlyOne", 1)]
    public void Compile_ShortListForAnOperatorWithNoIdentity_ReportsInfixArityViolation_Test(string op, int count)
    {
        CompilationResult<RuleTestContext> result = Build(op, count, asList: true).Compile(CreateCompiler());

        Assert.False(result.Succeeded);
        Assert.Contains(result.Diagnostics, d => d.Code == DiagnosticCodes.InfixArityViolation);
    }

    /// <summary><c>GreaterThan</c> and <c>LessThan</c> accept a sequence, as the other threshold operators do.</summary>
    [Fact]
    public void GreaterThanAndLessThan_WithAList_BuildTheSameRuleAsTheArray_Test()
    {
        List<RuleBuilder> operands = [RuleBuilder.Predicate("isManager"), RuleBuilder.Predicate("isDepartmentHead")];
        RuleBuilder[] array = [.. operands];

        CompilationResult<RuleTestContext> greater = RuleBuilder.GreaterThan(0, operands).Compile(CreateCompiler());
        CompilationResult<RuleTestContext> less = RuleBuilder.LessThan(2, operands).Compile(CreateCompiler());

        Assert.Equal(RuleBuilder.GreaterThan(0, array).ToJson(), RuleBuilder.GreaterThan(0, operands).ToJson());
        Assert.Equal(RuleBuilder.LessThan(2, array).ToJson(), RuleBuilder.LessThan(2, operands).ToJson());
        Assert.True(greater.Succeeded);
        Assert.True(less.Succeeded);
    }

    /// <summary>A null sequence for <c>GreaterThan</c> or <c>LessThan</c> throws, as for the other threshold operators.</summary>
    [Fact]
    public void GreaterThan_WithANullSequence_ThrowsArgumentNullException_Test()
    {
        Assert.Throws<ArgumentNullException>(() => RuleBuilder.GreaterThan(1, (IEnumerable<RuleBuilder>)null!));
    }

    /// <summary>An argument value of an unsupported type is a diagnostic from <c>Compile</c>, not an exception.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Compile_UnsupportedArgumentValueType_ReportsArgumentTypeMismatch_Test(int which)
    {
        object?[] unsupported = [new object(), null, new object[] { "ok", new() }, new List<DateTime> { DateTime.UtcNow }];
        RuleBuilder builder = RuleBuilder.Predicate("hasRole", ("role", unsupported[which]!));

        CompilationResult<RuleTestContext> result = builder.Compile(CreateCompiler());

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.ArgumentTypeMismatch, diagnostic.Code);
        Assert.Equal("$.args.role", diagnostic.Path);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// A builder argument value that holds a lone UTF-16 surrogate, at any depth, is a malformed tree at the argument
    /// from <c>Compile</c>: JSON cannot carry it, and replacing it would silently change the value.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Compile_ArgumentValueWithALoneSurrogate_ReportsMalformedTree_Test(int which)
    {
        object[] values = ["Y\uD800", new[] { "ok", "\uDC00" }, Arg.From("user\uD800", "$.role")];
        RuleBuilder builder = RuleBuilder.Predicate("hasRole", ("role", values[which]));

        CompilationResult<RuleTestContext> result = builder.Compile(CreateCompiler());

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal(DiagnosticCodes.MalformedTree, diagnostic.Code);
        Assert.Equal("$.args.role", diagnostic.Path);
        Assert.Contains("UTF-16", diagnostic.Message, StringComparison.Ordinal);
        Assert.Null(result.CompiledRule);
    }

    /// <summary>
    /// A <see langword="double"/> argument value that is <c>NaN</c> or an infinity, alone or in a sequence, is the
    /// diagnostic <c>ArgumentTypeMismatch</c> from <c>Compile</c>: JSON has no such number, so it is never an exception.
    /// </summary>
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Compile_NonFiniteDoubleArgument_ReportsArgumentTypeMismatch_Test(double value)
    {
        RuleBuilder single = RuleBuilder.Predicate("hasRole", ("role", value));
        RuleBuilder inSequence = RuleBuilder.Predicate("hasRole", ("role", new[] { 1.5, value }));

        CompilationResult<RuleTestContext>[] results = [single.Compile(CreateCompiler()), inSequence.Compile(CreateCompiler())];

        Assert.All(
            results,
            result =>
            {
                Diagnostic diagnostic = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
                Assert.Equal(DiagnosticCodes.ArgumentTypeMismatch, diagnostic.Code);
                Assert.Equal("$.args.role", diagnostic.Path);
                Assert.Contains("finite", diagnostic.Message, StringComparison.Ordinal);
            }
        );
    }

    /// <summary>A finite <see langword="double"/> argument value still renders as a JSON number.</summary>
    [Fact]
    public void ToJson_FiniteDoubleArgument_RendersTheNumber_Test()
    {
        string json = RuleBuilder.Predicate("score", ("min", 1.5)).ToJson();

        Assert.Equal("""{"predicate":"score","args":{"min":1.5}}""", json);
    }

    private static RuleBuilder Build(string op, int count, bool asList)
    {
        RuleBuilder[] all =
        [
            RuleBuilder.Predicate("isManager"),
            RuleBuilder.Predicate("isDepartmentHead"),
            RuleBuilder.Predicate("isOther"),
        ];
        RuleBuilder[] array = [.. all.Take(count)];
        List<RuleBuilder> list = [.. array];

        return (op, asList) switch
        {
            ("And", false) => RuleBuilder.And(array),
            ("And", true) => RuleBuilder.And(list),
            ("Or", false) => RuleBuilder.Or(array),
            ("Or", true) => RuleBuilder.Or(list),
            ("Any", false) => RuleBuilder.Any(array),
            ("Any", true) => RuleBuilder.Any(list),
            ("All", false) => RuleBuilder.All(array),
            ("All", true) => RuleBuilder.All(list),
            ("None", false) => RuleBuilder.None(array),
            ("None", true) => RuleBuilder.None(list),
            ("Parity", false) => RuleBuilder.Parity(array),
            ("Parity", true) => RuleBuilder.Parity(list),
            ("ExactlyOne", false) => RuleBuilder.ExactlyOne(array),
            ("ExactlyOne", true) => RuleBuilder.ExactlyOne(list),
            ("Coalesce", false) => RuleBuilder.Coalesce(array),
            ("Coalesce", true) => RuleBuilder.Coalesce(list),
            ("AtLeast", false) => RuleBuilder.AtLeast(1, array),
            ("AtLeast", true) => RuleBuilder.AtLeast(1, list),
            ("AtMost", false) => RuleBuilder.AtMost(1, array),
            ("AtMost", true) => RuleBuilder.AtMost(1, list),
            ("GreaterThan", false) => RuleBuilder.GreaterThan(1, array),
            ("GreaterThan", true) => RuleBuilder.GreaterThan(1, list),
            ("LessThan", false) => RuleBuilder.LessThan(1, array),
            ("LessThan", true) => RuleBuilder.LessThan(1, list),
            ("Exactly", false) => RuleBuilder.Exactly(1, array),
            ("Exactly", true) => RuleBuilder.Exactly(1, list),
            ("Between", false) => RuleBuilder.Between(0, 1, array),
            ("Between", true) => RuleBuilder.Between(0, 1, list),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, "Unknown operator."),
        };
    }

    private static RuleCompiler<RuleTestContext> CreateCompiler()
    {
        return new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .AddStringArgPredicate("hasRole", "role", "Y")
                .AddConstant("isManager", true)
                .AddConstant("isDepartmentHead", true)
                .AddConstant("isOther", true)
                .Build()
        );
    }
}
