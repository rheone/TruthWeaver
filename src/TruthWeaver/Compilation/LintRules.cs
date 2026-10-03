namespace TruthWeaver.Compilation;

/// <summary>
/// The opt-in lint rules the compiler can run after analysis (set through <see cref="CompilerOptions.Lints"/>). Each one
/// flags a construct that is redundant under Strong Kleene (K3) semantics and reports it as an informational
/// diagnostic with a replacement suggestion; none changes the compiled rule. The default is <see cref="None"/>, so a
/// rule that compiled clean before keeps compiling clean.
/// </summary>
[Flags]
public enum LintRules
{
    /// <summary>No lint runs.</summary>
    None = 0,

    /// <summary>
    /// An <c>IsTrue</c>/<c>IsFalse</c>/<c>IsUnknown</c>/<c>IsKnown</c> whose operand can never be <c>Unknown</c> (or can
    /// never be known), so the inspection is a constant or a plain restatement of its operand (<c>BRE0017</c>).
    /// </summary>
    RedundantInspection = 1,

    /// <summary>
    /// A <c>COALESCE</c> with an operand that can never be <c>Unknown</c> followed by more operands, which are never
    /// reached (<c>BRE0018</c>).
    /// </summary>
    RedundantCoalesce = 2,

    /// <summary>
    /// An <c>If</c> whose condition can only ever be <c>True</c> or only ever be <c>False</c>, so one branch is dead
    /// (<c>BRE0019</c>).
    /// </summary>
    ConstantIfCondition = 4,

    /// <summary>An <c>If</c> whose two branches are the same expression, so the condition does not matter (<c>BRE0020</c>).</summary>
    IdenticalIfBranches = 8,

    /// <summary>
    /// A threshold (<c>AtLeast</c>, <c>AtMost</c>, <c>GreaterThan</c>, <c>LessThan</c>, <c>Exactly</c>) or <c>BETWEEN</c>
    /// whose constant operands already decide the result whatever the other operands are (<c>BRE0021</c>).
    /// </summary>
    VacuousCardinality = 16,

    /// <summary>
    /// An operand repeated, structurally identical, inside <c>AND</c>, <c>OR</c>, <c>ANY</c>, <c>ALL</c> or <c>COALESCE</c>,
    /// where the repeat can be dropped without changing the value (<c>BRE0022</c>).
    /// </summary>
    DuplicateOperands = 32,

    /// <summary><c>NOT (NOT x)</c>, which is <c>x</c> in Strong K3 (<c>BRE0023</c>).</summary>
    DoubleNegation = 64,

    /// <summary>Every lint rule.</summary>
    All =
        RedundantInspection
        | RedundantCoalesce
        | ConstantIfCondition
        | IdenticalIfBranches
        | VacuousCardinality
        | DuplicateOperands
        | DoubleNegation,
}
