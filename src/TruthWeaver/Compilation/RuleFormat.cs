namespace TruthWeaver.Compilation;

/// <summary>
/// The three forms a rule can be written in. All three compile to the same tree, and a compiled rule prints to each of
/// them without loss, notes included. The helpers that take a format (<c>CompileAs</c>, <c>PrintAs</c>, <c>Convert</c>)
/// are in <c>TruthWeaver.Yaml</c>, the one package that reads and writes all three.
/// </summary>
public enum RuleFormat
{
    /// <summary>Rule text, the DSL: <c>RuleCompiler.Compile</c> reads it and <c>CompiledRule.PrintRuleText</c> writes it.</summary>
    Text,

    /// <summary>The JSON tree: <c>RuleCompiler.CompileJson</c> reads it and <c>CompiledRule.PrintJson</c> writes it.</summary>
    Json,

    /// <summary>The YAML tree: <c>CompileYaml</c> reads it and <c>PrintYaml</c> writes it (both in <c>TruthWeaver.Yaml</c>).</summary>
    Yaml,
}
