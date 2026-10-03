namespace TruthWeaver.Diagnostics;

/// <summary>Well-known <see cref="Diagnostic.Code"/> values produced by the compiler pipeline.</summary>
public static class DiagnosticCodes
{
    /// <summary>A syntax error while parsing rule text.</summary>
    public const string SyntaxError = "BRE0001";

    /// <summary>A term references a predicate name with no matching registration.</summary>
    public const string UnknownPredicate = "BRE0002";

    /// <summary>A term omits a required argument declared by the predicate's schema.</summary>
    public const string MissingArgument = "BRE0003";

    /// <summary>A term supplies an argument whose value's kind does not match the predicate's schema.</summary>
    public const string ArgumentTypeMismatch = "BRE0004";

    /// <summary>A term supplies an argument name the predicate's schema does not declare.</summary>
    public const string UnknownArgument = "BRE0005";

    /// <summary><c>XOR</c>, <c>EQUIVALENT</c> (<c>XNOR</c>), <c>IMPLIES</c>, <c>NAND</c> or <c>NOR</c> was given other than exactly two operands.</summary>
    public const string InfixArityViolation = "BRE0006";

    /// <summary>
    /// An infix operator other than <c>NOT</c>/<c>AND</c>/<c>OR</c> (<c>XOR</c>, <c>XNOR</c>, ...) was combined
    /// with <c>AND</c>/<c>OR</c> or with a different such operator at the same syntactic level without
    /// parentheses (ADR-0005 decision 8). The span is the offending operator, or the bare infix expression.
    /// </summary>
    public const string AmbiguousOperatorMixing = "BRE0007";

    /// <summary>
    /// A count-threshold operator (<c>AtLeast</c>/<c>AtMost</c>/<c>GreaterThan</c>/<c>LessThan</c>/
    /// <c>Exactly</c>) was given a <c>k</c> that makes it a structural constant for its operand count.
    /// </summary>
    public const string InvalidThresholdValue = "BRE0008";

    /// <summary>The expression tree exceeds <c>CompilerOptions.MaxDepth</c>.</summary>
    public const string MaxDepthExceeded = "BRE0009";

    /// <summary>The expression tree exceeds <c>CompilerOptions.MaxNodeCount</c>.</summary>
    public const string MaxNodeCountExceeded = "BRE0010";

    /// <summary>Constant/contradiction analysis was skipped because the term count exceeds <c>CompilerOptions.MaxAnalysisTerms</c>.</summary>
    public const string AnalysisSkippedTooManyTerms = "BRE0011";

    /// <summary>
    /// The Strong K3 analyzer found a sub-expression that is <c>True</c> for every <c>{True, False, Unknown}</c>
    /// assignment of its terms (a K3 tautology). <c>A OR NOT A</c> is not one: it is <c>Unknown</c> when
    /// <c>A</c> is. The name is historical ("structural" as opposed to evaluated) and kept so the code and
    /// constant are stable.
    /// </summary>
    public const string StructuralTautology = "BRE0012";

    /// <summary>
    /// The Strong K3 analyzer found a sub-expression that is <c>False</c> for every <c>{True, False, Unknown}</c>
    /// assignment of its terms (a K3 contradiction). <c>A AND NOT A</c> is not one: it is <c>Unknown</c> when
    /// <c>A</c> is. The name is historical ("structural" as opposed to evaluated) and kept so the code and
    /// constant are stable.
    /// </summary>
    public const string StructuralContradiction = "BRE0013";

    /// <summary>The tree/JSON/YAML source is malformed independently of DSL syntax (e.g. unknown <c>op</c>, missing discriminator key).</summary>
    public const string MalformedTree = "BRE0014";

    /// <summary>A string literal in DSL rule text contains a <c>\</c> not followed by one of the supported escapes (<c>\"</c>, <c>\\</c>, <c>\n</c>, <c>\t</c>).</summary>
    public const string InvalidEscapeSequence = "BRE0015";

    /// <summary>
    /// An expanding rewrite (<c>ExpandToPrimitives</c>, <c>ExpandToNand</c> or <c>ExpandToNor</c>) would produce more than
    /// <c>CompilerOptions.MaxRewriteNodeCount</c> nodes and was not performed.
    /// </summary>
    public const string RewriteTooLarge = "BRE0016";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an inspection (<c>IsTrue</c>, <c>IsFalse</c>, <c>IsUnknown</c>,
    /// <c>IsKnown</c>) over an operand that can never be <c>Unknown</c>, or never be known, so the inspection is redundant.
    /// </summary>
    public const string RedundantInspection = "BRE0017";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): a <c>COALESCE</c> operand can never be <c>Unknown</c>, so the
    /// operands after it are never reached.
    /// </summary>
    public const string RedundantCoalesce = "BRE0018";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an <c>If</c> whose condition is always <c>True</c> or always
    /// <c>False</c>, so only one branch can ever be chosen.
    /// </summary>
    public const string ConstantIfCondition = "BRE0019";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an <c>If</c> whose two branches are the same expression, so the
    /// condition does not affect the result.
    /// </summary>
    public const string IdenticalIfBranches = "BRE0020";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): a threshold or <c>BETWEEN</c> whose constant operands already fix
    /// its value, so it is the same constant whatever the other operands are.
    /// </summary>
    public const string VacuousCardinality = "BRE0021";

    /// <summary>
    /// Lint (opt-in via <c>CompilerOptions.Lints</c>): an operand repeated inside <c>AND</c>, <c>OR</c>, <c>ANY</c>,
    /// <c>ALL</c> or <c>COALESCE</c>, where the repeat adds nothing.
    /// </summary>
    public const string DuplicateOperands = "BRE0022";

    /// <summary>Lint (opt-in via <c>CompilerOptions.Lints</c>): <c>NOT (NOT x)</c>, which is <c>x</c> in Strong K3.</summary>
    public const string DoubleNegation = "BRE0023";
}
