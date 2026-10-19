namespace TruthWeaver.Tool.Tui.Controllers;

using System.Text.RegularExpressions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Palette;
using TruthWeaver.Tool.Core.Tree;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Tool.Tui.Theme;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// Finds the token, symbol or diagnostic span under an offset of the editor text (the caret logic), and describes what
/// it finds. The word-character test and the subtree-aware span math live here once, for every caller.
/// </summary>
internal sealed partial class InspectController
{
    /// <summary>Describes the place at an offset of the editor text.</summary>
    /// <param name="offset">The offset into the text of the document.</param>
    /// <returns>The text, or <see langword="null"/> when the place has nothing to describe.</returns>
    public string? DescribeOffset(int offset)
    {
        return this.DescribeCharacter(offset);
    }

    /// <summary>
    /// Describes the part of the editor text that the cursor is on, for the inspect key. The character under the cursor
    /// wins when it has a description. Otherwise the character before the cursor decides, so a cursor right after a word
    /// (where it is after typing) or at the end of the text describes that word.
    /// </summary>
    /// <param name="caret">The offset of the cursor into the text of the document.</param>
    /// <returns>The text, or <see langword="null"/> when neither character has a description.</returns>
    public string? DescribeCaret(int caret)
    {
        int length = this.workbench.Document.Text.Length;
        caret = Math.Clamp(caret, 0, length);
        string? under = caret < length ? this.DescribeCharacter(caret) : null;
        return under ?? (caret > 0 ? this.DescribeCharacter(caret - 1) : null);
    }

    /// <summary>
    /// Gets where the part at an offset starts: the start of the word, symbol, note or error that covers the offset, or the
    /// offset itself when no part covers it. Every offset inside one part has the same start, so the hover can tell a move
    /// inside a part from a move to another part.
    /// </summary>
    /// <param name="offset">The offset into the text of the document.</param>
    /// <returns>The start of the part.</returns>
    public int PartStartAt(int offset)
    {
        string text = this.workbench.Document.Text;
        if (text.Length == 0)
        {
            return 0;
        }

        offset = Math.Clamp(offset, 0, text.Length - 1);
        Diagnostic? diagnostic = this.workbench.Document.Diagnostics.FirstOrDefault(d =>
            d.Span != SourceSpan.None && Covers(d.Span, offset, text.Length)
        );
        if (diagnostic is not null)
        {
            return Math.Clamp(diagnostic.Span.Start, 0, text.Length - 1);
        }

        if (
            this.workbench.Document.Format == RuleFormat.Text
            && SyntaxMarks.Compute(text) is { } marks
            && marks.At(offset) is { } kind
        )
        {
            return TokenAround(marks, text, offset, kind).Start;
        }

        return offset;
    }

    // Whether a character can be part of a word: a letter, a digit or an underscore. Shared by every scan for a word's
    // boundary or for the leading name of a label.
    private static bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
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
        if (kind != SyntaxKind.Description && IsWordChar(text[offset]))
        {
            while (start > 0 && IsWordChar(text[start - 1]))
            {
                start--;
            }

            while (end < text.Length && IsWordChar(text[end]))
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

    // The description of the one character at an offset and the token around it. An offset past the end counts as the last
    // character, which the hover uses for a point beyond the end of a line.
    private string? DescribeCharacter(int offset)
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

    private void OperatorWord(Lines text, string token)
    {
        string name = OperatorSpellings.GetValueOrDefault(token, token);

        // The description is the one of the operator table, which the Insert menu reads; the palette is the fallback.
        string? description =
            InsertItems
                .Operators.SelectMany(g => g.Items)
                .FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?.Description
            ?? this.PaletteEntryFor(e =>
                e.Source == PaletteSource.Operator && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
            )?.Description;
        text.Add(description is null ? $"Keyword {token}" : $"Operator {token}");
        if (description is { Length: > 0 })
        {
            text.Add(description);
        }

        OperatorExtras(text, name);
        if (StructureController.For(this.workbench).Tree is { } tree && OnlyOperator(tree, name) is { } node)
        {
            IReadOnlyDictionary<string, EvaluationStep> results = EvaluationStepController.ResultsOf(
                tree,
                this.workbench.Outcome
            );
            text.Add($"Value now: {ValueWord(results.GetValueOrDefault(node.Path))}");
            OperandSummary(text, tree, node, results);
        }
    }

    private void PredicateWord(Lines text, string token)
    {
        RuleTree? tree = StructureController.For(this.workbench).Tree;
        if (
            FindDefinition(tree?.Definitions, d => string.Equals(d.Name, token, StringComparison.OrdinalIgnoreCase)) is
            { } definition
        )
        {
            text.Add($"Definition {definition.Name}({string.Join(", ", definition.Parameters)})");
            text.Add($"Body: {BodyOf(tree!, definition.Path)}");
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

        RuleTree? tree = StructureController.For(this.workbench).Tree;
        RuleDefinitions? definitions = tree?.Definitions ?? this.workbench.Document.Definitions;
        DefinitionDeclaration? owner = FindDefinition(
            definitions,
            d => d.Parameters.Any(p => string.Equals(p, token, StringComparison.OrdinalIgnoreCase))
        );
        text.Add(owner is null ? $"Variable {token} (bound by a quantifier)" : $"Parameter {token} of definition {owner.Name}");
        text.Add(
            owner is null
                ? "It stands for each element of the collection in turn."
                : "Each use of the definition puts its argument in this place."
        );
    }
}
