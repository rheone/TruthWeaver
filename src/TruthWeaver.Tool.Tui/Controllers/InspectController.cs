namespace TruthWeaver.Tool.Tui.Controllers;

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Documents;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Tool.Tui.Predicates;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The text of the inspect key and of the mouse hover: what a part of the rule means and what it does now. Both routes call
/// <see cref="DescribeNode"/> (a node of the tree) or <see cref="DescribeOffset"/> (a place in the editor text), so the
/// keyboard and the hover never differ. The descriptions come from the library: the outline of the compiled rule (the
/// operator table), the predicate catalog and the palette, not from copies.
/// </summary>
internal sealed partial class InspectController
{
    // The operators that count their operands, so the text adds how many operands are True, False and Unknown now.
    private static readonly HashSet<string> CardinalityOperators = new(
        ["any", "all", "none", "exactlyone", "atleast", "atmost", "exactly", "greaterthan", "lessthan", "between"],
        StringComparer.OrdinalIgnoreCase
    );

    // The external operators (ADR-0005): they read how known a value is, so they are not information-monotone.
    private static readonly HashSet<string> ExternalOperators = new(
        ["istrue", "isfalse", "isunknown", "isknown", "coalesce"],
        StringComparer.OrdinalIgnoreCase
    );

    // The other spellings of an operator in rule text: the aliases and the symbols. The name is the word of the palette.
    private static readonly Dictionary<string, string> OperatorSpellings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IFF"] = "Equivalent",
        ["XNOR"] = "Equivalent",
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
        ["↔"] = "Equivalent",
        ["⇔"] = "Equivalent",
        ["↑"] = "Nand",
        ["⊼"] = "Nand",
        ["↓"] = "Nor",
        ["⊽"] = "Nor",
    };

    private static readonly ConditionalWeakTable<Workbench, InspectController> Controllers = [];

    private readonly Workbench workbench;

    /// <summary>Initializes a new instance of the <see cref="InspectController"/> class.</summary>
    /// <param name="workbench">The shared state.</param>
    public InspectController(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        this.workbench = workbench;
    }

    /// <summary>Gets the controller of a workbench. The window, the keys and the hover use the same controller.</summary>
    /// <param name="workbench">The workbench.</param>
    /// <returns>The controller.</returns>
    public static InspectController For(Workbench workbench)
    {
        ArgumentNullException.ThrowIfNull(workbench);

        return Controllers.GetValue(workbench, key => new InspectController(key));
    }

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
                ConstantNode(text, node, Results(tree, this.workbench.Outcome).GetValueOrDefault(node.Path));
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

    /// <summary>Describes the place at an offset of the editor text.</summary>
    /// <param name="offset">The offset into the text of the document.</param>
    /// <returns>The text, or <see langword="null"/> when the place has nothing to describe.</returns>
    public string? DescribeOffset(int offset)
    {
        string text = this.workbench.Document.Text;
        if (text.Length == 0)
        {
            return null;
        }

        offset = Math.Clamp(offset, 0, text.Length - 1);

        // A diagnostic wins over the syntax, as its mark does in the editor.
        Diagnostic? diagnostic = this
            .workbench.Document.Diagnostics.Where(d => d.Span != SourceSpan.None && Covers(d.Span, offset, text.Length))
            .OrderByDescending(d => d.Severity)
            .FirstOrDefault();
        if (diagnostic is not null)
        {
            Lines found = [];
            DiagnosticText(found, diagnostic);
            return found.ToString();
        }

        // The syntax kinds are lexical rule text; a JSON or YAML document is inspected in the tree pane.
        if (this.workbench.Document.Format != RuleFormat.Text)
        {
            return null;
        }

        SyntaxMarks marks = SyntaxMarks.Compute(text);
        if (marks.At(offset) is not { } kind)
        {
            return null;
        }

        (int start, int end) = TokenAround(marks, text, offset, kind);
        string token = text[start..end];
        Lines lines = [];
        switch (kind)
        {
            case SyntaxKind.Description:
                lines.Add("Note");
                lines.Add(token.Trim());
                break;
            case SyntaxKind.Operator:
                this.OperatorWord(lines, token);
                break;
            case SyntaxKind.Predicate:
                this.PredicateWord(lines, token);
                break;
            case SyntaxKind.Variable:
                this.VariableWord(lines, text, start, end);
                break;
            default:
                ConstantWord(lines, token);
                break;
        }

        return lines.ToString();
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
        while (end < label.Length && (char.IsLetterOrDigit(label[end]) || label[end] == '_'))
        {
            end++;
        }

        return label[..end];
    }

    private static bool Covers(SourceSpan span, int offset, int length)
    {
        // An empty span marks the character at its start, or the last character at the end of the text.
        int start = Math.Clamp(span.Start, 0, length - 1);
        int end = Math.Clamp(span.End, start + 1, length);
        return offset >= start && offset < end;
    }

    // The characters of the token at an offset: a word, or a run of one kind of symbol or comment.
    private static (int Start, int End) TokenAround(SyntaxMarks marks, string text, int offset, SyntaxKind kind)
    {
        int start = offset;
        int end = offset + 1;
        if (kind != SyntaxKind.Description && (char.IsLetterOrDigit(text[offset]) || text[offset] == '_'))
        {
            while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_'))
            {
                start--;
            }

            while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
            {
                end++;
            }

            return (start, end);
        }

        while (start > 0 && marks.At(start - 1) == kind)
        {
            start--;
        }

        while (end < text.Length && marks.At(end) == kind)
        {
            end++;
        }

        // A symbol is one operator; "&&", "||" and "??" are two characters of one kind.
        return kind == SyntaxKind.Operator ? SymbolAt(text, offset) : (start, end);
    }

    private static (int Start, int End) SymbolAt(string text, int offset)
    {
        static bool IsPair(string text, int at)
        {
            return at >= 0 && at + 1 < text.Length && text.AsSpan(at, 2) is "&&" or "||" or "??";
        }

        if (IsPair(text, offset - 1))
        {
            return (offset - 1, offset + 1);
        }

        return IsPair(text, offset) ? (offset, offset + 2) : (offset, offset + 1);
    }

    private static void DiagnosticText(Lines text, Diagnostic diagnostic)
    {
        text.Add($"{diagnostic.Severity} {diagnostic.Code}");
        text.Add(diagnostic.Message);
        if (diagnostic.Expected is not null || diagnostic.Found is not null)
        {
            text.Add($"Expected: {diagnostic.Expected ?? "-"}; found: {diagnostic.Found ?? "-"}");
        }

        if (diagnostic.Suggestion is { } suggestion)
        {
            text.Add($"Suggestion: {suggestion.Text}");
        }

        text.Add("Documentation: docs/diagnostics.md");
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

    // The nodes of the rule with the value each returned, by path. A tree of a definition body has none.
    private static IReadOnlyDictionary<string, EvaluationStep> Results(RuleTree tree, AssignmentOutcome outcome)
    {
        return EvaluationStepController.ResultsOf(tree, outcome);
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

        int End(int at)
        {
            int next = at + 1;
            for (int i = 0; i < tree.Nodes[at].OperandCount && next < tree.Nodes.Count; i++)
            {
                next = End(next);
            }

            return next;
        }

        int Visit(int at, OutlineNode outline)
        {
            RuleTreeNode node = tree.Nodes[at];
            map[node.Path] = outline;
            if (outline.Operands.Count != node.OperandCount)
            {
                return End(at);
            }

            int next = at + 1;
            foreach (OutlineNode child in outline.Operands)
            {
                next = Visit(next, child);
            }

            return next;
        }
    }

    // The paths of the operands of a node, in order.
    private static List<string> OperandPaths(RuleTree tree, RuleTreeNode node)
    {
        List<string> paths = [];
        int at = tree.Nodes.ToList().FindIndex(n => ReferenceEquals(n, node));
        int next = at + 1;
        for (int i = 0; at >= 0 && i < node.OperandCount && next < tree.Nodes.Count; i++)
        {
            paths.Add(tree.Nodes[next].Path);
            next = EndOf(tree, next);
        }

        return paths;
    }

    private static int EndOf(RuleTree tree, int at)
    {
        int next = at + 1;
        for (int i = 0; i < tree.Nodes[at].OperandCount && next < tree.Nodes.Count; i++)
        {
            next = EndOf(tree, next);
        }

        return next;
    }

    private static string PredicateKinds(PaletteEntry entry)
    {
        return entry.Arguments.Count == 0
            ? "none"
            : string.Join(
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

    // from("source", "path"): the call as it is written in rule text.
    [GeneratedRegex("""from\(\s*"(?:[^"\\]|\\.)*"\s*,\s*"(?:[^"\\]|\\.)*"\s*\)""", RegexOptions.IgnoreCase)]
    private static partial Regex FromCall();

    [GeneratedRegex("""from\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\)""", RegexOptions.IgnoreCase)]
    private static partial Regex FromParts();

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
            tree.Definitions?.Definitions.FirstOrDefault(d =>
                string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)
            ) is
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
        IReadOnlyDictionary<string, EvaluationStep> results = Results(tree, this.workbench.Outcome);
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
        EvaluationStep? step = Results(tree, outcome).GetValueOrDefault(node.Path);
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
        PaletteEntry? entry = StructureController
            .For(this.workbench)
            .Palette()
            .Entries.FirstOrDefault(e =>
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

    // The analyzer findings: at the root the ones that name no node (a whole-rule finding), at a node the ones of its path.
    private void Findings(Lines text, string path, bool isRoot)
    {
        Diagnostic[] findings =
        [
            .. this.workbench.Document.Diagnostics.Where(d =>
                d.Severity != DiagnosticSeverity.Error && (d.Path == path || (isRoot && d.Path is null))
            ),
        ];
        if (findings.Length == 0)
        {
            return;
        }

        text.Add("Findings:");
        foreach (Diagnostic finding in findings.Take(5))
        {
            text.Add($"  {finding.Code}: {finding.Message}");
        }

        if (findings.Length > 5)
        {
            text.Add($"  and {findings.Length - 5} more (see the Diagnostics pane)");
        }
    }

    private void OperatorWord(Lines text, string token)
    {
        string name = OperatorSpellings.GetValueOrDefault(token, token);

        // The description is the one of the operator table, which the Insert menu reads; the palette is the fallback.
        string? description =
            InsertItems
                .Operators.SelectMany(g => g.Items)
                .FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?.Description
            ?? StructureController
                .For(this.workbench)
                .Palette()
                .Entries.FirstOrDefault(e =>
                    e.Source == PaletteSource.Operator && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                )
                ?.Description;
        text.Add(description is null ? $"Keyword {token}" : $"Operator {token}");
        if (description is { Length: > 0 })
        {
            text.Add(description);
        }

        OperatorExtras(text, name);
        if (this.StructureTree() is { } tree && OnlyOperator(tree, name) is { } node)
        {
            IReadOnlyDictionary<string, EvaluationStep> results = Results(tree, this.workbench.Outcome);
            text.Add($"Value now: {ValueWord(results.GetValueOrDefault(node.Path))}");
            OperandSummary(text, tree, node, results);
        }
    }

    private void PredicateWord(Lines text, string token)
    {
        RuleTree? tree = this.StructureTree();
        if (
            tree?.Definitions?.Definitions.FirstOrDefault(d =>
                string.Equals(d.Name, token, StringComparison.OrdinalIgnoreCase)
            ) is
            { } definition
        )
        {
            text.Add($"Definition {definition.Name}({string.Join(", ", definition.Parameters)})");
            text.Add($"Body: {BodyOf(tree, definition.Path)}");
            return;
        }

        this.PredicateFacts(text, token);
        AssignmentOutcome outcome = this.workbench.Outcome;
        TermAssignment[] terms =
        [
            .. outcome.Terms.Where(t =>
                t.Display.StartsWith(token + "(", StringComparison.Ordinal) || t.Display.Equals(token, StringComparison.Ordinal)
            ),
        ];
        foreach (TermAssignment term in terms.Take(3))
        {
            text.Add(
                $"Value now: {(term.Key is null && term.HasDataReference ? "Unknown (no data yet)" : term.Value.ToString())}  for {term.Display}"
            );
        }

        if (terms.Length > 3)
        {
            text.Add($"and {terms.Length - 3} more terms use it");
        }
    }

    private void VariableWord(Lines text, string source, int start, int end)
    {
        string token = source[start..end];
        if (token.Equals("from", StringComparison.OrdinalIgnoreCase))
        {
            Match call = FromCall().Match(source, start);
            if (call.Success && call.Index == start)
            {
                this.FromFacts(text, call.Value, call.Value);
                return;
            }
        }

        RuleTree? tree = this.StructureTree();
        RuleDefinitions? definitions = tree?.Definitions ?? this.workbench.Document.Definitions;
        DefinitionDeclaration? owner = definitions.Definitions.FirstOrDefault(d =>
            d.Parameters.Any(p => string.Equals(p, token, StringComparison.OrdinalIgnoreCase))
        );
        text.Add(owner is null ? $"Variable {token} (bound by a quantifier)" : $"Parameter {token} of definition {owner.Name}");
        text.Add(
            owner is null
                ? "It stands for each element of the collection in turn."
                : "Each use of the definition puts its argument in this place."
        );
    }

    private RuleTree? StructureTree()
    {
        return StructureController.For(this.workbench).Tree;
    }

    // The lines of one description. The text is the lines joined with line feeds.
    private sealed class Lines : List<string>
    {
        public override string ToString()
        {
            StringBuilder builder = new();
            foreach (string line in this)
            {
                _ = builder.AppendLine(line);
            }

            return builder.ToString().TrimEnd().ReplaceLineEndings("\n");
        }
    }
}
