namespace TruthWeaver.Samples.DataSource;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.DataSources.Json;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;

/// <summary>
/// The sample rule, its data source declaration and the compiler that connects them. The rule takes
/// its minimum age from a policy document instead of a literal, so a change to the policy needs no
/// change to the rule.
/// </summary>
public static class AgePolicyRules
{
    /// <summary>The name that the rule and the declaration use for the policy data source.</summary>
    public const string PolicySource = "policy";

    /// <summary>
    /// The sample rule in rule text. <c>from("policy", "$.minAge")</c> is the data-source argument:
    /// a source name and a JSONPath query. The rule is satisfied when the applicant is old enough.
    /// </summary>
    public const string MeetsAgePolicy = "ageAtLeast(min: from(\"policy\", \"$.minAge\"))";

    /// <summary>
    /// Creates a compiler that knows the <c>ageAtLeast</c> predicate and the <c>policy</c> data source.
    /// Declaring the source with <see cref="JsonQueryValidator"/> makes the compiler reject a
    /// malformed JSONPath query at compile time (TRE0025). An undeclared source name is rejected too (TRE0024).
    /// </summary>
    /// <returns>A compiler for rules over <see cref="Applicant"/>.</returns>
    public static RuleCompiler<Applicant> CreateCompiler()
    {
        PredicateRegistry<Applicant> registry = PredicateRegistry<Applicant>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "ageAtLeast",
                    "Age at least",
                    "Is the applicant at least the given age?",
                    [new PredicateArgumentSchema("min", "The smallest age that passes.", LiteralKind.Int64)]
                ),
                (applicant, args, _) =>
                    ValueTask.FromResult(applicant.Age >= args.GetInt64("min") ? TruthValue.True : TruthValue.False)
            )
            .Build();

        return new RuleCompiler<Applicant>(
            registry,
            new CompilerOptions(DataSources: new DataSourceDeclarations { { PolicySource, JsonQueryValidator.Instance } })
        );
    }

    /// <summary>
    /// Compiles <see cref="MeetsAgePolicy"/> and evaluates it with the given policy document.
    /// </summary>
    /// <param name="applicant">The applicant to check.</param>
    /// <param name="policyJson">The policy document, for example <c>{ "minAge": 18 }</c>.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>The decision. It is not satisfied when the policy cannot supply <c>minAge</c>.</returns>
    public static Task<Decision> EvaluateAsync(
        Applicant applicant,
        string policyJson,
        CancellationToken cancellationToken = default
    )
    {
        CompilationResult<Applicant> compiled = CreateCompiler().Compile(MeetsAgePolicy);
        DataSources sources = new() { [PolicySource] = JsonDataSource.Parse(policyJson) };

        return compiled.GetRuleOrThrow().EvaluateAsync(applicant, dataSources: sources, cancellationToken: cancellationToken);
    }
}
