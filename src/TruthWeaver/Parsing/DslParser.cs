namespace TruthWeaver.Parsing;

using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;

/// <summary>
/// Hand-written recursive-descent parser for the canonical word-operator DSL (ADR-0003): word
/// operators only, matched case-insensitively; precedence <c>NOT</c> &gt; <c>AND</c> &gt; <c>OR</c>;
/// <c>XOR</c> mixed with <c>AND</c>/<c>OR</c> at the same syntactic level without parentheses is
/// rejected rather than resolved by a precedence guess. Never throws for a syntax error. It reports
/// a <see cref="DiagnosticCodes.SyntaxError"/> diagnostic and recovers with an <see cref="ErrorNode"/>
/// so the rest of the source still gets parsed and can surface further diagnostics.
/// </summary>
internal sealed class DslParser
{
    // Infix operators that sit outside the NOT > AND > OR precedence chain: they may not be mixed with
    // each other or with AND/OR at one nesting level without parentheses (ADR-0005 decision 8).
    // COALESCE is infix only as the symbol ?? (the word is a function call), and, being associative, a chain of it
    // folds into one n-ary node instead of being rejected like the binary-only operators.
    private static readonly string[] InfixOperators = ["XOR", "EQUIVALENT", "IMPLIES", "NAND", "NOR", "COALESCE"];

    // The inspection function calls, each taking exactly one operand (checked by the compiler).
    private static readonly (string Keyword, InspectionKind Kind)[] InspectionKeywords =
    [
        ("ISTRUE", InspectionKind.IsTrue),
        ("ISFALSE", InspectionKind.IsFalse),
        ("ISUNKNOWN", InspectionKind.IsUnknown),
        ("ISKNOWN", InspectionKind.IsKnown),
    ];

    private readonly string source;
    private readonly IReadOnlyList<Token> tokens;
    private readonly List<Diagnostic> diagnostics;

    // Every 'let' name declared anywhere in the document (ADR-0008), collected by CollectDefinitionNames before any
    // body is parsed so a use may name a definition declared later in the same document, not only an earlier one.
    private readonly IReadOnlySet<string> definitionNames;

    // The extent, delimiters included, of each node written inside a group: a comment before the opener is that node's
    // note, so the note attacher needs to know where the group began. Keyed by reference, as two equal nodes are distinct.
    private readonly Dictionary<RuleNode, (int Start, int End)> groups = [with(ReferenceEqualityComparer.Instance)];

    // Guards the recursive-descent expression chain (ParseExpression -> ParseOrExpression -> ... ->
    // ParseNotExpression -> ParsePrimary) against unbounded recursion: a parenthesized group, a ternary branch, a
    // call-like operand list (ANY/ALL/threshold/BETWEEN/FORALL/EXISTS), a 'let' body, a use's arguments and one
    // step of a NOT chain each count as one level. Without this, a deeply nested or long input (hundreds of
    // thousands of '(' or 'NOT') overflows the call stack with an uncatchable StackOverflowException before
    // RuleNodeCompiler.Build ever gets to apply CompilerOptions.MaxDepth to the finished tree. This counter is
    // intentionally coarser than that later check (redundant parentheses count here even though they add no tree
    // depth), trading a few legal-but-pointless inputs for a guard simple enough to reason about.
    private readonly int maxDepth;

    // The current definition's declared parameters while its own body is being parsed, so a bare identifier matching
    // one is read as a ParameterReferenceNode rather than a term or a use; null outside a definition's body (ADR-0008
    // decision 5: definitions do not nest, so this is never more than one level deep).
    private IReadOnlySet<string>? currentParameters;
    private int position;
    private int depth;

    // How many delimiters ('(', '[', '{', or a stand-in for a missing call '(') are open at the cursor: each opener
    // consumed adds one and each ExpectClose, which every opener is paired with, removes one. Error recovery reads it
    // so a closer that an enclosing group is waiting for is left for that group's ExpectClose instead of being skipped.
    private int openGroups;
    private bool maxDepthReported;

    // The diagnostics count at the moment the MaxDepthExceeded diagnostic was added. Once the guard trips, the
    // recursive-descent machinery unwinds with tokens left unconsumed wherever it stopped early, which would
    // otherwise cost a few bogus "expected closer"/"unexpected token" diagnostics purely as an artifact of bailing
    // out mid-expression. RuleNodeCompiler.Build's own MaxDepth check has no such artifact (it just returns a
    // placeholder and the enclosing operators build normally around it), so Parse() truncates back to this count
    // to give the same single-diagnostic contract: callers (e.g. the validation tool) that treat "every error is a
    // limit code" as "the rule is otherwise fine, just too deep" depend on that.
    private int diagnosticsCeiling = int.MaxValue;

    private DslParser(
        string source,
        IReadOnlyList<Token> tokens,
        List<Diagnostic> diagnostics,
        IReadOnlySet<string> definitionNames,
        int maxDepth
    )
    {
        this.source = source;
        this.tokens = tokens;
        this.diagnostics = diagnostics;
        this.definitionNames = definitionNames;
        this.maxDepth = maxDepth;
    }

    private Token Current => this.tokens[this.position];

    /// <summary>Determines whether a bare identifier is a reserved DSL keyword, ignoring case, and therefore cannot be a predicate name.</summary>
    /// <param name="identifier">The identifier text.</param>
    /// <returns><see langword="true"/> if the identifier is reserved.</returns>
    public static bool IsReservedWord(string identifier)
    {
        return ReservedWords.Contains(identifier);
    }

    /// <summary>Determines whether <paramref name="identifier"/> matches the DSL's identifier grammar: a letter or '_' first, then letters, digits or '_'.</summary>
    /// <param name="identifier">The candidate name.</param>
    /// <returns><see langword="true"/> if <paramref name="identifier"/> is a syntactically valid identifier.</returns>
    public static bool IsValidIdentifier(string identifier)
    {
        return IdentifierSyntax.IsValidIdentifier(identifier);
    }

