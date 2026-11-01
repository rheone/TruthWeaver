namespace TruthWeaver.Diagnostics;

/// <summary>
/// Well-known <see cref="Diagnostic.Code"/> values produced by the compiler pipeline. Every code carries the
/// <c>TRE</c> prefix ("Trinary Rule Expression") followed by a four-digit number; the prefix was <c>BRE</c> before
/// ADR-0007 and the numbers are unchanged.
/// </summary>
public static class DiagnosticCodes
{
    /// <summary>A syntax error while parsing rule text.</summary>
    public const string SyntaxError = "TRE0001";

    /// <summary>
    /// A term references a predicate name with no matching registration, or a tree <c>op</c> or an operator call names an
    /// operator that does not exist. <c>Collapse</c>, <c>Project</c> and <c>NXOR</c> are not operators, so they get this code
    /// too, with a hint that points to what to use instead.
    /// </summary>
    public const string UnknownPredicate = "TRE0002";

    /// <summary>A term omits a required argument declared by the predicate's schema.</summary>
    public const string MissingArgument = "TRE0003";

    /// <summary>A term supplies an argument whose value's kind does not match the predicate's schema.</summary>
    public const string ArgumentTypeMismatch = "TRE0004";

    /// <summary>A term supplies an argument name the predicate's schema does not declare.</summary>
    public const string UnknownArgument = "TRE0005";

    /// <summary>
    /// An operator was given an operand count it does not accept: other than two for <c>XOR</c>, <c>EQUIVALENT</c>
    /// (<c>XNOR</c>), <c>IMPLIES</c>, <c>NAND</c> and <c>NOR</c>, other than one for <c>NOT</c> and the inspections, other than
    /// three for <c>If</c>, and fewer than the minimum for the n-ary operators. The code is the same in rule text, JSON, YAML
    /// and <c>RuleBuilder</c>. The name is historical: it began as the binary-operator code.
    /// </summary>
    public const string InfixArityViolation = "TRE0006";

    /// <summary>
    /// An infix operator other than <c>NOT</c>/<c>AND</c>/<c>OR</c> (<c>XOR</c>, <c>XNOR</c>, ...) was combined
    /// with <c>AND</c>/<c>OR</c> or with a different such operator at the same syntactic level without
    /// parentheses (ADR-0005 decision 8). The span is the offending operator, or the bare infix expression.
    /// </summary>
    public const string AmbiguousOperatorMixing = "TRE0007";

    /// <summary>
    /// A count-threshold operator (<c>AtLeast</c>/<c>AtMost</c>/<c>GreaterThan</c>/<c>LessThan</c>/
    /// <c>Exactly</c>) was given a <c>k</c>, or <c>BETWEEN</c> was given bounds, that are not whole numbers or that make it a
    /// structural constant for its operand count.
    /// </summary>
    public const string InvalidThresholdValue = "TRE0008";

    /// <summary>The expression tree exceeds <c>CompilerOptions.MaxDepth</c>.</summary>
    public const string MaxDepthExceeded = "TRE0009";

    /// <summary>The expression tree exceeds <c>CompilerOptions.MaxNodeCount</c>.</summary>
    public const string MaxNodeCountExceeded = "TRE0010";

    /// <summary>Constant/contradiction analysis was skipped because the term count exceeds <c>CompilerOptions.MaxAnalysisTerms</c>.</summary>
    public const string AnalysisSkippedTooManyTerms = "TRE0011";

    /// <summary>
    /// The Strong K3 analyzer found a sub-expression that is <c>True</c> for every <c>{True, False, Unknown}</c>
    /// assignment of its terms (a K3 tautology). <c>A OR NOT A</c> is not one: it is <c>Unknown</c> when
    /// <c>A</c> is. The name is historical ("structural" as opposed to evaluated) and kept so the code and
    /// constant are stable.
    /// </summary>
    public const string StructuralTautology = "TRE0012";

