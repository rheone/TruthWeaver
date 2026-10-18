namespace TruthWeaver.Tool.Core.Assignment;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Yaml;

/// <summary>Lists the distinct terms of a compiled rule, in the order of their first use.</summary>
internal static class RuleTermList
{
    /// <summary>Lists the terms of a rule.</summary>
    /// <param name="rule">The rule.</param>
    /// <param name="settings">The settings that the compiler uses to read a term back.</param>
    /// <returns>The terms.</returns>
    public static IReadOnlyList<Entry> Of(CompiledRule<StubAssignment> rule, ToolSettings settings)
    {
        RuleCompiler<StubAssignment> compiler = RuleConverter.CreateCompiler(settings);
        List<Entry> terms = [];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        Visit(JsonNode.Parse(rule.PrintJson(includeNotes: false)), compiler, terms, seen);
        return terms;
    }

    // The JSON tree names a term with a "predicate" member, and every other node holds its operands in a child array or object.
    private static void Visit(JsonNode? node, RuleCompiler<StubAssignment> compiler, List<Entry> terms, HashSet<string> seen)
    {
        switch (node)
        {
            case JsonObject tree when tree["predicate"] is JsonValue:
                // The term alone, compiled the same way as a key of the stub values, gives the canonical text of the key.
                JsonObject term = new()
                {
                    ["predicate"] = tree["predicate"]?.DeepClone(),
                    ["args"] = tree["args"]?.DeepClone() ?? new JsonObject(),
                };
                if (
                    compiler
                        .CompileAs(term.ToJsonString(), RuleFormat.Json)
                        .TryGetRule(out CompiledRule<StubAssignment>? compiled, out _) && seen.Add(compiled.CanonicalText)
                )
                {
                    bool data = term["args"] is JsonObject args && args.Any(a => a.Value is JsonObject);
                    terms.Add(new Entry(compiled.CanonicalText, data));
                }

                break;
            case JsonObject tree:
                foreach ((string _, JsonNode? child) in tree)
                {
                    Visit(child, compiler, terms, seen);
                }

                break;
            case JsonArray list:
                foreach (JsonNode? child in list)
                {
                    Visit(child, compiler, terms, seen);
                }

                break;
        }
    }

    /// <summary>One term: its canonical text and whether it has a <c>from(...)</c> argument.</summary>
    /// <param name="Canonical">The canonical text of the term.</param>
    /// <param name="HasDataReference">Whether an argument of the term is a <c>from(...)</c> reference.</param>
    internal sealed record Entry(string Canonical, bool HasDataReference);
}
