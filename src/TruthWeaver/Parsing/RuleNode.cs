namespace TruthWeaver.Parsing;

using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;

// SA1402 relaxed: a single closed-set discriminated union describing raw (pre-validation) parse
// output, shared identically by the DSL, JSON, and YAML front ends so that
// "parse(print(x)) is structurally equal to x" and "JSON/YAML parse to the same AST as DSL" are true
// by construction rather than by parallel maintenance of three grammars.
#pragma warning disable SA1402

/// <summary>
/// A node in the raw parse tree produced by any of the three rule-text front ends, before predicate
/// names are resolved and argument literals are validated against a schema. <c>RuleNodeCompiler</c>
/// (in the <c>Compilation</c> namespace) is the single place that turns a <see cref="RuleNode"/> tree
/// into a validated <c>Ast.Expression</c> tree. Every front end funnels through it, which is what
/// makes DSL/JSON/YAML round-tripping and cross-format equality a property of the design rather than
/// something tested for and hoped to remain true.
/// </summary>
/// <param name="Span">This node's location in the source it was parsed from.</param>
internal abstract record RuleNode(SourceSpan Span)
{
    /// <summary>
    /// Gets where this node sits in the JSON or YAML document it came from, such as <c>$.operands[1]</c>, or
    /// <see langword="null"/> for DSL text (which is located by <see cref="Span"/>). The tree front ends set it, and the
    /// compiler copies it onto the diagnostics it raises for the node.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets or sets the note written on this node, in normal form (<c>NoteText.Normalize</c>), or <see langword="null"/>
    /// for none. A tree front end sets it from the node's <c>note</c> key; the DSL parser sets it after parsing, when it
    /// attaches each comment to a node, which is why it is settable. The compiler copies it beside the built tree, never
    /// into it, so a note never changes the compiled expression.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Gets where this node is written in a library rule, or <see langword="null"/> for a node of the compiled text.
    /// <c>DefinitionExpander</c> sets it on each node a <c>rule("name")</c> reference expands to, whose
    /// <see cref="Span"/> is the reference; the compiler adds it to the diagnostics it raises for the node, and the
    /// evaluator to the trace entry of the node.
    /// </summary>
    public LibraryLocator? Library { get; init; }

    /// <summary>
    /// Gets the operand nodes of <paramref name="node"/> in order, the same order the compiled expression keeps them in;
    /// empty for a constant, a term and an error placeholder.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>The operands.</returns>
    public static IReadOnlyList<RuleNode> OperandsOf(RuleNode node)
    {
        return node switch
        {
            NotNode not => [not.Operand],
            AndNode and => and.Operands,
            OrNode or => or.Operands,
            XorNode xor => xor.Operands,
            EquivalentNode equivalent => equivalent.Operands,
            ImpliesNode implies => implies.Operands,
            NandNode nand => nand.Operands,
            NorNode nor => nor.Operands,
            ParityNode parity => parity.Operands,
            AnyNode any => any.Operands,
            AllNode all => all.Operands,
            NoneNode none => none.Operands,
            ExactlyOneNode exactlyOne => exactlyOne.Operands,
            ThresholdNode threshold => threshold.Operands,
            BetweenNode between => between.Operands,
            InspectionNode inspection => inspection.Operands,
            IfNode conditional => conditional.Operands,
            CoalesceNode coalesce => coalesce.Operands,
            UseNode use => use.Arguments,
            DefinitionNode definition => [definition.Body],
            RuleDocumentNode document => [.. document.Definitions, document.Body],
            QuantifierNode quantifier => [quantifier.Body],
            _ => [],
        };
    }
}

/// <summary>The literal K3 constant: <c>True</c>, <c>False</c> or <c>Unknown</c>.</summary>
internal sealed record ConstantNode(TruthValue Value, SourceSpan Span) : RuleNode(Span);