    /// <summary>Parses DSL rule text into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="source">The rule text.</param>
    /// <param name="maxDepth">
    /// The nesting bound for the recursive-descent grammar (groups, call-like operand lists, ternary branches and
    /// NOT chains), matching <see cref="TruthWeaver.Compilation.CompilerOptions.MaxDepth"/>'s default. Exceeding it
    /// is reported as <see cref="DiagnosticCodes.MaxDepthExceeded"/>, the same code
    /// <c>RuleNodeCompiler.Build</c> reports for a built tree that is too deep.
    /// </param>
    /// <returns>The parsed root node and the diagnostics raised while parsing.</returns>
    public static (RuleNode Root, IReadOnlyList<Diagnostic> Diagnostics) Parse(string source, int maxDepth = 32)
    {
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics, long _) = ParseCore(source, maxDepth);
        return (root, diagnostics);
    }

    /// <summary>
    /// <see cref="Parse"/>, plus <see cref="NoteAttacher.Attach"/>'s step count: a deterministic, machine-independent
    /// proxy for comment-to-note attachment's cost, for a test to assert it scales linearly with the input rather
    /// than quadratically (a wall-clock measurement of the same thing is noisy under load). Test-only; production
    /// code has no use for the count and calls <see cref="Parse"/>.
    /// </summary>
    /// <param name="source">The rule text.</param>
    /// <param name="maxDepth">As for <see cref="Parse"/>.</param>
    /// <returns>The parsed root node, the diagnostics, and the note-attachment step count.</returns>
    internal static (RuleNode Root, IReadOnlyList<Diagnostic> Diagnostics, long NoteAttachSteps) ParseWithNoteAttachSteps(
        string source,
        int maxDepth = 32
    )
    {
        return ParseCore(source, maxDepth);
    }

    private static (RuleNode Root, IReadOnlyList<Diagnostic> Diagnostics, long NoteAttachSteps) ParseCore(
        string source,
        int maxDepth
    )
    {
        Lexer lexer = new(source);
        IReadOnlyList<Token> tokens = lexer.Tokenize();
        List<Diagnostic> diagnostics = [.. lexer.Diagnostics];
        DslParser parser = new(source, tokens, diagnostics, CollectDefinitionNames(tokens), maxDepth);

        List<DefinitionNode> definitions = [];
        while (parser.IsKeyword("LET"))
        {
            definitions.Add(parser.ParseDefinition());
        }

        RuleNode body = parser.ParseExpression();
        if (IsGroupCloser(parser.Current.Kind))
        {
            // Nothing is open at the top level, so a closer here never had an opener.
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.SyntaxError,
                    $"Unexpected closing '{parser.Current.Text}' with no matching opener.",
                    parser.Current.Span,
                    expected: "the end of the rule",
                    found: DescribeFound(parser.Current)
                )
            );
        }
        else if (parser.Current.Kind != TokenKind.Eof)
        {
            diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.SyntaxError,
                    $"Unexpected token '{parser.Current.Text}' after end of expression.",
                    parser.Current.Span,
                    expected: "an operator or the end of the rule",
                    found: DescribeFound(parser.Current),
                    suggestion: SuggestInfixWord(parser.Current)
                )
            );
        }

        RuleNode root =
            definitions.Count == 0
                ? body
                : new RuleDocumentNode(definitions, body, SpanCovering(definitions[0].Span.Start, body.Span.End));
        long noteAttachSteps = NoteAttacher.Attach(root, lexer.Comments, tokens, source, parser.groups);

        // Drop whatever "expected closer"/"unexpected token" noise the unwind above added once the depth guard
        // tripped (see the field comment on diagnosticsCeiling): it is an artifact of bailing out mid-expression,
        // not a second real problem with the rule.
        if (diagnostics.Count > parser.diagnosticsCeiling)
        {
            diagnostics.RemoveRange(parser.diagnosticsCeiling, diagnostics.Count - parser.diagnosticsCeiling);
        }

        return (root, diagnostics, noteAttachSteps);
    }

    /// <summary>
    /// Scans the token stream for every <c>let name</c> declared at the top level, without parsing bodies, so a use may
    /// name a definition declared later in the document (ADR-0008 decision 2 does not require declaration order). Each
    /// <c>let</c> statement is skipped up to its own top-level <c>;</c>, honoring nesting depth, so a definition's body
    /// cannot hide another <c>let</c> keyword from this scan.
    /// </summary>
    private static IReadOnlySet<string> CollectDefinitionNames(IReadOnlyList<Token> tokens)
    {
        HashSet<string> names = [with(StringComparer.OrdinalIgnoreCase)];
        int i = 0;
        while (
            i < tokens.Count
            && tokens[i].Kind == TokenKind.Identifier
            && string.Equals(tokens[i].Text, "LET", StringComparison.OrdinalIgnoreCase)
        )
        {
            i++;
            if (i < tokens.Count && tokens[i].Kind == TokenKind.Identifier)
            {
                names.Add(tokens[i].Text);
                i++;
            }

            int depth = 0;
            while (i < tokens.Count)
            {
                TokenKind kind = tokens[i].Kind;
                if (IsGroupOpener(kind))
                {
                    depth++;
                }
                else if (IsGroupCloser(kind))
                {
                    depth--;
                }
                else if (kind == TokenKind.Semicolon && depth == 0)
                {
                    i++;
                    break;
                }
                else if (kind == TokenKind.Eof)
                {
                    break;
                }

                i++;
            }
        }

        return names;
    }

    /// <summary>Describes a token for a diagnostic's <c>Found</c>: its text in quotes, or "end of rule" for the end of input.</summary>
    private static string DescribeFound(Token token)
    {
        return token.Kind == TokenKind.Eof ? "end of rule" : $"'{token.Text}'";
    }

    /// <summary>Suggests the nearest word operator when the token is a word that is probably a misspelt one.</summary>
    private static DiagnosticSuggestion? SuggestInfixWord(Token token)
    {
        return token.Kind == TokenKind.Identifier ? NameSuggester.Suggest(token.Text, DslVocabulary.InfixWords) : null;
    }

    private static bool IsGroupOpener(TokenKind kind)
    {
        return kind is TokenKind.LParen or TokenKind.LBracket or TokenKind.LBrace;
    }

    private static bool IsGroupCloser(TokenKind kind)
    {
        return kind is TokenKind.RParen or TokenKind.RBracket or TokenKind.RBrace;
    }

    /// <summary>Gets the closing delimiter that pairs with an opening one.</summary>
    private static char ClosingFor(TokenKind opener)
    {
        return opener switch
        {
            TokenKind.LBracket => ']',
            TokenKind.LBrace => '}',
            _ => ')',
        };
    }

    private static TokenKind ExpectedCloserKind(TokenKind opener)
    {
        return opener switch
        {
            TokenKind.LBracket => TokenKind.RBracket,
            TokenKind.LBrace => TokenKind.RBrace,
            _ => TokenKind.RParen,
        };
    }

    private static SourceSpan SpanCovering(int start, int end)
    {
        return new(start, Math.Max(0, end - start));
    }

    /// <summary>Gets the spelling used in diagnostics for an infix operator: its symbol for COALESCE, else its name.</summary>
    private static string DisplayName(string infixOperator)
    {
        return infixOperator == "COALESCE" ? "??" : infixOperator;
    }

    /// <summary>
    /// Maps a symbolic operator to the named operator it aliases, so the rest of the parser only ever
    /// reasons about named operators and notation can never change the resulting tree.
    /// </summary>
    private static string? SymbolAlias(string symbol)
    {
        return OperatorDefinitions.TryGetBySymbol(symbol, out OperatorDefinition? definition) ? definition.LabelTemplate : null;
    }

    /// <summary>
    /// Maps a word alias to the canonical operator it stands for: <c>IFF</c> and the legacy <c>XNOR</c> both mean
    /// <c>EQUIVALENT</c> (ADR-0005 decision 5), so persisted rules written with <c>XNOR</c> keep compiling.
    /// </summary>
    private static string? WordAlias(string word)
    {
        return word.ToUpperInvariant() switch
        {
            "IFF" or "XNOR" => "EQUIVALENT",
            _ => null,
        };
    }

    /// <summary>Whether a word is an infix operator word (<c>AND</c>, <c>OR</c>, <c>XOR</c> and the rest, aliases included), ignoring case.</summary>
    private static bool IsInfixWord(string word)
    {
        return DslVocabulary.InfixWords.Contains(word, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Whether a token can begin an operand: a word, a group opener or the symbolic NOT.</summary>
    private static bool CanStartOperand(Token token)
    {
        return token.Kind == TokenKind.Identifier
            || IsGroupOpener(token.Kind)
            || (token.Kind == TokenKind.Operator && SymbolAlias(token.Text) == "NOT");
    }

    private bool IsKeyword(string keyword)
    {
        return this.Current.Kind switch
        {
            TokenKind.Identifier => string.Equals(this.Current.Text, keyword, StringComparison.OrdinalIgnoreCase)
                || WordAlias(this.Current.Text) == keyword,
            TokenKind.Operator => SymbolAlias(this.Current.Text) == keyword,
            _ => false,
        };
    }

    private bool TryConsumeKeyword(string keyword)
    {
        if (!this.IsKeyword(keyword))
        {
            return false;
        }

        this.position++;
        return true;
    }

    /// <summary>
    /// Parses one complete expression: an <c>OR</c>-level expression, optionally the condition of the ternary
    /// <c>condition ? whenTrue : whenFalse</c>. The ternary is not an operand of a bare infix expression and, like every infix operator
    /// outside <c>NOT</c> &gt; <c>AND</c> &gt; <c>OR</c>, may not be mixed with another at one level without parentheses
    /// (ADR-0005 decision 8): its condition and both branches must each be a single operand or a parenthesized group.
    /// Every place that accepts a full expression (the root, parentheses, call arguments) goes through here.
    /// </summary>
    private RuleNode ParseExpression()
    {
        int start = this.position;
        (RuleNode condition, string? bareInfix) = this.ParseOrExpression();
        return this.Current.Kind == TokenKind.Question ? this.ParseTernaryTail(condition, bareInfix, start) : condition;
    }

    /// <summary>
    /// Parses <c>? whenTrue : whenFalse</c> after an already parsed condition. A condition or branch that is a bare
    /// <c>AND</c>/<c>OR</c> chain or infix expression, or that is itself an unparenthesized ternary, is reported as
    /// ambiguous at its own span; a nested ternary is still parsed so later problems surface in the same pass.
    /// </summary>
    private RuleNode ParseTernaryTail(RuleNode condition, string? conditionBare, int conditionStart, bool checkCondition = true)
    {
        if (checkCondition)
        {
            this.ReportBareTernaryPart(condition, conditionBare, conditionStart, "condition");
        }

        this.position++; // The '?'.
        int trueStart = this.position;
        (RuleNode whenTrue, string? trueBare) = this.ParseOrExpression();
        this.ReportBareTernaryPart(whenTrue, trueBare, trueStart, "true branch");
        if (this.Current.Kind == TokenKind.Question)
        {
            whenTrue = this.ParseNestedTernary(whenTrue, trueStart);
        }

        this.Expect(TokenKind.Colon, "':'");
        int falseStart = this.position;
        (RuleNode whenFalse, string? falseBare) = this.ParseOrExpression();
        this.ReportBareTernaryPart(whenFalse, falseBare, falseStart, "false branch");
        if (this.Current.Kind == TokenKind.Question)
        {
            whenFalse = this.ParseNestedTernary(whenFalse, falseStart);
        }

        return new IfNode([condition, whenTrue, whenFalse], SpanCovering(condition.Span.Start, whenFalse.Span.End));
    }

    /// <summary>Reports an unparenthesized ternary nested in a branch (<c>a ? b : c ? d : e</c>) and parses it for recovery.</summary>
    private RuleNode ParseNestedTernary(RuleNode nestedCondition, int nestedStart)
    {
        const string message =
            "Mixing ?: with another ?: at the same level requires explicit parentheses. "
            + "Add parentheses around the nested conditional to say how the branches group.";
        this.ReportAmbiguousMixing(
            this.Current.Span,
            message,
            "?: next to ?: without parentheses",
            "Add parentheses around the nested conditional to say how the branches group."
        );

        // The nested condition was already checked as the enclosing branch.
        return this.ParseTernaryTail(nestedCondition, null, nestedStart, checkCondition: false);
    }

    /// <summary>
    /// Reports a ternary operand that sits at the same level as the ternary without parentheses when it is an infix
    /// expression or a bare <c>AND</c>/<c>OR</c> chain. <paramref name="startToken"/> is the index of the operand's first
    /// token, used to tell <c>a AND b</c> from <c>(a AND b)</c> (the node does not retain its parentheses).
    /// </summary>
    private void ReportBareTernaryPart(RuleNode part, string? bareInfix, int startToken, string role)
    {
        string? bare = bareInfix is not null
            ? DisplayName(bareInfix)
            : part switch
            {
                AndNode when !this.IsWrappedInParentheses(startToken) => "AND",
                OrNode when !this.IsWrappedInParentheses(startToken) => "OR",
                _ => null,
            };
        if (bare is not null)
        {
            string message =
                $"Mixing ?: with {bare} at the same level requires explicit parentheses. "
                + $"Add parentheses around the {bare} expression used as the {role} to say which operator applies first.";
            this.ReportAmbiguousMixing(
                part.Span,
                message,
                $"?: next to {bare} without parentheses",
                this.WrapHint(bare, part.Span)
            );
        }
    }

    /// <summary>
    /// Whether the tokens from <paramref name="startToken"/> up to the cursor are one grouped expression: they open with
    /// '(', '[' or '{' whose matching closer is the last token consumed (so <c>(a) AND (b)</c> is not wrapped). Any
    /// delimiter kind counts, because the three are interchangeable.
    /// </summary>
    private bool IsWrappedInParentheses(int startToken)
    {
        if (!IsGroupOpener(this.tokens[startToken].Kind))
        {
            return false;
        }

        int bracketDepth = 0;
        for (int i = startToken; i < this.position; i++)
        {
            bracketDepth += this.tokens[i].Kind switch
            {
                TokenKind.LParen or TokenKind.LBracket or TokenKind.LBrace => 1,
                TokenKind.RParen or TokenKind.RBracket or TokenKind.RBrace => -1,
                _ => 0,
            };
            if (bracketDepth == 0)
            {
                return i == this.position - 1;
            }
        }

        return false;
    }

    private (RuleNode Node, string? BareInfix) ParseOrExpression()
    {
        if (!this.TryEnterNesting())
        {
            return (new ErrorNode(this.Current.Span), null);
        }

        try
        {
            return this.ParseAndOrChain("OR", this.ParseAndExpression, (operands, span) => new OrNode(operands, span));
        }
        finally
        {
            this.depth--;
        }
    }

    /// <summary>
    /// Enters one level of the recursive-descent expression chain. Returns <see langword="false"/> once
    /// <see cref="maxDepth"/> nesting levels are already open, in which case the caller must not descend any
    /// further (it should return a placeholder instead, without a matching exit: a failed entry never happened).
    /// The <see cref="DiagnosticCodes.MaxDepthExceeded"/> diagnostic is reported only once, no matter how many
    /// guarded call sites are still unwinding above the first one to trip.
    /// </summary>
    private bool TryEnterNesting()
    {
        this.depth++;
        if (this.depth <= this.maxDepth)
        {
            return true;
        }

        this.depth--;
        if (!this.maxDepthReported)
        {
            this.maxDepthReported = true;
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MaxDepthExceeded,
                    $"Rule exceeds the maximum tree depth of {this.maxDepth}.",
                    this.Current.Span,
                    expected: $"nesting at most {this.maxDepth} deep",
                    found: "deeper nesting"
                )
            );
            this.diagnosticsCeiling = this.diagnostics.Count;
        }

        return false;
    }

    private (RuleNode Node, string? BareInfix) ParseAndExpression()
    {
        return this.ParseAndOrChain("AND", this.ParseInfixChain, (operands, span) => new AndNode(operands, span));
    }

    /// <summary>
    /// Parses one <c>AND</c> or <c>OR</c> level. Any operand that is a bare (unparenthesized) infix
    /// expression such as <c>a XOR b</c> is ambiguous next to <c>AND</c>/<c>OR</c> (ADR-0005 decision 8), so
    /// it is reported at its own span. A level with a single operand is passed through so an enclosing
    /// level can still see that the expression is a bare infix one.
    /// </summary>
    private (RuleNode Node, string? BareInfix) ParseAndOrChain(
        string keyword,
        Func<(RuleNode Node, string? BareInfix)> parseOperand,
        Func<List<RuleNode>, SourceSpan, RuleNode> construct
    )
    {
        (RuleNode node, string? bareInfix) = parseOperand();
        List<RuleNode> operands = [node];
        List<(RuleNode Operand, string Operator)> bareOperands = [];
        if (bareInfix is not null)
        {
            bareOperands.Add((node, bareInfix));
        }

        while (this.TryConsumeKeyword(keyword))
        {
            (RuleNode next, string? nextBare) = parseOperand();
            operands.Add(next);
            if (nextBare is not null)
            {
                bareOperands.Add((next, nextBare));
            }
        }

        if (operands.Count == 1)
        {
            return (operands[0], bareInfix);
        }

        foreach ((RuleNode operand, string infixOperator) in bareOperands)
        {
            string shown = DisplayName(infixOperator);
            string message =
                $"Mixing {shown} with AND/OR at the same level requires explicit parentheses. "
                + $"Add parentheses around the {shown} expression to say which operator applies first.";
            this.ReportAmbiguousMixing(
                operand.Span,
                message,
                $"{shown} next to {keyword} without parentheses",
                this.WrapHint(shown, operand.Span)
            );
        }

        return (construct(operands, SpanCovering(node.Span.Start, operands[^1].Span.End)), null);
    }

    /// <summary>
    /// Parses a chain of one infix operator other than <c>AND</c>/<c>OR</c> (<c>a XOR b</c>). Operands
    /// are <c>NOT</c>-level expressions. A second, different infix operator in the same chain is ambiguous
    /// and is reported at that operator's own token (ADR-0005 decision 8).
    /// </summary>
    /// <returns>The chain's node and, when it is a real chain, the operator that built it.</returns>
    private (RuleNode Node, string? BareInfix) ParseInfixChain()
    {
        RuleNode node = this.ParseNotExpression();
        if (this.CurrentInfixOperator() is not { } chainOperator)
        {
            return (node, null);
        }

        List<RuleNode> operands = [node];
        int start = node.Span.Start;
        while (this.CurrentInfixOperator() is { } current)
        {
            if (current != chainOperator)
            {
                string message =
                    $"Mixing {DisplayName(chainOperator)} with {DisplayName(current)} at the same level requires explicit parentheses. "
                    + "Add parentheses around the operands that should be grouped first.";
                this.ReportAmbiguousMixing(
                    this.Current.Span,
                    message,
                    $"{DisplayName(chainOperator)} next to {DisplayName(current)} without parentheses",
                    "Add parentheses around the operands that should be grouped first."
                );
            }

            this.position++;
            operands.Add(this.ParseNotExpression());
        }

        SourceSpan span = SpanCovering(start, operands[^1].Span.End);
        RuleNode result = chainOperator switch
        {
            "XOR" => new XorNode(operands, span),
            "EQUIVALENT" => new EquivalentNode(operands, span),
            "IMPLIES" => new ImpliesNode(operands, span),
            "NAND" => new NandNode(operands, span),
            "NOR" => new NorNode(operands, span),
            "COALESCE" => new CoalesceNode(operands, span),
            _ => throw new InvalidOperationException($"Unhandled infix operator '{chainOperator}'."),
        };
        return (result, chainOperator);
    }

    /// <summary>Gets the canonical name of the infix operator (other than AND/OR) at the cursor, or <see langword="null"/>.</summary>
    private string? CurrentInfixOperator()
    {
        // The word COALESCE is a function call, so only its ?? symbol token counts as an infix operator.
        return InfixOperators.FirstOrDefault(op =>
            this.IsKeyword(op) && (op != "COALESCE" || this.Current.Kind == TokenKind.Operator)
        );
    }

    private RuleNode ParseNotExpression()
    {
        if (this.TryConsumeKeyword("NOT"))
        {
            int start = this.tokens[this.position - 1].Span.Start;

            // A NOT chain recurses here directly, without going back through ParseOrExpression, so it needs its
            // own guard: "NOT " repeated enough times is otherwise an unbounded self-recursion.
            if (!this.TryEnterNesting())
            {
                return new ErrorNode(SpanCovering(start, this.Current.Span.End));
            }

            try
            {
                RuleNode operand = this.ParseNotExpression();
                return new NotNode(operand, SpanCovering(start, operand.Span.End));
            }
            finally
            {
                this.depth--;
            }
        }

        return this.ParsePrimary();
    }

    private ConstantNode ConsumeConstant(TruthValue value)
    {
        SourceSpan span = this.Current.Span;
        this.position++;
        return new ConstantNode(value, span);
    }

    private RuleNode ParsePrimary()
    {
        if (IsGroupOpener(this.Current.Kind))
        {
            // '(', '[' and '{' are interchangeable; the node keeps no trace of which was written (ADR-0005 decision 9).
            Token opener = this.Current;
            this.position++;
            this.openGroups++;
            RuleNode inner = this.ParseExpression();
            this.ExpectClose(opener);

            // An outer group is recorded after an inner one around the same node, so the widest extent wins.
            this.groups[inner] = (opener.Span.Start, this.tokens[this.position - 1].Span.End);
            return inner;
        }

        // Keyword matching is case-insensitive (IsKeyword), so True/TRUE/true all yield the same node.
        if (this.IsKeyword("TRUE"))
        {
            return this.ConsumeConstant(TruthValue.True);
        }

        if (this.IsKeyword("FALSE"))
        {
            return this.ConsumeConstant(TruthValue.False);
        }

        if (this.IsKeyword("UNKNOWN"))
        {
            return this.ConsumeConstant(TruthValue.Unknown);
        }

        // T, F and U are the short spellings of the constants (ConstantStyle.Letters). A letter followed by '(' is a call
        // to a predicate with a reserved name, so it stays a term and the compiler reports the reserved name.
        if (this.Current.Kind == TokenKind.Identifier && this.tokens[this.position + 1].Kind != TokenKind.LParen)
        {
            if (this.IsKeyword("T"))
            {
                return this.ConsumeConstant(TruthValue.True);
            }

            if (this.IsKeyword("F"))
            {
                return this.ConsumeConstant(TruthValue.False);
            }

            if (this.IsKeyword("U"))
            {
                return this.ConsumeConstant(TruthValue.Unknown);
            }
        }

        if (this.IsKeyword("EXACTLYONE"))
        {
            return this.ParseExactlyOne();
        }

        if (this.IsKeyword("PARITY"))
        {
            return this.ParseOperandCall((operands, span) => new ParityNode(operands, span));
        }

        if (this.IsKeyword("NXOR"))
        {
            return this.RejectNxor();
        }

        if (this.IsKeyword("ANY"))
        {
            return this.ParseOperandCall((operands, span) => new AnyNode(operands, span));
        }

        if (this.IsKeyword("ALL"))
        {
            return this.ParseOperandCall((operands, span) => new AllNode(operands, span));
        }

        if (this.IsKeyword("NONE"))
        {
            return this.ParseOperandCall((operands, span) => new NoneNode(operands, span));
        }

        if (this.IsKeyword("FORALL"))
        {
            return this.ParseQuantifier(QuantifierKind.ForAll);
        }

        if (this.IsKeyword("EXISTS"))
        {
            return this.ParseQuantifier(QuantifierKind.Exists);
        }

        if (this.Current.Kind == TokenKind.Identifier && this.IsKeyword("COALESCE"))
        {
            return this.ParseOperandCall((operands, span) => new CoalesceNode(operands, span));
        }

        foreach ((string keyword, InspectionKind kind) in InspectionKeywords)
        {
            if (this.IsKeyword(keyword))
            {
                return this.ParseOperandCall((operands, span) => new InspectionNode(kind, operands, span));
            }
        }

        if (this.IsKeyword("PROJECT"))
        {
            return this.RejectProject();
        }

        if (this.IsKeyword("COLLAPSE"))
        {
            return this.RejectCollapse();
        }

        if (this.IsKeyword("IF"))
        {
            return this.ParseOperandCall((operands, span) => new IfNode(operands, span));
        }

        if (this.IsKeyword("BETWEEN"))
        {
            return this.ParseBetween();
        }

        if (this.IsKeyword("ATLEAST"))
        {
            return this.ParseThreshold(ThresholdComparison.AtLeast);
        }

        if (this.IsKeyword("ATMOST"))
        {
            return this.ParseThreshold(ThresholdComparison.AtMost);
        }

        if (this.IsKeyword("GREATERTHAN"))
        {
            return this.ParseThreshold(ThresholdComparison.GreaterThan);
        }

        if (this.IsKeyword("LESSTHAN"))
        {
            return this.ParseThreshold(ThresholdComparison.LessThan);
        }

        if (this.IsKeyword("EXACTLY"))
        {
            return this.ParseThreshold(ThresholdComparison.Exactly);
        }

        // A bare identifier matching the current definition's own parameter (ADR-0008 decision 5) stands for whatever
        // sub-expression the use that is being expanded supplied; followed by '(' it is instead an ordinary term/use
        // name, matching the T/F/U short-constant precedent just above.
        if (
            this.Current.Kind == TokenKind.Identifier
            && this.currentParameters is { } parameters
            && parameters.Contains(this.Current.Text)
            && this.tokens[this.position + 1].Kind != TokenKind.LParen
        )
        {
            Token parameterToken = this.Current;
            this.position++;
            return new ParameterReferenceNode(parameterToken.Text, parameterToken.Span);
        }

        if (this.Current.Kind == TokenKind.Identifier && this.definitionNames.Contains(this.Current.Text))
        {
            return this.ParseUse();
        }

        if (this.Current.Kind == TokenKind.Identifier && IsInfixWord(this.Current.Text))
        {
            return this.RejectInfixWordAsOperand();
        }

        if (this.Current.Kind == TokenKind.Identifier)
        {
            return this.ParseTerm();
        }

        this.ReportExpectedOperand();
        SourceSpan errorSpan = this.Current.Span;

        // Skip the bad token so parsing moves on, unless it is the end of input or a closer that an enclosing group is
        // waiting for: that group's ExpectClose consumes it, so '(a AND )' does not also report the '(' as unclosed.
        if (this.Current.Kind != TokenKind.Eof && !(IsGroupCloser(this.Current.Kind) && this.openGroups > 0))
        {
            this.position++;
        }

        return new ErrorNode(errorSpan);
    }

    /// <summary>Reports that the token at the cursor stands where an operand (a term, constant or group) must be.</summary>
    private void ReportExpectedOperand()
    {
        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.SyntaxError,
                $"Expected a term, constant, or '(' but found '{this.Current.Text}'.",
                this.Current.Span,
                expected: "a term, constant or '('",
                found: DescribeFound(this.Current)
            )
        );
    }

    /// <summary>
    /// Reports an infix operator word that stands where an operand must be, as in <c>a AND OR</c> or <c>XOR(a, b)</c>,
    /// as a syntax error at the word. Without this the word parses as a term name and the compiler reports a reserved
    /// predicate name, which points the author at the wrong mistake. Recovery depends on what follows, so that one
    /// mistake gives one diagnostic:
    /// <list type="bullet">
    /// <item><description>A call form (<c>XOR(...)</c>, which the language does not have): the word and its whole
    /// parenthesized list are skipped.</description></item>
    /// <item><description>A token that can start an operand (<c>a AND OR b</c>): the word stays in place. The operator
    /// chain above this operand always accepts an infix word, so it reads the word as the operator after a missing
    /// operand.</description></item>
    /// <item><description>Anything else (the end of the rule, a closer, a comma): the word is skipped.</description></item>
    /// </list>
    /// </summary>
    private ErrorNode RejectInfixWordAsOperand()
    {
        this.ReportExpectedOperand();
        Token word = this.Current;
        Token next = this.tokens[this.position + 1];
        if (next.Kind == TokenKind.LParen)
        {
            this.position++;
            this.SkipBalancedGroup();
            return new ErrorNode(SpanCovering(word.Span.Start, this.tokens[this.position - 1].Span.End));
        }

        if (!CanStartOperand(next))
        {
            this.position++;
        }

        return new ErrorNode(word.Span);
    }

    /// <summary>
    /// Skips the group that opens at the cursor through its matching closer, counting every delimiter kind for nesting.
    /// At the end of input it stops, so an unbalanced group does not add a second diagnostic to the one that caused the
    /// skip.
    /// </summary>
    private void SkipBalancedGroup()
    {
        int nesting = 0;
        do
        {
            if (IsGroupOpener(this.Current.Kind))
            {
                nesting++;
            }
            else if (IsGroupCloser(this.Current.Kind))
            {
                nesting--;
            }

            this.position++;
        } while (nesting > 0 && this.Current.Kind != TokenKind.Eof);
    }

    /// <summary>
    /// Skips the rest of a malformed argument up to the next ',', closer or ';' at the argument's own nesting level, or
    /// the end of input. That token is not consumed, so the enclosing argument list resumes at a point it understands.
    /// </summary>
    private void SkipToArgumentEnd()
    {
        int nesting = 0;
        while (this.Current.Kind != TokenKind.Eof)
        {
            TokenKind kind = this.Current.Kind;
            if (nesting == 0 && (kind is TokenKind.Comma or TokenKind.Semicolon || IsGroupCloser(kind)))
            {
                return;
            }

            if (IsGroupOpener(kind))
            {
                nesting++;
            }
            else if (IsGroupCloser(kind))
            {
                nesting--;
            }

            this.position++;
        }
    }

    private RuleNode ParseTerm()
    {
        Token nameToken = this.Current;
        this.position++;
        List<ArgumentNode> arguments = [];
        int end = nameToken.Span.End;
        if (this.Current.Kind == TokenKind.LParen)
        {
            Token opener = this.Current;
            this.position++;
            this.openGroups++;
            if (this.Current.Kind != TokenKind.RParen)
            {
                arguments.Add(this.ParseArgument());
                while (this.Current.Kind == TokenKind.Comma)
                {
                    this.position++;
                    arguments.Add(this.ParseArgument());
                }
            }

            end = this.Current.Span.End;
            this.ExpectClose(opener);
        }

        return new TermNode(nameToken.Text, arguments, SpanCovering(nameToken.Span.Start, end));
    }

    /// <summary>
    /// Parses one <c>let name = expression;</c> or <c>let name(p1, p2, ...) = expression;</c> declaration (ADR-0008
    /// decision 2). Name uniqueness, reserved-word and predicate-name collisions, argument counts, unknown definitions
    /// and recursion are checked later by <c>DefinitionExpander</c>, which has the predicate registry this parser does
    /// not; this method only builds the raw declaration.
    /// </summary>
    private DefinitionNode ParseDefinition()
    {
        int letStart = this.Current.Span.Start;
        this.position++; // 'let'
        Token nameToken = this.Current;
        this.Expect(TokenKind.Identifier, "a definition name");
        List<string> parameters = [];
        if (this.Current.Kind == TokenKind.LParen)
        {
            Token opener = this.Current;
            this.position++;
            this.openGroups++;
            if (this.Current.Kind != TokenKind.RParen)
            {
                parameters.Add(this.ExpectParameterName());
                while (this.Current.Kind == TokenKind.Comma)
                {
                    this.position++;
                    parameters.Add(this.ExpectParameterName());
                }
            }

            this.ExpectClose(opener);
        }

        this.Expect(TokenKind.Equals, "'='");

        // A definition's own body is the only place its parameters are in scope (ADR-0008 decision 5: definitions do
        // not nest, so there is never more than one set of parameters active at once).
        IReadOnlySet<string>? enclosingParameters = this.currentParameters;
        this.currentParameters = new HashSet<string>(parameters, StringComparer.OrdinalIgnoreCase);
        RuleNode body = this.ParseExpression();
        this.currentParameters = enclosingParameters;

        int declarationEnd = this.Current.Span.End;
        this.Expect(TokenKind.Semicolon, "';'");
        return new DefinitionNode(nameToken.Text, parameters, body, SpanCovering(letStart, declarationEnd), nameToken.Span);
    }

    private string ExpectParameterName()
    {
        Token token = this.Current;
        this.Expect(TokenKind.Identifier, "a parameter name");
        return token.Text;
    }

    /// <summary>
    /// Parses a use of a declared definition (ADR-0008 decision 2): the name, optionally applied, call-like, to
    /// positional arguments (<c>atLeastAge(21)</c>), each itself a full expression, unlike a <see cref="TermNode"/>'s
    /// named arguments, a use's arguments are parsed the same way an <c>AND</c>/<c>OR</c> operand is.
    /// </summary>
    private RuleNode ParseUse()
    {
        Token nameToken = this.Current;
        this.position++;
        List<RuleNode> arguments = [];
        int end = nameToken.Span.End;
        if (this.Current.Kind == TokenKind.LParen)
        {
            Token opener = this.Current;
            this.position++;
            this.openGroups++;
            if (this.Current.Kind != TokenKind.RParen)
            {
                arguments.Add(this.ParseExpression());
                while (this.Current.Kind == TokenKind.Comma)
                {
                    this.position++;
                    arguments.Add(this.ParseExpression());
                }
            }

            end = this.Current.Span.End;
            this.ExpectClose(opener);
        }

        return new UseNode(nameToken.Text, arguments, SpanCovering(nameToken.Span.Start, end), nameToken.Span);
    }

    private ArgumentNode ParseArgument()
    {
        Token nameToken = this.Current;
        this.Expect(TokenKind.Identifier, "an argument name");
        this.Expect(TokenKind.Colon, "':'");
        RawLiteral value = this.IsVariableReferenceStart() ? this.ParseVariableReference() : this.ParseLiteral();
        return new ArgumentNode(nameToken.Text, value, SpanCovering(nameToken.Span.Start, value.Span.End));
    }

    /// <summary>
    /// Whether the current token starts a variable reference: the reserved word <c>from</c> directly followed by
    /// <c>(</c> (ADR-0006 decision 10). A bare <c>from</c> is left to the literal parser to reject.
    /// </summary>
    private bool IsVariableReferenceStart()
    {
        return this.Current.Kind == TokenKind.Identifier
            && string.Equals(this.Current.Text, "from", StringComparison.OrdinalIgnoreCase)
            && this.position + 1 < this.tokens.Count
            && this.tokens[this.position + 1].Kind == TokenKind.LParen;
    }

    /// <summary>
    /// Parses <c>from("source", "query")</c>. It is accepted only as a whole argument value, never inside an array
    /// literal: the argument's value is then a variable the engine resolves at evaluation time.
    /// </summary>
    private RawLiteral ParseVariableReference()
    {
        Token fromToken = this.Current;
        this.position++;
        Token opener = this.ExpectOpenParen();
        SourceSpan sourceSpan = this.Current.Span;
        string sourceName = this.ExpectQuotedText("a quoted source name");
        this.Expect(TokenKind.Comma, "','");
        SourceSpan querySpan = this.Current.Span;
        string query = this.ExpectQuotedText("a quoted query");
        int end = this.Current.Span.End;
        this.ExpectClose(opener);
        return RawLiteral.OfVariable(
            sourceName,
            query,
            SpanCovering(fromToken.Span.Start, end),
            new VariableParts(sourceSpan, querySpan)
        );
    }

    /// <summary>Consumes a quoted string and returns its text, or reports <paramref name="description"/> and returns an empty string.</summary>
    private string ExpectQuotedText(string description)
    {
        if (this.Current.Kind != TokenKind.StringLiteral)
        {
            this.Expect(TokenKind.StringLiteral, description);
            return string.Empty;
        }

        string text = this.Current.Text;
        this.position++;
        return text;
    }

    private RawLiteral ParseLiteral()
    {
        Token current = this.Current;
        switch (current.Kind)
        {
            case TokenKind.StringLiteral:
                this.position++;
                return RawLiteral.OfString(current.Text, current.Span);
            case TokenKind.NumberLiteral:
                this.position++;
                return RawLiteral.OfNumber(current.Text, current.Span);
            case TokenKind.Identifier when string.Equals(current.Text, "true", StringComparison.OrdinalIgnoreCase):
                this.position++;
                return RawLiteral.OfBoolean(true, current.Span);
            case TokenKind.Identifier when string.Equals(current.Text, "false", StringComparison.OrdinalIgnoreCase):
                this.position++;
                return RawLiteral.OfBoolean(false, current.Span);
            case TokenKind.LBracket:
                return this.ParseArrayLiteral();
            default:
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.SyntaxError,
                        $"Expected a literal value but found '{current.Text}'.",
                        current.Span,
                        expected: "a literal value",
                        found: DescribeFound(current)
                    )
                );
                if (current.Kind != TokenKind.Eof)
                {
                    this.position++;
                }

                return RawLiteral.OfBoolean(false, current.Span);
        }
    }

    private RawLiteral ParseArrayLiteral()
    {
        int start = this.Current.Span.Start;
        this.position++;
        List<RawLiteral> elements = [];
        if (this.Current.Kind != TokenKind.RBracket)
        {
            elements.Add(this.ParseLiteral());
            while (this.Current.Kind == TokenKind.Comma)
            {
                this.position++;
                elements.Add(this.ParseLiteral());
            }
        }

        int end = this.Current.Span.End;
        this.Expect(TokenKind.RBracket, "']'");
        return RawLiteral.OfArray(elements, SpanCovering(start, end));
    }

    /// <summary>
    /// Parses <c>FORALL(variable IN collection, body)</c> or <c>EXISTS(...)</c> (ADR-0010 decision 2): the bound
    /// variable and the collection name are plain identifiers read from the quantifier's head. <c>IN</c> is a
    /// contextual keyword here only. It is not in <see cref="ReservedWords"/>, so a predicate elsewhere named
    /// <c>In</c> is unaffected.
    /// </summary>
    private RuleNode ParseQuantifier(QuantifierKind kind)
    {
        int start = this.Current.Span.Start;
        this.position++; // 'FORALL' or 'EXISTS'
        Token opener = this.ExpectOpenParen();

        string variable = this.ExpectIdentifierName("a bound variable name");
        this.ExpectIn();
        string collection = this.ExpectIdentifierName("a collection name");
        this.Expect(TokenKind.Comma, "','");
        RuleNode body = this.ParseExpression();

        int end = this.Current.Span.End;
        this.ExpectClose(opener);
        return new QuantifierNode(kind, variable, collection, body, SpanCovering(start, end));
    }

    /// <summary>Consumes an identifier token and returns its text, or reports <paramref name="description"/> and returns an empty string.</summary>
    private string ExpectIdentifierName(string description)
    {
        Token token = this.Current;
        this.Expect(TokenKind.Identifier, description);
        return token.Kind == TokenKind.Identifier ? token.Text : string.Empty;
    }

    /// <summary>Consumes the contextual <c>IN</c> keyword of a quantifier head, or reports what is wrong.</summary>
    private void ExpectIn()
    {
        if (
            this.Current.Kind == TokenKind.Identifier
            && string.Equals(this.Current.Text, "IN", StringComparison.OrdinalIgnoreCase)
        )
        {
            this.position++;
            return;
        }

        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.SyntaxError,
                $"Expected 'IN' but found '{this.Current.Text}'.",
                this.Current.Span,
                expected: "'IN'",
                found: DescribeFound(this.Current)
            )
        );
    }

    private RuleNode ParseExactlyOne()
    {
        int start = this.Current.Span.Start;
        this.position++;
        List<RuleNode> operands = this.ParseParenthesizedOperandList();
        return new ExactlyOneNode(operands, SpanCovering(start, this.tokens[this.position - 1].Span.End));
    }

    /// <summary>
    /// Parses a keyword followed by a parenthesized, comma-separated operand list (<c>PARITY</c>, <c>ANY</c>, <c>ALL</c>,
    /// <c>NONE</c>) and wraps it with <paramref name="create"/>. The operand count is checked later by the compiler.
    /// </summary>
    private RuleNode ParseOperandCall(Func<IReadOnlyList<RuleNode>, SourceSpan, RuleNode> create)
    {
        int start = this.Current.Span.Start;
        this.position++;
        List<RuleNode> operands = this.ParseParenthesizedOperandList();
        return create(operands, SpanCovering(start, this.tokens[this.position - 1].Span.End));
    }

    private RuleNode ParseThreshold(ThresholdComparison comparison)
    {
        int start = this.Current.Span.Start;
        this.position++;
        Token opener = this.ExpectOpenParen();
        int k = this.ParseIntegerBound($"threshold as {comparison}'s first argument");

        List<RuleNode> operands = [];
        while (this.Current.Kind == TokenKind.Comma)
        {
            this.position++;
            operands.Add(this.ParseExpression());
        }

        int end = this.Current.Span.End;
        this.ExpectClose(opener);
        return new ThresholdNode(comparison, k, operands, SpanCovering(start, end));
    }

    /// <summary>
    /// Parses <c>BETWEEN(min, max, operand, ...)</c>: two integer bounds followed by the operands. The bounds' range and
    /// the operand count are validated by the compiler, like the threshold family's <c>k</c>.
    /// </summary>
    private RuleNode ParseBetween()
    {
        int start = this.Current.Span.Start;
        this.position++;
        Token opener = this.ExpectOpenParen();
        int min = this.ParseIntegerBound("minimum as BETWEEN's first argument");
        this.Expect(TokenKind.Comma, "','");
        int max = this.ParseIntegerBound("maximum as BETWEEN's second argument");

        List<RuleNode> operands = [];
        while (this.Current.Kind == TokenKind.Comma)
        {
            this.position++;
            operands.Add(this.ParseExpression());
        }

        int end = this.Current.Span.End;
        this.ExpectClose(opener);
        return new BetweenNode(min, max, operands, SpanCovering(start, end));
    }

    /// <summary>
    /// Rejects a <c>Collapse(...)</c> call. The call is still parsed as an operand list so the closing parenthesis is found and
    /// the rest of the text keeps being checked, but it yields an <see cref="ErrorNode"/> and one diagnostic that points the
    /// author at <c>Decision.Collapse</c> (ADR-0005 decision 14).
    /// </summary>
    private RuleNode RejectCollapse()
    {
        return this.ParseOperandCall(
            (_, span) =>
            {
                this.diagnostics.Add(CollapseRejection.Create(DiagnosticCodes.UnknownPredicate, span));
                return new ErrorNode(span);
            }
        );
    }

    /// <summary>
    /// Rejects a <c>NXOR(...)</c> call, the retired spelling of <c>PARITY</c>: the call is parsed as an operand list so the
    /// rest of the text keeps being checked, then yields an <see cref="ErrorNode"/> and one diagnostic that suggests
    /// <c>PARITY</c> (ADR-0005 decision 4).
    /// </summary>
    private RuleNode RejectNxor()
    {
        return this.ParseOperandCall(
            (_, span) =>
            {
                this.diagnostics.Add(NxorRejection.Create(DiagnosticCodes.UnknownPredicate, span, "PARITY"));
                return new ErrorNode(span);
            }
        );
    }

    /// <summary>
    /// Rejects a <c>Project(...)</c> call the same way <see cref="RejectCollapse"/> rejects <c>Collapse</c>: the call is parsed
    /// as an operand list so the rest of the text keeps being checked, then yields an <see cref="ErrorNode"/> and one
    /// diagnostic that points the author at <c>COALESCE</c> and <c>Decision.Project</c> (ADR-0005 decision 12).
    /// </summary>
    private RuleNode RejectProject()
    {
        return this.ParseOperandCall(
            (_, span) =>
            {
                this.diagnostics.Add(ProjectRejection.Create(DiagnosticCodes.UnknownPredicate, span));
                return new ErrorNode(span);
            }
        );
    }

    /// <summary>
    /// Reads one integer literal for a threshold <c>k</c> or a BETWEEN bound. The text is parsed with the invariant
    /// culture, so the result does not depend on the host. A missing, fractional or out-of-range (for example
    /// <c>1.5</c> or <c>99999999999</c>) value is an <see cref="DiagnosticCodes.InvalidThresholdValue"/> error, as in the tree formats; the rest of the argument is skipped up to
    /// the next ',' or closer so parsing can continue, and <c>0</c> is returned only as a placeholder next to that error.
    /// </summary>
    /// <param name="description">What is expected, as "name as OPERATOR's Nth argument", for the diagnostic.</param>
    private int ParseIntegerBound(string description)
    {
        if (
            this.Current.Kind == TokenKind.NumberLiteral
            && int.TryParse(this.Current.Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value)
        )
        {
            this.position++;
            return value;
        }

        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.InvalidThresholdValue,
                $"Expected an integer {description}.",
                this.Current.Span,
                expected: "an integer",
                found: DescribeFound(this.Current)
            )
        );

        // Skip the whole bad argument, not only a number token, so a forgotten bound (AtLeast(a, b, c)) is not read
        // again as a misplaced operand by the closer check and the end-of-rule check.
        this.SkipToArgumentEnd();
        return 0;
    }

    private List<RuleNode> ParseParenthesizedOperandList()
    {
        Token opener = this.ExpectOpenParen();
        List<RuleNode> operands = [];
        if (this.Current.Kind != TokenKind.RParen)
        {
            operands.Add(this.ParseExpression());
            while (this.Current.Kind == TokenKind.Comma)
            {
                this.position++;
                operands.Add(this.ParseExpression());
            }
        }

        this.ExpectClose(opener);
        return operands;
    }

    /// <summary>
    /// Consumes the '(' that opens a function call's argument list and returns it, so the matching closer can be
    /// checked against it. Only '(' opens a call: '[' and '{' are for grouping a sub-expression (ADR-0005 decision 9), so
    /// <c>ANY[a, b]</c> stays an error. When the '(' is missing it is reported and a stand-in at the cursor is returned.
    /// </summary>
    private Token ExpectOpenParen()
    {
        Token opener = this.Current;
        this.openGroups++;
        if (opener.Kind == TokenKind.LParen)
        {
            this.position++;
            return opener;
        }

        this.Expect(TokenKind.LParen, "'('");
        return new Token(TokenKind.LParen, "(", opener.Span);
    }

    private void Expect(TokenKind kind, string description)
    {
        if (this.Current.Kind == kind)
        {
            this.position++;
            return;
        }

        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.SyntaxError,
                $"Expected {description} but found '{this.Current.Text}'.",
                this.Current.Span,
                expected: description,
                found: DescribeFound(this.Current)
            )
        );
    }

    /// <summary>
    /// Consumes the delimiter that closes <paramref name="opener"/>, or reports exactly what is wrong: a closer of another
    /// kind is reported at that closer (and consumed, as if it were the intended one, to limit follow-on errors); the
    /// end of input is reported at the opener, which is the token that was never closed; any other token is reported at
    /// itself.
    /// </summary>
    private void ExpectClose(Token opener)
    {
        this.openGroups--;
        char expected = ClosingFor(opener.Kind);
        if (this.Current.Kind == ExpectedCloserKind(opener.Kind))
        {
            this.position++;
            return;
        }

        if (this.Current.Kind == TokenKind.Eof)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.SyntaxError,
                    $"Unclosed '{opener.Text}' at offset {opener.Span.Start}: expected '{expected}' before the end of the rule.",
                    opener.Span,
                    expected: $"'{expected}'",
                    found: "end of rule"
                )
            );
            return;
        }

        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.SyntaxError,
                $"Expected '{expected}' to close '{opener.Text}' at offset {opener.Span.Start} but found '{this.Current.Text}'.",
                this.Current.Span,
                expected: $"'{expected}'",
                found: DescribeFound(this.Current),
                suggestion: SuggestInfixWord(this.Current)
            )
        );
        if (IsGroupCloser(this.Current.Kind))
        {
            this.position++;
        }
    }

    private void ReportAmbiguousMixing(SourceSpan span, string message, string found, string hint)
    {
        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.AmbiguousOperatorMixing,
                message,
                span,
                expected: "parentheses around one of the groups",
                found: found,
                suggestion: new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, hint)
            )
        );
    }

    /// <summary>Gets the source text a span covers, clamped to the source.</summary>
    private string TextOf(SourceSpan span)
    {
        int start = Math.Clamp(span.Start, 0, this.source.Length);
        return this.source[start..Math.Clamp(span.End, start, this.source.Length)];
    }

    /// <summary>Builds the parentheses hint for a bare operand, quoting its source text already wrapped so it can be pasted back.</summary>
    private string WrapHint(string operatorName, SourceSpan operand)
    {
        return $"Wrap the {operatorName} expression in parentheses: ({this.TextOf(operand)})";
    }
}
