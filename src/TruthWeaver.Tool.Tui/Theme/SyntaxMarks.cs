namespace TruthWeaver.Tool.Tui.Theme;

using TruthWeaver.Tool.Tui.Sessions;

/// <summary>
/// The syntax kind of each character of the rule text, so the editor can color predicates, operators, variables, constants
/// and comments while it draws. The classification is lexical: it reads the words, the symbols, the strings and the comments of
/// the text and does not need a rule that compiles, so it colors a rule that has errors as well. The workbench computes it
/// after the compile that follows a pause in typing, never on a key. A diagnostic mark wins over a syntax kind where the two
/// overlap (see <c>RuleEditorView</c>).
/// </summary>
/// <remarks>
/// The tokenizer of the library is internal to <c>TruthWeaver</c>, and only an explicit list of assemblies sees it
/// (an architecture test enforces that list), so this scanner reads the same lexical forms by itself: words and numbers,
/// strings with backslash escapes, <c>//</c> and <c>/* */</c> comments, and the symbolic operators. The operator and constant
/// words come from <see cref="InsertItems"/>, which takes them from the library.
/// </remarks>
internal sealed class SyntaxMarks
{
    // The symbols of the symbolic operators that are one character (the two-character ones are read as a pair).
    private const string OperatorSymbols = "!¬∧∨⊕→↔↑↓⊼⊽⊻⇒⇔";

    // The operator words that the Insert menu does not list: the aliases, the definition keyword and the quantifiers.
    private static readonly string[] ExtraOperatorWords = ["IFF", "XNOR", "LET", "FORALL", "EXISTS", "IN"];

    private static readonly Lazy<HashSet<string>> OperatorWords = new(() =>
        new HashSet<string>(
            InsertItems.Operators.SelectMany(g => g.Items).Select(i => i.Name).Concat(ExtraOperatorWords),
            StringComparer.OrdinalIgnoreCase
        )
    );

    private static readonly Lazy<HashSet<string>> ConstantWords = new(() =>
        new HashSet<string>(InsertItems.Constants.Select(i => i.Name), StringComparer.Ordinal)
    );

    // The single-entry cache of Compute. Per thread, so a test or a second thread never replaces the entry of another.
    [ThreadStatic]
    private static (string Text, SyntaxMarks Marks)? last;

    private readonly SyntaxKind?[] kinds;

    private SyntaxMarks(SyntaxKind?[] kinds)
    {
        this.kinds = kinds;
    }

    /// <summary>Gets marks that mark nothing.</summary>
    public static SyntaxMarks None { get; } = new([]);

    /// <summary>
    /// Computes the syntax kind of each character of a text. The last text is remembered per thread, so the hover, the caret
    /// and the term-name lookups of one text revision share one scan. The marks are immutable, so sharing them is safe.
    /// </summary>
    /// <param name="text">The rule text.</param>
    /// <returns>The marks. A character that is not part of a predicate, operator, variable, constant or comment has no kind.</returns>
    public static SyntaxMarks Compute(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // A string compare is cheaper than a scan, and it ends at once for the same instance.
        if (last is { } cached && string.Equals(cached.Text, text, StringComparison.Ordinal))
        {
            return cached.Marks;
        }

        SyntaxKind?[] kinds = new SyntaxKind?[text.Length];
        new Scanner(text, kinds).Run();
        SyntaxMarks marks = new(kinds);
        last = (text, marks);
        return marks;
    }

    /// <summary>Gets the syntax kind of a character.</summary>
    /// <param name="offset">The offset into the text.</param>
    /// <returns>The kind, or <see langword="null"/> when the character has none or the offset is outside the text.</returns>
    public SyntaxKind? At(int offset)
    {
        return offset >= 0 && offset < this.kinds.Length ? this.kinds[offset] : null;
    }

    // One pass over the text. The names that the text binds as variables (the parameters of a let and the variable of a
    // quantifier) are collected for the whole text, not by scope, which is enough to color a use.
    private sealed class Scanner(string text, SyntaxKind?[] kinds)
    {
        private readonly HashSet<string> bound = [with(StringComparer.OrdinalIgnoreCase)];

        // One entry for each open bracket: true for the parentheses of a call, where "name:" names an argument.
        private readonly Stack<bool> brackets = new();

        // 'let name(p, q) =': 1 after let, 2 after the name, 3 inside the parameter list.
        private int letState;

        // 'FORALL(x IN xs, ...)': 1 after the quantifier word, 2 after its parenthesis.
        private int quantifierState;
        private bool previousWasWord;
        private int position;

        public void Run()
        {
            while (this.position < text.Length)
            {
                char c = text[this.position];
                char next = this.position + 1 < text.Length ? text[this.position + 1] : '\0';
                if (char.IsWhiteSpace(c))
                {
                    this.position++;
                }
                else if (c == '/' && next == '/')
                {
                    int end = text.IndexOf('\n', this.position);
                    this.position = this.Fill(this.position, end < 0 ? text.Length : end, SyntaxKind.Description);
                }
                else if (c == '/' && next == '*')
                {
                    int end = text.IndexOf("*/", this.position + 2, StringComparison.Ordinal);
                    this.position = this.Fill(this.position, end < 0 ? text.Length : end + 2, SyntaxKind.Description);
                }
                else if (c == '"')
                {
                    this.position = this.SkipString(this.position);
                    this.Other();
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    this.Word();
                }
                else if (char.IsDigit(c))
                {
                    int end = this.position + 1;
                    while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] is '.' or '_'))
                    {
                        end++;
                    }