/// <summary>A raw predicate reference with its (not yet validated) named arguments.</summary>
internal sealed record TermNode(string PredicateName, IReadOnlyList<ArgumentNode> Arguments, SourceSpan Span) : RuleNode(Span);

/// <summary>One raw named argument of a <see cref="TermNode"/>.</summary>
internal sealed record ArgumentNode(string Name, RawLiteral Value, SourceSpan Span)
{
    /// <summary>Gets the argument's JSON or YAML path, such as <c>$.args.role</c>, or <see langword="null"/> for DSL text.</summary>
    public string? Path { get; init; }
}

/// <summary>Logical negation.</summary>
internal sealed record NotNode(RuleNode Operand, SourceSpan Span) : RuleNode(Span);

/// <summary>Logical conjunction (raw operand count may be any size &gt;= 1; the compiler enforces &gt;= 2 was written).</summary>
internal sealed record AndNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>Logical disjunction.</summary>
internal sealed record OrNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A raw <c>XOR</c> reference. Operand count is not yet validated here. A DSL infix chain
/// (<c>a XOR b XOR c</c>) or an explicit JSON/YAML <c>op: xor</c> node with the wrong operand count
/// both flow through as an <see cref="XorNode"/>, and <c>RuleNodeCompiler</c> uniformly rejects
/// anything other than exactly two operands (ADR-0003).
/// </summary>
internal sealed record XorNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A raw <c>EQUIVALENT</c> reference (logical biconditional; <c>IFF</c> and the legacy <c>XNOR</c> are aliases that
/// parse to this same node). Same operand-count validation
/// story as <see cref="XorNode"/>: <c>RuleNodeCompiler</c> uniformly rejects anything other than
/// exactly two operands.
/// </summary>
internal sealed record EquivalentNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A raw <c>IMPLIES</c> reference (material implication). Operand count is validated by
/// <c>RuleNodeCompiler</c> (exactly two), like <see cref="XorNode"/>, so a DSL chain
/// (<c>a IMPLIES b IMPLIES c</c>) and a malformed JSON/YAML node are rejected the same way.
/// </summary>
internal sealed record ImpliesNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A raw <c>NAND</c> reference (negated conjunction). Operand count is validated by <c>RuleNodeCompiler</c>
/// (exactly two), like <see cref="XorNode"/>, so a DSL chain and a malformed JSON/YAML node are rejected the same way.
/// </summary>
internal sealed record NandNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>A raw <c>NOR</c> reference (negated disjunction); validated exactly like <see cref="NandNode"/>.</summary>
internal sealed record NorNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// N-ary parity reference (<c>PARITY(a, b, ...)</c>). Operand count (at least two) is validated by <c>RuleNodeCompiler</c>
/// like the other n-ary operators.
/// </summary>
internal sealed record ParityNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// N-ary <c>ANY(...)</c> reference (at least one operand is true). Operand count (at least two) is validated by
/// <c>RuleNodeCompiler</c> like the other n-ary operators.
/// </summary>
internal sealed record AnyNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>N-ary <c>ALL(...)</c> reference (every operand is true); validated like <see cref="AnyNode"/>.</summary>
internal sealed record AllNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>N-ary <c>NONE(...)</c> reference (no operand is true); validated like <see cref="AnyNode"/>.</summary>
internal sealed record NoneNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>N-ary "exactly one of these is true".</summary>
internal sealed record ExactlyOneNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// N-ary count-threshold reference (<c>AtLeast</c>/<c>AtMost</c>/<c>GreaterThan</c>/<c>LessThan</c>/
/// <c>Exactly</c>); <paramref name="K"/>'s valid range is validated by the compiler, not the parser.
/// </summary>
internal sealed record ThresholdNode(ThresholdComparison Comparison, int K, IReadOnlyList<RuleNode> Operands, SourceSpan Span)
    : RuleNode(Span);

/// <summary>
/// <c>BETWEEN(min, max, ...)</c> reference: the true-operand count lies in <c>[min, max]</c>. The bounds' valid range and
/// the operand count (at least two) are validated by the compiler, not the parser.
/// </summary>
internal sealed record BetweenNode(int Min, int Max, IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// <c>IsTrue(x)</c> / <c>IsFalse(x)</c> / <c>IsUnknown(x)</c> / <c>IsKnown(x)</c> reference. The operand count (exactly one)
/// is validated by <c>RuleNodeCompiler</c>, so <c>IsTrue(a, b)</c> and a malformed JSON/YAML node are rejected the same way.
/// </summary>
internal sealed record InspectionNode(InspectionKind Kind, IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// <c>If(condition, whenTrue, whenFalse)</c> / ternary <c>c ? t : f</c> reference. The operand count (exactly three) is
/// validated by <c>RuleNodeCompiler</c>, so a malformed JSON/YAML node or <c>If(a, b)</c> is rejected the same way.
/// </summary>
internal sealed record IfNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// N-ary <c>COALESCE(...)</c> / infix <c>??</c> reference (first operand that is not <c>Unknown</c>). Operand count
/// (at least two) is validated by <c>RuleNodeCompiler</c> like the other n-ary operators.
/// </summary>
internal sealed record CoalesceNode(IReadOnlyList<RuleNode> Operands, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A placeholder produced only after a syntax/structure error has already been reported, so parsing
/// can continue (and thus report further diagnostics in the same pass) without ever throwing.
/// </summary>
internal sealed record ErrorNode(SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A reference to a named rule of the compiler's <c>RuleLibrary</c>: <c>rule("name")</c>, or
/// <c>rule("name", hash: "...")</c> with a pin. <c>DefinitionExpander</c> replaces it with the library rule's own
/// expanded tree before <c>RuleNodeCompiler</c> builds the compiled tree, so nothing after expansion sees it.
/// </summary>
/// <param name="Name">The library rule name, as written.</param>
/// <param name="PinnedHash">The content hash the reference pins, or <see langword="null"/> for the current version.</param>
/// <param name="Span">The whole reference, from <c>rule</c> to its closing parenthesis.</param>
internal sealed record LibraryReferenceNode(string Name, string? PinnedHash, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A document-scoped <c>let</c> declaration (ADR-0008): names a reusable sub-expression, with zero or more declared
/// parameters, that a <see cref="UseNode"/> elsewhere in the same document can expand at compile time. Never reaches
/// the evaluator, analyzer, rewrites or printers. <c>DefinitionExpander</c> replaces every use before
/// <c>RuleNodeCompiler</c> builds the compiled <see cref="Ast.Expression"/> tree.
/// </summary>
/// <param name="Name">The definition's name, unique (case-insensitively) within its document.</param>
/// <param name="Parameters">The definition's declared parameter names, in positional order; empty for a parameterless definition.</param>
/// <param name="Body">The definition's own expression, in which a <see cref="ParameterReferenceNode"/> stands for each parameter.</param>
/// <param name="Span">The declaration's full span, from <c>let</c> to the closing <c>;</c>.</param>
/// <param name="NameSpan">The span of the definition's name token alone, the span a diagnostic about the declaration (as opposed to a use) points at.</param>
internal sealed record DefinitionNode(
    string Name,
    IReadOnlyList<string> Parameters,
    RuleNode Body,
    SourceSpan Span,
    SourceSpan NameSpan
) : RuleNode(Span)
{
    /// <summary>
    /// Gets where this definition's own name sits in the JSON or YAML document it came from (the definitions entry's
    /// path, e.g. <c>$.definitions.adult</c>), or <see langword="null"/> for a DSL definition, where <see cref="NameSpan"/>
    /// alone already locates it. <c>RuleCompiler</c> resolves a diagnostic located by this path to a real span the same
    /// way it resolves any other path-located diagnostic (ADR-0008 decision 8).
    /// </summary>
    public string? NamePath { get; init; }
}

/// <summary>
/// A reference, inside a <see cref="DefinitionNode"/>'s own body, to one of that definition's declared parameters
/// (ADR-0008 decision 5). <c>DefinitionExpander</c> replaces it with the argument tree supplied at the use site that
/// triggered this expansion; a parameter is never itself a node the evaluator or printers see.
/// </summary>
internal sealed record ParameterReferenceNode(string Name, SourceSpan Span) : RuleNode(Span);

/// <summary>
/// A use of a declared <see cref="DefinitionNode"/> (ADR-0008 decision 2): the name applied, call-like, to its
/// positional arguments (<c>atLeastAge(21)</c>), or written bare for a parameterless definition (<c>adult</c>).
/// <c>DefinitionExpander</c> replaces it, at compile time, with the definition's body with each parameter substituted
/// by the corresponding (already expanded) argument.
/// </summary>
/// <param name="Name">The definition's name as written at the use site.</param>
/// <param name="Arguments">The positional arguments, each itself a term, constant or sub-expression; empty for a parameterless use.</param>
/// <param name="Span">The use's full span, from the name to the closing <c>)</c> (or just the name for a parameterless use).</param>
/// <param name="NameSpan">The span of the name token alone, the span the definition-expansion diagnostics point at.</param>
internal sealed record UseNode(string Name, IReadOnlyList<RuleNode> Arguments, SourceSpan Span, SourceSpan NameSpan)
    : RuleNode(Span)
{
    /// <summary>
    /// Gets where this use's own name sits in the JSON or YAML document it came from (e.g. <c>$.operands[1].name</c>), or
    /// <see langword="null"/> for a DSL use, where <see cref="NameSpan"/> alone already locates it. <c>RuleCompiler</c>
    /// resolves a diagnostic located by this path to a real span the same way it resolves any other path-located
    /// diagnostic (ADR-0008 decision 8).
    /// </summary>
    public string? NamePath { get; init; }
}

/// <summary>
/// The root of a rule-text document that declared one or more <c>let</c> definitions (ADR-0008 decision 2).
/// <c>DefinitionExpander</c> expands it, before <c>RuleNodeCompiler</c> ever runs, into <see cref="Body"/> alone with
/// every <see cref="UseNode"/> replaced, so this node, like <see cref="DefinitionNode"/>, <see cref="UseNode"/> and
/// <see cref="ParameterReferenceNode"/>, never reaches the evaluator, analyzer, rewrites or printers.
/// </summary>
/// <param name="Definitions">The document's <c>let</c> declarations, in source order.</param>
/// <param name="Body">The rule's own expression, the document's single value.</param>
/// <param name="Span">The whole document's span.</param>
internal sealed record RuleDocumentNode(IReadOnlyList<DefinitionNode> Definitions, RuleNode Body, SourceSpan Span)
    : RuleNode(Span);

/// <summary>
/// A raw <c>FORALL</c>/<c>EXISTS</c> reference (ADR-0010): the bound variable, the collection name at the head, and
/// the body (the quantifier's one structural operand). <c>RuleNodeCompiler</c> resolves the collection name
/// against the registry's collection bindings and compiles the body against that binding's own element registry,
/// rather than against the registry it compiles the rest of the rule with.
/// </summary>
/// <param name="Kind">Whether this is <c>FORALL</c> or <c>EXISTS</c>.</param>
/// <param name="Variable">The bound variable name, as written at the quantifier's head.</param>
/// <param name="CollectionName">The collection name at the head, not yet resolved against the registry.</param>
/// <param name="Body">The element condition.</param>
/// <param name="Span">The quantifier's own span.</param>
internal sealed record QuantifierNode(
    QuantifierKind Kind,
    string Variable,
    string CollectionName,
    RuleNode Body,
    SourceSpan Span
) : RuleNode(Span);
