namespace TruthWeaver.Testing.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// Shared setup for the variable-reference tests: a registry of "takes" predicates that each declare one argument <c>v</c> of
/// a given kind, return <see cref="TruthValue.True"/> and record the value they were called with, so a test can see exactly
/// what a variable resolved to.
/// </summary>
internal sealed class VariableHarness
{
    private static readonly (string Name, LiteralKind Kind)[] Takers =
    [
        ("takesString", LiteralKind.String),
        ("takesInt64", LiteralKind.Int64),
        ("takesDecimal", LiteralKind.Decimal),
        ("takesBoolean", LiteralKind.Boolean),
        ("takesDateTime", LiteralKind.DateTimeOffset),
        ("takesGuid", LiteralKind.Guid),
        ("takesStrings", LiteralKind.StringArray),
        ("takesInt64s", LiteralKind.Int64Array),
        ("takesDecimals", LiteralKind.DecimalArray),
        ("takesBooleans", LiteralKind.BooleanArray),
        ("takesDateTimes", LiteralKind.DateTimeOffsetArray),
        ("takesGuids", LiteralKind.GuidArray),
    ];

    /// <summary>Gets the argument values the "takes" predicates were called with, in call order.</summary>
    public List<LiteralValue> Received { get; } = [];

    /// <summary>Evaluates a compiled rule with the given sources, using no services.</summary>
    public static Task<Decision> EvaluateAsync(
        CompiledRule<object?> rule,
        DataSources? sources,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return rule.EvaluateAsync(null, new NoServices(), sources, options, cancellationToken);
    }

    /// <summary>Compiles <paramref name="ruleText"/> with the given source names declared.</summary>
    public CompiledRule<object?> Compile(string ruleText, params string[] declaredSources)
    {
        DataSourceDeclarations declarations = [];
        foreach (string name in declaredSources)
        {
            declarations.Add(name);
        }

        PredicateRegistryBuilder<object?> builder = PredicateRegistry<object?>.CreateBuilder();
        foreach ((string name, LiteralKind kind) in Takers)
        {
            builder.Add(
                new PredicateSchema(
                    name,
                    name,
                    "Records its argument and returns True.",
                    [new PredicateArgumentSchema("v", "The value.", kind)]
                ),
                (_, args, _) =>
                {
                    this.Received.Add(args.GetRaw("v"));
                    return ValueTask.FromResult(TruthValue.True);
                }
            );
        }

        builder.Add(
            PredicateSchema.NoArguments("yes", "Yes", "Always true."),
            (_, _, _) => ValueTask.FromResult(TruthValue.True)
        );
        builder.Add(
            PredicateSchema.NoArguments("no", "No", "Always false."),
            (_, _, _) => ValueTask.FromResult(TruthValue.False)
        );

        RuleCompiler<object?> compiler = new(builder.Build(), new CompilerOptions(DataSources: declarations));
        CompilationResult<object?> result = compiler.Compile(ruleText);
        return result.CompiledRule
            ?? throw new InvalidOperationException($"Test rule did not compile: {result.FormatDiagnostics(ruleText)}");
    }

    private sealed class NoServices : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
