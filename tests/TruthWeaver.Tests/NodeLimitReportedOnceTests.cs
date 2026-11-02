namespace TruthWeaver.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// A rule that crosses <see cref="CompilerOptions.MaxNodeCount"/> reports <see cref="DiagnosticCodes.MaxNodeCountExceeded"/>
/// exactly once, wherever the limit is crossed and however many nodes follow the crossing.
/// </summary>
public sealed class NodeLimitReportedOnceTests
{
    private const int Limit = 10;

    /// <summary>A limit crossed in the rule itself is reported once.</summary>
    [Fact]
    public void Compile_LimitCrossedInTheRule_ReportsTheLimitOnce_Test()
    {
        CompilationResult<CollectionContext> result = CreateCompiler().Compile(Repeat("a", 12));

        Assert.Single(result.Diagnostics, IsNodeLimit);
    }

    /// <summary>A limit crossed in a quantifier body is reported once, although more nodes follow the quantifier.</summary>
    [Fact]
    public void Compile_LimitCrossedInAQuantifierBody_ReportsTheLimitOnce_Test()
    {
        string rule = $"FORALL(x IN items, {Repeat("elementTrue", 12)}) AND {Repeat("a", 3)}";

        CompilationResult<CollectionContext> result = CreateCompiler().Compile(rule);

        Assert.Single(result.Diagnostics, IsNodeLimit);
    }

    /// <summary>A limit crossed in a nested quantifier body is reported once, although more nodes follow in the outer body and the rule.</summary>
    [Fact]
    public void Compile_LimitCrossedInANestedQuantifierBody_ReportsTheLimitOnce_Test()
    {
        string rule =
            $"FORALL(g IN groups, EXISTS(m IN members, {Repeat("elementTrue", 12)}) AND {Repeat("groupOk", 3)}) AND {Repeat("a", 3)}";

        CompilationResult<CollectionContext> result = CreateCompiler().Compile(rule);

        Assert.Single(result.Diagnostics, IsNodeLimit);
    }

    /// <summary>A limit crossed by the uses of a <c>let</c> definition is reported once.</summary>
    [Fact]
    public void Compile_LimitCrossedInALetUse_ReportsTheLimitOnce_Test()
    {
        string rule = $"let big = {Repeat("a", 12)}; big AND big AND {Repeat("a", 3)}";

        CompilationResult<CollectionContext> result = CreateCompiler().Compile(rule);

        Assert.Single(result.Diagnostics, IsNodeLimit);
    }

    /// <summary>A limit crossed by a library rule reference is reported once, although more nodes follow the reference.</summary>
    [Fact]
    public void Compile_LimitCrossedInALibraryRule_ReportsTheLimitOnce_Test()
    {
        RuleLibrary library = new([new KeyValuePair<string, string>("big", Repeat("a", 12))]);

        CompilationResult<CollectionContext> result = CreateCompiler(library).Compile($"rule(\"big\") AND {Repeat("a", 3)}");

        Assert.Single(result.Diagnostics, IsLibraryNodeLimit);
    }

    private static bool IsNodeLimit(Diagnostic diagnostic)
    {
        return diagnostic.Code == DiagnosticCodes.MaxNodeCountExceeded;
    }

    // A library rule body that goes over the limit surfaces as one InvalidLibraryRule diagnostic that carries the code.
    private static bool IsLibraryNodeLimit(Diagnostic diagnostic)
    {
        return diagnostic.Code == DiagnosticCodes.InvalidLibraryRule
            && diagnostic.Message.Contains(DiagnosticCodes.MaxNodeCountExceeded, StringComparison.Ordinal);
    }

    private static string Repeat(string term, int count)
    {
        return string.Join(" AND ", Enumerable.Repeat(term, count));
    }

    private static RuleCompiler<CollectionContext> CreateCompiler(RuleLibrary? library = null)
    {
        return CollectionContext.CreateCompiler(new CompilerOptions(MaxNodeCount: Limit, Library: library));
    }
}
