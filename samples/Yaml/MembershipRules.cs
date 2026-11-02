namespace TruthWeaver.Samples.Yaml;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

/// <summary>
/// A rule written as YAML and the compiler that loads it. The YAML tree has the same shape as the JSON
/// tree, so a team can keep rules in configuration files and compile them at start-up.
/// </summary>
public static class MembershipRules
{
    /// <summary>
    /// The sample rule in YAML. It is satisfied for an active member who is not suspended.
    /// </summary>
    public const string CanDownload = """
        op: and
        operands:
          - predicate: isActive
          - op: not
            operands:
              - predicate: isSuspended
        """;

    /// <summary>Creates a compiler that knows the <c>isActive</c> and <c>isSuspended</c> predicates.</summary>
    /// <returns>A compiler for rules over <see cref="Member"/>.</returns>
    public static RuleCompiler<Member> CreateCompiler()
    {
        PredicateRegistry<Member> registry = PredicateRegistry<Member>
            .CreateBuilder()
            .Add(
                PredicateSchema.NoArguments("isActive", "Is active", "Is the membership active?"),
                (member, _, _) => ValueTask.FromResult(member.IsActive ? TruthValue.True : TruthValue.False)
            )
            .Add(
                PredicateSchema.NoArguments("isSuspended", "Is suspended", "Is the member suspended?"),
                (member, _, _) => ValueTask.FromResult(member.IsSuspended ? TruthValue.True : TruthValue.False)
            )
            .Build();

        return new RuleCompiler<Member>(registry);
    }

    /// <summary>
    /// Compiles <see cref="CanDownload"/> from YAML with <c>CompileYaml</c>.
    /// </summary>
    /// <returns>The compilation result. It carries diagnostics when the YAML is not a valid rule.</returns>
    public static CompilationResult<Member> Compile()
    {
        return CreateCompiler().CompileYaml(CanDownload);
    }
}
