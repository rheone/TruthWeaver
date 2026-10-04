namespace TruthWeaver.Architecture.Tests;

using System.Reflection;
using System.Text.RegularExpressions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>
/// Guards the naming cleanup of ADR-0007: a name or diagnostic prefix the glossary retired must not return to a
/// shipped assembly. Each test fails on a repository-wide rule.
/// </summary>
public sealed partial class VocabularyGuardTests
{
    private const BindingFlags AllMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Assembly[] Shipped =
    [
        typeof(TruthWeaver.Abstractions.TruthValue).Assembly,
        typeof(RuleCompiler<>).Assembly,
        typeof(TruthWeaver.Yaml.YamlRuleExtensions).Assembly,
        typeof(TruthWeaver.Predicates.StringPredicates).Assembly,
        typeof(TruthWeaver.Testing.DecisionAssertions).Assembly,
    ];

    /// <summary>No type or member in a shipped assembly is named with a retired word.</summary>
    [Fact]
    public void ShippedAssemblies_TypesAndMembers_UseNoRetiredName_Test()
    {
        List<string> offenders = [];
        foreach (Type type in Shipped.SelectMany(a => a.GetTypes()))
        {
            if (RetiredName().IsMatch(type.Name))
            {
                offenders.Add(type.FullName!);
            }

            offenders.AddRange(
                type.GetMembers(AllMembers).Where(m => RetiredName().IsMatch(m.Name)).Select(m => $"{type.FullName}.{m.Name}")
            );
            offenders.AddRange(
                type.GetMethods(AllMembers)
                    .SelectMany(m => m.GetParameters().Select(p => (m, p)))
                    .Where(x => RetiredName().IsMatch(x.p.Name ?? string.Empty))
                    .Select(x => $"{type.FullName}.{x.m.Name}({x.p.Name})")
            );
        }

        Assert.Empty(offenders);
    }

    /// <summary>The short-circuit mode is named for its behavior, so <c>EvaluationMode</c> has no <c>Default</c> member.</summary>
    [Fact]
    public void EvaluationMode_Members_IncludeNoDefault_Test()
    {
        Assert.DoesNotContain("Default", Enum.GetNames<EvaluationMode>());
    }

    /// <summary>Every diagnostic code constant uses the <c>TRE</c> prefix, never the retired <c>BRE</c> one.</summary>
    [Fact]
    public void DiagnosticCodes_Constants_NoneStartWithBre_Test()
    {
        List<string> retired =
        [
            .. Shipped
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetFields(AllMembers))
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .Where(v => v.StartsWith("BRE", StringComparison.Ordinal)),
        ];

        Assert.Empty(retired);
    }

    /// <summary>
    /// Matches the retired identifiers of ADR-0007, each listed as a whole name. <c>Gate</c> matches as a whole
    /// PascalCase word, so <c>Delegate</c>, <c>Negate</c> and <c>Aggregate</c> do not match. "ResolvedValue" is not
    /// retired as a prefix: <c>ResolvedValuePredicates</c> and <c>TResolved</c> are retired, while
    /// <c>EvaluationOptions.IncludeResolvedValues</c> keeps "resolved" for data sources (ADR-0006).
    /// </summary>
    /// <remarks>
    /// <c>Gate</c> became <c>NandNorExpander</c>; <c>RuleDescription</c> became <c>OutlineNode</c>;
    /// <c>EvaluatedNode</c> became <c>TraceNode</c>; <c>ResolvedValuePredicates</c> became
    /// <c>SelectedValuePredicates</c>; <c>TResolved</c> became <c>TSelected</c>.
    /// </remarks>
    [GeneratedRegex("Gate(?![a-z])|RuleDescription|EvaluatedNode|ResolvedValuePredicates|TResolved")]
    private static partial Regex RetiredName();
}