    /// <summary>
    /// The Strong K3 analyzer found a sub-expression that is <c>False</c> for every <c>{True, False, Unknown}</c>
    /// assignment of its terms (a K3 contradiction). <c>A AND NOT A</c> is not one: it is <c>Unknown</c> when
    /// <c>A</c> is. The name is historical ("structural" as opposed to evaluated) and kept so the code and
    /// constant are stable.
    /// </summary>
    public const string StructuralContradiction = "TRE0013";

    /// <summary>
    /// The shape of a tree/JSON/YAML source is wrong independently of the rule's meaning: a missing discriminator key, a key
    /// the node does not define, a value of the wrong JSON or YAML kind, or text that is not well-formed JSON or YAML. A
    /// mistake with its own code (an operand count, a bound, an unknown operator) never gets this one.
    /// </summary>
    public const string MalformedTree = "TRE0014";

    /// <summary>A string literal in DSL rule text contains a <c>\</c> not followed by one of the supported escapes (<c>\"</c>, <c>\\</c>, <c>\n</c>, <c>\t</c>).</summary>
    public const string InvalidEscapeSequence = "TRE0015";

    /// <summary>
    /// An expanding rewrite (<c>ExpandToPrimitives</c>, <c>ExpandToNand</c> or <c>ExpandToNor</c>) would produce more than
    /// <c>CompilerOptions.MaxRewriteNodeCount</c> nodes and was not performed.
    /// </summary>
    public const string RewriteTooLarge = "TRE0016";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an inspection (<c>IsTrue</c>, <c>IsFalse</c>, <c>IsUnknown</c>,
    /// <c>IsKnown</c>) over an operand that can never be <c>Unknown</c>, or never be known, so the inspection is redundant.
    /// </summary>
    public const string RedundantInspection = "TRE0017";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): a <c>COALESCE</c> operand can never be <c>Unknown</c>, so the
    /// operands after it are never reached.
    /// </summary>
    public const string RedundantCoalesce = "TRE0018";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an <c>If</c> whose condition is always <c>True</c> or always
    /// <c>False</c>, so only one branch can ever be chosen.
    /// </summary>
    public const string ConstantIfCondition = "TRE0019";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an <c>If</c> whose two branches are the same expression, so the
    /// condition does not affect the result.
    /// </summary>
    public const string IdenticalIfBranches = "TRE0020";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): a threshold or <c>BETWEEN</c> whose constant operands already fix
    /// its value, so it is the same constant whatever the other operands are.
    /// </summary>
    public const string VacuousCardinality = "TRE0021";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an operand repeated inside <c>AND</c>, <c>OR</c>, <c>ANY</c>,
    /// <c>ALL</c> or <c>COALESCE</c>, where the repeat adds nothing.
    /// </summary>
    public const string DuplicateOperands = "TRE0022";

    /// <summary>Lint (opt-in via <c>CompilerOptions.Lints</c>): <c>NOT (NOT x)</c>, which is <c>x</c> in Strong K3.</summary>
    public const string DoubleNegation = "TRE0023";

    /// <summary>
    /// A variable reference, <c>from("name", "query")</c>, names a data source that was not declared in
    /// <c>CompilerOptions.DataSources</c> (ADR-0006 decision 4). The span is the reference.
    /// </summary>
    public const string UndeclaredDataSource = "TRE0024";

    /// <summary>
    /// A variable reference's query is not valid in the dialect of its data source, according to the
    /// <c>IQueryValidator</c> the source name was declared with (ADR-0006 decision 4). The span or path is the query string.
    /// </summary>
    public const string MalformedDataQuery = "TRE0025";

    /// <summary>
    /// The literal argument values of a predicate call break a rule that the predicate's
    /// <c>PredicateSchema.ArgumentValidator</c> checks, for example reversed bounds (<c>lower</c> greater than
    /// <c>upper</c>) on an <c>IsBetween</c> or <c>IsNotBetween</c> range predicate. The span or path is the predicate call.
    /// </summary>
    public const string InvalidArgumentValue = "TRE0026";

    /// <summary>
    /// A rule uses a predicate whose <c>PredicateSchema.Deprecation</c> is set. This is a warning, one per use: the rule
    /// still compiles. The span or path is the predicate call, and the replacement, when given, is the suggestion.
    /// </summary>
    public const string DeprecatedPredicate = "TRE0027";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): the rule's depth reaches <c>CompilerOptions.DeepNestingFraction</c>
    /// of <c>CompilerOptions.MaxDepth</c>, so it is close to the compile limit.
    /// </summary>
    public const string DeepNesting = "TRE0028";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an <c>AND</c> or <c>OR</c> chain with more operands than
    /// <c>CompilerOptions.WideChainOperandLimit</c>.
    /// </summary>
    public const string WideChain = "TRE0029";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): <c>Canonicalize()</c> would change the rule. The suggestion is the
    /// canonical rule text.
    /// </summary>
    public const string NotCanonical = "TRE0030";

    /// <summary>
    /// Warning: <c>ToNnf</c>, <c>ToCnf</c> or <c>ToDnf</c> kept a threshold (<c>AtLeast</c>, <c>AtMost</c>, <c>Exactly</c> and
    /// the like) as an atom, because expanding it can grow the rule a lot. The message gives the growth estimate. Pass
    /// <c>NormalFormOptions</c> with <c>ExpandThresholds</c> set to expand it.
    /// </summary>
    public const string ThresholdKeptAsAtom = "TRE0031";

    /// <summary>
    /// A predicate call names the same argument more than once, in rule text, JSON, YAML or <c>RuleBuilder</c>. The span or
    /// path is the repeated argument. No occurrence wins: the call is an error.
    /// </summary>
    public const string DuplicateArgument = "TRE0032";

    /// <summary>
    /// Warning: <c>CompressToDerived</c>, <c>Canonicalize</c> or <c>Simplify</c> stopped at its pass limit before the rule
    /// stopped changing. The rule is equivalent to the input, but rewriting it again can still change it.
    /// </summary>
    public const string RewriteNotConverged = "TRE0033";

    /// <summary>
    /// Warning: <c>PrintTruthTable</c> printed a table with more terms than the default cap of 8, because the options set
    /// <c>AllowLargerTable</c>. The message gives the row count.
    /// </summary>
    public const string TruthTableCapRaised = "TRE0034";

    /// <summary>
    /// Error: <c>PrintTruthTable</c> refused to print a table. The rule has more terms than the default cap of 8 and the options
    /// do not set <c>AllowLargerTable</c>, or it has more terms than <c>TruthTableOptions.TermLimit</c> (12 by default). Nothing was written.
    /// </summary>
    public const string TruthTableTooLarge = "TRE0035";

    /// <summary>
    /// A <c>let</c> definition references itself, directly or through a chain of other definitions' uses (ADR-0008
    /// decision 6). The message names every definition in the cycle, in the order the compiler found them. The span is
    /// the use that closed the cycle.
    /// </summary>
    public const string DefinitionCycleDetected = "TRE0036";

    /// <summary>
    /// A <c>let</c> definition's name clashes with another definition already declared in the same document, a
    /// reserved word (including <c>let</c> itself and <c>T</c>/<c>F</c>/<c>U</c>), or a predicate name registered with
    /// the compiler (ADR-0008 decision 6), or one of its parameters is a reserved word. The span is the definition's own
    /// name, not a use of it.
    /// </summary>
    public const string DefinitionNameCollision = "TRE0037";

    /// <summary>
    /// Expanding a use would nest more <c>let</c> definitions deep than <c>CompilerOptions.MaxDefinitionDepth</c>
    /// (ADR-0008 decision 6), independent of <c>MaxDepthExceeded</c>, which bounds the expanded tree's structural
    /// depth, not the definition chain, and independent of <see cref="DefinitionCycleDetected"/>, which only catches an
    /// unbounded (cyclic) chain, not a pathologically deep but non-cyclic one.
    /// </summary>
    public const string MaxDefinitionDepthExceeded = "TRE0038";

    /// <summary>
    /// A use of a <c>let</c> definition supplies a different number of arguments than the definition declares
    /// parameters (ADR-0008 decision 6). The span is the use.
    /// </summary>
    public const string DefinitionArgumentCountMismatch = "TRE0039";

    /// <summary>
    /// A use names a <c>let</c> definition that was never declared in this document (ADR-0008 decision 6). The span is
    /// the use.
    /// </summary>
    public const string UnknownDefinition = "TRE0040";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>, <c>TruthWeaver.Compilation.LintRules.UnusedDefinition</c>): a
    /// <c>let</c> definition with no use anywhere in the document (ADR-0008 decision 6). The span is the definition's
    /// own name.
    /// </summary>
    public const string UnusedDefinition = "TRE0041";

    /// <summary>
    /// A <c>FORALL</c>/<c>EXISTS</c> head names a collection no <c>PredicateRegistryBuilder.AddCollection</c> call
    /// registered (ADR-0010 decision 3). The span or path is the collection name.
    /// </summary>
    public const string UnknownCollection = "TRE0042";

    /// <summary>
    /// A rule nests quantifiers deeper than <c>CompilerOptions.MaxQuantifierDepth</c> (ADR-0010 decision 6). The span
    /// or path is the innermost quantifier that crossed the limit.
    /// </summary>
    public const string MaxQuantifierDepthExceeded = "TRE0043";

    /// <summary>
    /// A quantifier's bound variable name is already in scope: reusing an enclosing quantifier's own variable name
    /// (ADR-0010 decision 2). The span or path is the inner quantifier's head.
    /// </summary>
    public const string ShadowedVariable = "TRE0044";

    /// <summary>
    /// Constant/contradiction analysis stopped partway and was skipped because the shared decision diagram it was
    /// building would need more nodes than the analyzer's internal node budget, even though the rule's distinct-term
    /// count is within <c>CompilerOptions.MaxAnalysisTerms</c> (ticket 24). The diagram's size depends on the term
    /// ordering and the rule's shape, not only on the term count, so a rule with few terms but an unlucky ordering can
    /// still trip it. The rule still compiles.
    /// </summary>
    public const string AnalysisSkippedNodeBudgetExceeded = "TRE0045";

    /// <summary>
    /// A <c>let</c> definition's own parameter list repeats a name (compared case-insensitively), in rule text, JSON,
    /// YAML or <c>RuleBuilder</c> (ticket 20). Binding a use's arguments by parameter name would otherwise let a
    /// later duplicate silently overwrite an earlier one, the same "no occurrence wins" rationale as
    /// <see cref="DuplicateArgument"/> for a repeated predicate argument name. The span or path is the definition's
    /// own declaration, not any particular use.
    /// </summary>
    public const string DuplicateDefinitionParameter = "TRE0046";

    /// <summary>
    /// A <c>rule("name")</c> reference names a rule that the compiler's <c>CompilerOptions.Library</c> does not hold, or
    /// the compiler has no library. The message names the rule. The span is the whole reference.
    /// </summary>
    public const string UnknownLibraryRule = "TRE0047";

    /// <summary>
    /// A pinned reference <c>rule("name", hash: "...")</c> names a content hash that is not the current content hash of
    /// the library rule. The message names the rule, the pinned hash and the current hash. The span is the whole reference.
    /// </summary>
    public const string LibraryRuleHashMismatch = "TRE0048";

    /// <summary>
    /// A library rule references itself, directly or through a chain of other library rules. The message lists the cycle
    /// path, such as <c>a -&gt; b -&gt; a</c>. The span is the reference in the compiled text that leads into the cycle.
    /// </summary>
    public const string LibraryRuleCycleDetected = "TRE0049";

    /// <summary>
    /// A library rule does not compile against the compile that references it: a predicate name or a data source name of
    /// the library rule is not in the caller's registry or data sources, or the library rule has another error. The
    /// message names the library rule and the error. The span is the reference in the compiled text.
    /// </summary>
    public const string InvalidLibraryRule = "TRE0050";
}