                    this.position = end;
                    this.Other();
                }
                else
                {
                    this.Symbol(c, next);
                }
            }
        }

        private static bool IsConstantWord(string word)
        {
            return ConstantWords.Value.Contains(word)
                || word.Equals("true", StringComparison.OrdinalIgnoreCase)
                || word.Equals("false", StringComparison.OrdinalIgnoreCase)
                || word.Equals("unknown", StringComparison.OrdinalIgnoreCase);
        }

        private void Word()
        {
            int start = this.position;
            int end = start + 1;
            while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
            {
                end++;
            }

            string word = text[start..end];
            int after = end;
            while (after < text.Length && char.IsWhiteSpace(text[after]))
            {
                after++;
            }

            char afterChar = after < text.Length ? text[after] : '\0';
            bool isArgumentName =
                afterChar == ':'
                && this.brackets.Count > 0
                && this.brackets.Peek()
                && !(after + 1 < text.Length && text[after + 1] == ':');

            SyntaxKind? kind = isArgumentName ? null : this.Classify(word, afterChar);
            if (kind is { } found)
            {
                _ = this.Fill(start, end, found);
            }

            this.UpdateStates(word, kind);
            this.previousWasWord = true;
            this.position = end;
        }

        private SyntaxKind? Classify(string word, char afterChar)
        {
            if (this.letState == 3 || this.quantifierState == 2)
            {
                // A parameter of a let, or the variable of a quantifier.
                return SyntaxKind.Variable;
            }

            if (this.letState == 1)
            {
                // The name that a let defines is the name of a term, so it is colored like a predicate.
                return SyntaxKind.Predicate;
            }

            if (word.Equals("from", StringComparison.OrdinalIgnoreCase) && afterChar == '(')
            {
                return SyntaxKind.Variable;
            }

            if (OperatorWords.Value.Contains(word))
            {
                return SyntaxKind.Operator;
            }

            if (afterChar != '(' && this.bound.Contains(word))
            {
                return SyntaxKind.Variable;
            }

            if (afterChar != '(' && IsConstantWord(word))
            {
                return SyntaxKind.Constant;
            }

            return SyntaxKind.Predicate;
        }

        private void UpdateStates(string word, SyntaxKind? kind)
        {
            if (this.letState == 3 || this.quantifierState == 2)
            {
                _ = this.bound.Add(word);
                this.quantifierState = 0;
            }
            else if (this.letState == 1)
            {
                this.letState = 2;
            }
            else
            {
                this.letState = word.Equals("let", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                this.quantifierState =
                    kind == SyntaxKind.Operator
                    && (
                        word.Equals("forall", StringComparison.OrdinalIgnoreCase)
                        || word.Equals("exists", StringComparison.OrdinalIgnoreCase)
                    )
                        ? 1
                        : 0;
            }
        }

        private void Symbol(char c, char next)
        {
            if ((c == '&' && next == '&') || (c == '|' && next == '|') || (c == '?' && next == '?'))
            {
                this.position = this.Fill(this.position, this.position + 2, SyntaxKind.Operator);
                this.Other();
                return;
            }

            if (OperatorSymbols.Contains(c, StringComparison.Ordinal) && !(c == '!' && next == '='))
            {
                this.position = this.Fill(this.position, this.position + 1, SyntaxKind.Operator);
                this.Other();
                return;
            }

            if (c is '(' or '[' or '{')
            {
                this.brackets.Push(c == '(' && this.previousWasWord);
                if (c == '(' && this.letState == 2)
                {
                    this.letState = 3;
                }
                else if (c == '(' && this.quantifierState == 1)
                {
                    this.quantifierState = 2;
                }
                else
                {
                    this.letState = 0;
                    this.quantifierState = 0;
                }
            }
            else if (c is ')' or ']' or '}')
            {
                if (this.brackets.Count > 0)
                {
                    _ = this.brackets.Pop();
                }

                this.letState = c == ')' && this.letState == 3 ? 2 : 0;
                this.quantifierState = 0;
            }
            else if (c != ',' || this.letState != 3)
            {
                this.letState = 0;
                this.quantifierState = 0;
            }

            this.position++;
            this.previousWasWord = false;
        }

        // A string or a number ends the run of words that a state follows.
        private void Other()
        {
            this.previousWasWord = false;
            this.letState = 0;
            this.quantifierState = 0;
        }

        private int SkipString(int start)
        {
            int i = start + 1;
            while (i < text.Length && text[i] != '"')
            {
                i += text[i] == '\\' ? 2 : 1;
            }

            return Math.Min(i + 1, text.Length);
        }

        private int Fill(int start, int end, SyntaxKind kind)
        {
            int stop = Math.Min(end, kinds.Length);
            for (int i = start; i < stop; i++)
            {
                kinds[i] = kind;
            }

            return stop;
        }
    }
}
