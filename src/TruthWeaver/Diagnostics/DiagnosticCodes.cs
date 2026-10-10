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

    /// <summary>A term references a predicate name with no matching registration.</summary>
    public const string UnknownPredicate = "TRE0002";

    /// <summary>A term omits a required argument declared by the predicate's schema.</summary>
    public const string MissingArgument = "TRE0003";

    /// <summary>A term supplies an argument whose value's kind does not match the predicate's schema.</summary>
    public const string ArgumentTypeMismatch = "TRE0004";

    /// <summary>A term supplies an argument name the predicate's schema does not declare.</summary>
    public const string UnknownArgument = "TRE0005";

    /// <summary><c>XOR</c>, <c>EQUIVALENT</c> (<c>XNOR</c>), <c>IMPLIES</c>, <c>NAND</c> or <c>NOR</c> was given other than exactly two operands.</summary>
    public const string InfixArityViolation = "TRE0006";

    /// <summary>
    /// An infix operator other than <c>NOT</c>/<c>AND</c>/<c>OR</c> (<c>XOR</c>, <c>XNOR</c>, ...) was combined
    /// with <c>AND</c>/<c>OR</c> or with a different such operator at the same syntactic level without
    /// parentheses (ADR-0005 decision 8). The span is the offending operator, or the bare infix expression.
    /// </summary>
    public const string AmbiguousOperatorMixing = "TRE0007";

    /// <summary>
    /// A count-threshold operator (<c>AtLeast</c>/<c>AtMost</c>/<c>GreaterThan</c>/<c>LessThan</c>/
    /// <c>Exactly</c>) was given a <c>k</c> that makes it a structural constant for its operand count.
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

    /// <summary>The tree/JSON/YAML source is malformed independently of DSL syntax (e.g. unknown <c>op</c>, missing discriminator key).</summary>
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
    /// <c>upper</c>) on a <c>Between</c> or <c>Outside</c> range predicate. The span or path is the predicate call.
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
}
