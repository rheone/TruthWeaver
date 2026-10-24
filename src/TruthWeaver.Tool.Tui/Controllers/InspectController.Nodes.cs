namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Tool.Tui.Predicates;

/// <summary>
/// The text of a node of the tree, and the operator and predicate facts that both the tree and the editor-text routes
/// read: the outline of the compiled rule, the predicate catalog and the palette. Adding an operator to the library needs
/// no edit here; only <see cref="CardinalityOperators"/> and <see cref="ExternalOperators"/> are hand-written, because the
/// outline carries no such fact, and a test checks each against the library's own operator list.
/// </summary>
internal sealed partial class InspectController
{
    // The operators that count their operands, so the text adds how many operands are True, False and Unknown now. The
    // outline gives no "counts its operands" fact, so this is the Cardinality group of the Insert menu, read as names
    // internal so a test can check it against that group.
    internal static readonly HashSet<string> CardinalityOperators = new(
        ["any", "all", "none", "exactlyone", "atleast", "atmost", "exactly", "greaterthan", "lessthan", "between"],
        StringComparer.OrdinalIgnoreCase
    );

    // The external operators (ADR-0005): they read how known a value is, so they are not information-monotone. The
    // outline gives no such fact either; internal so a test can check it against the library's operator list.
    internal static readonly HashSet<string> ExternalOperators = new(
        ["istrue", "isfalse", "isunknown", "isknown", "coalesce"],
        StringComparer.OrdinalIgnoreCase
    );

