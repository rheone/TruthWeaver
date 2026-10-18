namespace TruthWeaver.Tool.Core.Output;

using TruthWeaver.Evaluation;
using TruthWeaver.Yaml;

/// <summary>
/// An edited rule printed in the three formats, with its notes, so the write-back helpers need no context type. The JSON
/// form is also the form two rules are compared by: equal JSON means the same tree and the same notes.
/// </summary>
/// <param name="Json">The rule as tree-format JSON, with notes.</param>
/// <param name="Yaml">The rule as tree-format YAML, with notes, with line feeds.</param>
/// <param name="Text">The rule as rule text, with notes as comments.</param>
internal sealed record PrintedRule(string Json, string Yaml, string Text)
{
    /// <summary>Prints a compiled rule.</summary>
    /// <typeparam name="TContext">The context type of the rule.</typeparam>
    /// <param name="rule">The rule.</param>
    /// <returns>The printed forms.</returns>
    public static PrintedRule Of<TContext>(CompiledRule<TContext> rule)
    {
        string yaml = rule.PrintYaml(includeNotes: true).Replace("\r\n", "\n", StringComparison.Ordinal);
        return new PrintedRule(rule.PrintJson(includeNotes: true), yaml, rule.PrintRuleText());
    }
}