    // The other spellings of an operator in rule text: the aliases and the symbols. The name is the word of the palette.
    // Internal so a test can check every entry against the parser: each symbol must parse to the operator it claims.
    internal static readonly Dictionary<string, string> OperatorSpellings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IFF"] = Equivalent,
        ["XNOR"] = Equivalent,
        ["&&"] = "And",
        ["||"] = "Or",
        ["??"] = "Coalesce",
        ["!"] = "Not",
        ["¬"] = "Not",
        ["∧"] = "And",
        ["∨"] = "Or",
        ["⊕"] = "Xor",
        ["⊻"] = "Xor",
        ["→"] = "Implies",
        ["⇒"] = "Implies",
        ["↔"] = Equivalent,
        ["⇔"] = Equivalent,
        ["↑"] = "Nand",
        ["⊼"] = "Nand",
        ["↓"] = "Nor",
        ["⊽"] = "Nor",
    };

    private const string Equivalent = "Equivalent";

    /// <summary>Describes a node of the tree.</summary>
    /// <param name="path">The path of the node.</param>
    /// <returns>The text, or <see langword="null"/> when the tree has no such node.</returns>
    public string? DescribeNode(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (StructureController.For(this.workbench).Tree is not { } tree || tree.Find(path) is not { } node)
        {
            return null;
        }

        Lines text = [];
        switch (node.Kind)
        {
            case RuleTreeNodeKind.Operator:
                this.OperatorNode(text, tree, node);
                break;
            case RuleTreeNodeKind.Term:
                this.TermNode(text, tree, node);
                break;
            case RuleTreeNodeKind.Constant:
                ConstantNode(
                    text,
                    node,
                    EvaluationStepController.ResultsOf(tree, this.workbench.Outcome).GetValueOrDefault(node.Path)
                );
                break;
            case RuleTreeNodeKind.Definition:
                DefinitionNode(text, tree, node);
                break;
            case RuleTreeNodeKind.Use:
                UseNode(text, tree, node);
                break;
            default:
                text.Add($"Parameter {node.Label}");
                text.Add("Each use of the definition puts its argument in this place.");
                break;
        }

        if (node.Note is { } note)
        {
            text.Add($"Note: {note}");
        }

        this.Findings(text, node.Path, node.Path == "$");
        return text.ToString();
    }

    // The paths of the operands of a node, in order. Internal so a test can check it.
    internal static List<string> OperandPaths(RuleTree tree, RuleTreeNode node)
    {
        Dictionary<RuleTreeNode, int> indexes = NodeIndexes.GetValue(
            tree,
            key =>
            {
                Dictionary<RuleTreeNode, int> map = [with(ReferenceEqualityComparer.Instance)];
                for (int i = 0; i < key.Nodes.Count; i++)
                {
                    map[key.Nodes[i]] = i;
                }

                return map;
            }
        );
        List<string> paths = [];
        int at = indexes.GetValueOrDefault(node, -1);
        int next = at + 1;
        for (int i = 0; at >= 0 && i < node.OperandCount && next < tree.Nodes.Count; i++)
        {
            paths.Add(tree.Nodes[next].Path);
            next = EndOf(tree, next);
        }

        return paths;
    }

    // from("source", "path"): the call as it is written in rule text.
    [GeneratedRegex("""from\(\s*"(?:[^"\\]|\\.)*"\s*,\s*"(?:[^"\\]|\\.)*"\s*\)""", RegexOptions.IgnoreCase)]
    private static partial Regex FromCall();

    [GeneratedRegex("""from\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)""", RegexOptions.IgnoreCase)]
    private static partial Regex FromParts();

    // The index just past the subtree rooted at a node: shared by OperandPaths (walking siblings) and OutlineOf (skipping
    // a subtree whose outline does not match the tree's own operand count).
    private static int EndOf(RuleTree tree, int at)
    {
        int next = at + 1;
        for (int i = 0; i < tree.Nodes[at].OperandCount && next < tree.Nodes.Count; i++)
        {
            next = EndOf(tree, next);
        }

        return next;
    }

    // The path of the one node of the tree whose label is the operator, or null when there are none or several.
    private static RuleTreeNode? OnlyOperator(RuleTree tree, string name)
    {
        RuleTreeNode[] matches =
        [
            .. tree.Nodes.Where(n =>
                n.Kind == RuleTreeNodeKind.Operator
                && n.Path.StartsWith('$')
                && !n.Path.StartsWith("$.definitions", StringComparison.Ordinal)
                && NameOf(n.Label).Equals(name, StringComparison.OrdinalIgnoreCase)
            ),
        ];
        return matches.Length == 1 ? matches[0] : null;
    }

    // The name of an operator or a term from its label: the leading word, such as "AtLeast" of "AtLeast(2)".
    private static string NameOf(string label)
    {
        int end = 0;
        while (end < label.Length && IsWordChar(label[end]))
        {
            end++;
        }

        return label[..end];
    }

    private static string ValueWord(EvaluationStep? step)
    {
        if (step is null)
        {
            return "not evaluated";
        }

        return step.Result == "skipped" ? "skipped (short-circuited)" : step.Result;
    }

    private static void ConstantNode(Lines text, RuleTreeNode node, EvaluationStep? step)
    {
        text.Add($"Constant {node.Label}");
        text.Add(ConstantMeaning(node.Label));
        text.Add($"Value now: {ValueWord(step)}");
    }

    private static string ConstantMeaning(string name)
    {
        return name.ToUpperInvariant() switch
        {
            "T" or "TRUE" => "T stands for True: definitely true.",
            "F" or "FALSE" => "F stands for False: definitely false.",
            _ => "U stands for Unknown: not decided either way.",
        };
    }

    private static void ConstantWord(Lines text, string token)
    {
        text.Add($"Constant {token}");
        text.Add(ConstantMeaning(token));
    }

    // The outline node of each node of the rule, by path: the operator table and the predicate schema describe the node.
    private static Dictionary<string, OutlineNode> OutlineOf(RuleTree tree)
    {
        Dictionary<string, OutlineNode> map = [with(StringComparer.Ordinal)];
        if (tree.Nodes.Count > 0)
        {
            _ = Visit(0, tree.Rule.Outline());
        }

        return map;

        int Visit(int at, OutlineNode outline)
        {
            RuleTreeNode node = tree.Nodes[at];
            map[node.Path] = outline;
            if (outline.Operands.Count != node.OperandCount)
            {
                return EndOf(tree, at);
            }

            int next = at + 1;
            foreach (OutlineNode child in outline.Operands)
            {
                next = Visit(next, child);
            }

            return next;
        }
    }

    private static string PredicateKinds(PaletteEntry entry)
    {
        if (entry.Arguments.Count == 0)
        {
            return "none";
        }

        return string.Join(
            ", ",
            entry.Arguments.Select(a => $"{a.Name}: {a.Kind}{(a.Required ? string.Empty : " (optional)")}")
        );
    }

    // The threshold and how many operands are True, False and Unknown now, for a cardinality operator.
    private static void OperandSummary(
        Lines text,
        RuleTree tree,
        RuleTreeNode node,
        IReadOnlyDictionary<string, EvaluationStep> results
    )
    {
        if (!CardinalityOperators.Contains(NameOf(node.Label)))
        {
            return;
        }

        string[] bounds = [.. node.Literals.Where(l => l.Key is "k" or "min" or "max").Select(l => $"{l.Key}={l.Value}")];
        if (bounds.Length > 0)
        {
            text.Add($"Threshold: {string.Join(", ", bounds)}");
        }

        string[] values =
        [
            .. OperandPaths(tree, node).Select(p => results.TryGetValue(p, out EvaluationStep? s) ? s.Result : "skipped"),
        ];
        text.Add(
            $"Operands now: {values.Count(v => v == "True")} True, {values.Count(v => v == "False")} False, "
                + $"{values.Count(v => v == "Unknown")} Unknown, {values.Count(v => v == "skipped")} skipped"
        );
    }

    private static void ImpliesTable(Lines text)
    {
        text.Add("Truth table (rows: the condition, columns: the consequence):");
        text.Add("          T  F  U");
        text.Add("      T   T  F  U");
        text.Add("      F   T  T  T");
        text.Add("      U   T  U  U");
    }

    private static void IfTable(Lines text)
    {
        text.Add("If(condition, then, else):");
        text.Add("  condition True    -> then");
        text.Add("  condition False   -> else");
        text.Add("  condition Unknown -> that value when then and else are the same definite value, otherwise Unknown");
    }

    private static string BodyOf(RuleTree tree, string definitionPath)
    {
        ToolResult<string> body = tree.TextOf(definitionPath + ".body");
        return body.Succeeded ? body.Value.ReplaceLineEndings(" ") : "(not available)";
    }

    // The one definition that matches, or null when there is none. Shared by every lookup of a definition by name or by
    // one of its parameters, so the tree's definitions are searched the same way everywhere.
    private static DefinitionDeclaration? FindDefinition(RuleDefinitions? definitions, Func<DefinitionDeclaration, bool> match)
    {
        return definitions?.Definitions.FirstOrDefault(match);
    }

    // The table of IMPLIES and If, and the mark of an external operator.
    private static void OperatorExtras(Lines text, string name)
    {
        if (name.Equals("Implies", StringComparison.OrdinalIgnoreCase))
        {
            ImpliesTable(text);
        }
        else if (name.Equals("If", StringComparison.OrdinalIgnoreCase))
        {
            IfTable(text);
        }

        if (ExternalOperators.Contains(name))
        {
            text.Add("External operator: it reads how known a value is, so it is not information-monotone.");
        }
    }

    private static void DefinitionNode(Lines text, RuleTree tree, RuleTreeNode node)
    {
        text.Add($"Definition {node.Label[4..]}");
        text.Add($"Body: {BodyOf(tree, node.Path)}");
        string name = NameOf(node.Label[4..]);
        int uses = tree.Definitions?.Uses.Count(u => string.Equals(u.Name, name, StringComparison.OrdinalIgnoreCase)) ?? 0;
        text.Add($"Used {uses} time{(uses == 1 ? string.Empty : "s")} in the rule.");
        text.Add("No value of its own: each use is replaced by the body when the rule compiles.");
    }

    private static void UseNode(Lines text, RuleTree tree, RuleTreeNode node)
    {
        string use = node.Label[4..];
        string name = NameOf(use);
        text.Add($"Use of definition {use}");
        if (
            FindDefinition(tree.Definitions, d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)) is
            { } definition
        )
        {
            text.Add($"Parameters: {(definition.Parameters.Count == 0 ? "none" : string.Join(", ", definition.Parameters))}");
            text.Add($"Body: {BodyOf(tree, definition.Path)}");
        }
    }

    private void OperatorNode(Lines text, RuleTree tree, RuleTreeNode node)
    {
        string name = NameOf(node.Label);
        OutlineNode? outline = OutlineOf(tree).GetValueOrDefault(node.Path);
        IReadOnlyDictionary<string, EvaluationStep> results = EvaluationStepController.ResultsOf(tree, this.workbench.Outcome);
        text.Add(
            $"Operator {outline?.Label ?? node.Label} ({node.OperandCount} operand{(node.OperandCount == 1 ? string.Empty : "s")})"
        );
        if (outline is { Description.Length: > 0 })
        {
            text.Add(outline.Description);
        }

        OperatorExtras(text, name);
        text.Add($"Value now: {ValueWord(results.GetValueOrDefault(node.Path))}");
        OperandSummary(text, tree, node, results);
    }

    private void TermNode(Lines text, RuleTree tree, RuleTreeNode node)
    {
        AssignmentOutcome outcome = this.workbench.Outcome;
        EvaluationStep? step = EvaluationStepController.ResultsOf(tree, outcome).GetValueOrDefault(node.Path);
        string name = NameOf(node.Label);
        text.Add($"Term {node.Label}");
        if (OutlineOf(tree).GetValueOrDefault(node.Path) is { Description.Length: > 0 } outline)
        {
            text.Add(outline.Description);
        }

        this.PredicateFacts(text, name);
        text.Add($"Value now: {ValueWord(step)}");
        if (step?.Fault is { } fault)
        {
            text.Add($"Fault: {fault}");
        }

        foreach (Match from in FromCall().Matches(node.Label))
        {
            this.FromFacts(text, from.Value, node.Label);
        }
    }

    // What the predicate catalog, the schema file or the rule says about a predicate name.
    private void PredicateFacts(Lines text, string name)
    {
        PaletteEntry? entry = this.PaletteEntryFor(e =>
            e.Source is PaletteSource.Catalog or PaletteSource.SchemaFile or PaletteSource.Document
            && e.Name.Equals(name, StringComparison.Ordinal)
        );
        if (entry is null)
        {
            text.Add($"Predicate {name} is not in the catalog, the schema file or the rule.");
            return;
        }

        CatalogPredicate? catalog = PredicateCatalog.All.FirstOrDefault(p => p.Entry.Name == name);
        text.Add($"Predicate {name} ({(catalog is null ? entry.Source.ToString().ToLowerInvariant() : catalog.Family)})");
        if (entry.Description.Length > 0)
        {
            text.Add(entry.Description);
        }

        text.Add($"Arguments: {PredicateKinds(entry)}");
        if (catalog is { TakesNullBehavior: true })
        {
            text.Add("Null value: answers Unknown unless the host chose False.");
        }
    }

    // The entry of the palette that matches, searched once here so every caller shares the same chain.
    private PaletteEntry? PaletteEntryFor(Func<PaletteEntry, bool> match)
    {
        return StructureController.For(this.workbench).Palette().Entries.FirstOrDefault(match);
    }

    // The source, the path and the resolved value or the failure of a from(...) variable.
    private void FromFacts(Lines text, string call, string termLabel)
    {
        Match parts = FromParts().Match(call);
        text.Add(
            parts.Success
                ? $"Variable from source \"{parts.Groups[1].Value}\", path \"{parts.Groups[2].Value}\""
                : $"Variable {call}"
        );
        AssignmentOutcome outcome = this.workbench.Outcome;
        TermAssignment? term = outcome.Terms.FirstOrDefault(t =>
            t.Display == termLabel || t.Display.Contains(call, StringComparison.Ordinal)
        );
        if (term is null)
        {
            return;
        }

        if (term.Key is not null)
        {
            text.Add($"Resolved: {term.Key}");
            return;
        }

        string? fault = outcome
            .Faults.Where(f => f.StartsWith(term.Display + ":", StringComparison.Ordinal))
            .Select(f => f[(term.Display.Length + 1)..].Trim())
            .FirstOrDefault();
        text.Add($"Not resolved: {fault ?? "no data yet"}");
    }
}
