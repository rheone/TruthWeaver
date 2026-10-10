namespace TruthWeaver.Evaluation;

using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Json;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Rewriting;

/// <summary>
/// The immutable, thread-safe result of compiling a rule's text (CONTEXT.md). Safe to cache and
/// share; compile once, evaluate many times. Runtime rule updates are a compile-and-swap of the
/// reference holding the active instance — no lock is needed (ADR-0002).
/// </summary>
/// <typeparam name="TContext">The application context type this rule evaluates against.</typeparam>
public sealed class CompiledRule<TContext>
{
    private readonly PredicateRegistry<TContext> registry;
    private readonly ILogger logger;
    private readonly Lazy<string> canonicalText;
    private readonly Lazy<IReadOnlySet<string>> predicateNames;
    private readonly Lazy<RuleMetrics> metrics;
    private readonly CompilerOptions options;

    internal CompiledRule(
        Expression root,
        PredicateRegistry<TContext> registry,
        ILogger? logger = null,
        CompilerOptions? options = null
    )
    {
        this.options = options ?? CompilerOptions.Default;
        this.Root = root;
        this.registry = registry;
        this.logger = logger ?? NullLogger.Instance;
        this.canonicalText = new Lazy<string>(() => CanonicalPrinter.Print(this.Root));
        this.predicateNames = new Lazy<IReadOnlySet<string>>(() => CollectPredicateNames(this.Root));
        this.metrics = new Lazy<RuleMetrics>(() => RuleMetrics.Measure(this.Root, this.options.MaxAnalysisTerms));
    }

    /// <summary>Gets this rule's canonical printed DSL text — the form <c>RuleCompiler.Compile</c> reproduces a structurally equal tree from.</summary>
    public string CanonicalText => this.canonicalText.Value;

    /// <summary>
    /// Gets the name of each predicate this rule references, once each, in the casing term identity uses (the registered
    /// casing for a registered predicate). A predicate used by several terms, or written in different casings, appears once.
    /// The set is empty for a rule with no terms.
    /// </summary>
    public IReadOnlySet<string> PredicateNames => this.predicateNames.Value;

    /// <summary>
    /// Gets the size and analysis cost of this rule. The measures are computed on first read and cached, so a host that
    /// never reads them pays nothing. <see cref="RuleMetrics.BddNodeCount"/> runs the K3 analysis on demand, bounded by
    /// the <see cref="CompilerOptions.MaxAnalysisTerms"/> this rule was compiled with.
    /// </summary>
    public RuleMetrics Metrics => this.metrics.Value;

    /// <summary>
    /// Gets the underlying expression tree. Internal — visible to <c>TruthWeaver.Yaml</c> via
    /// <c>InternalsVisibleTo</c>, so its YAML printer can render the same tree <see cref="CanonicalText"/>
    /// and <see cref="PrintJson"/> render, without this package needing to know YAML exists.
    /// </summary>
    internal Expression Root { get; }

    /// <summary>
    /// Prints this rule as DSL text with the chosen grouping delimiters. <see cref="GroupingStyle.Parentheses"/> returns
    /// exactly <see cref="CanonicalText"/>; <see cref="GroupingStyle.DepthCycling"/> varies the delimiter by nesting depth
    /// for readability. Every style re-parses to a tree equal to this rule's, because the DSL treats <c>()</c>, <c>[]</c>
    /// and <c>{}</c> as the same grouping.
    /// </summary>
    /// <param name="grouping">The grouping delimiters to print with.</param>
    /// <returns>The DSL text.</returns>
    public string PrintRuleText(GroupingStyle grouping)
    {
        return grouping == GroupingStyle.Parentheses ? this.CanonicalText : CanonicalPrinter.Print(this.Root, grouping);
    }

    /// <summary>
    /// Rewrites every derived operator into the primitive kernel — <c>NOT</c>, <c>AND</c>, <c>OR</c>, <c>AtLeast</c>,
    /// <c>AtMost</c>, <c>Exactly</c> and <c>COALESCE</c> — and returns the result as a new rule (ADR-0005 decision 10). The
    /// derived operators are <c>IMPLIES</c>, <c>EQUIVALENT</c>, <c>XOR</c>, <c>NAND</c>, <c>NOR</c>, <c>PARITY</c>,
    /// <c>ExactlyOne</c>, <c>ANY</c>, <c>ALL</c>, <c>NONE</c>, <c>BETWEEN</c>, <c>GreaterThan</c>, <c>LessThan</c>,
    /// <c>If</c> and the four inspections; every one of them has a kernel definition, so nothing is left
    /// unexpanded.
    /// </summary>
    /// <remarks>
    /// The result evaluates to the same <see cref="TruthValue"/> as this rule for every assignment of its terms, and
    /// records the same faults for predicates that throw. This rule is immutable and is not changed. The expanded rule prints canonical text that compiles back to the same tree when that text fits <see cref="CompilerOptions.MaxNodeCount"/> (512 by default; raise it otherwise). The expanded rule is usually larger:
    /// an operator whose definition mentions an operand twice (<c>XOR</c>, <c>EQUIVALENT</c>, <c>If</c>, the inspections)
    /// repeats that operand's text, so deeply nested rules grow quickly (exponentially in the nesting depth).
    /// <para>
    /// The result is capped at <paramref name="maxNodeCount"/> nodes (the rule's own <see cref="CompilerOptions.MaxRewriteNodeCount"/> by default), counted as a printed tree. A larger
    /// result is not built: the call returns a failed <see cref="CompilationResult{TContext}"/> carrying a
    /// <see cref="Diagnostics.DiagnosticCodes.RewriteTooLarge"/> error, and never throws for size. A result over <see cref="CompilerOptions.MaxNodeCount"/> (512 by default) is a valid rule here, but its printed text compiles back only when you raise <c>MaxNodeCount</c>; that cap is independent of <see cref="CompilerOptions.MaxRewriteNodeCount"/>.
    /// </para>
    /// </remarks>
    /// <param name="maxNodeCount">The most nodes, counted as a printed tree, the rewritten rule may have. It controls only that cap. <see langword="null"/> uses the <see cref="CompilerOptions.MaxRewriteNodeCount"/> this rule was compiled with; pass a larger value to allow bigger results.</param>
    /// <returns>The new rule over the same predicates whose tree contains only primitive operators, constants and terms, or a failure when the result would exceed the cap.</returns>
    public CompilationResult<TContext> ExpandToPrimitives(int? maxNodeCount = null)
    {
        int cap = maxNodeCount ?? this.options.MaxRewriteNodeCount;
        Expression expanded = PrimitiveExpander.Expand(this.Root);
        return this.RewriteResult(expanded, "ExpandToPrimitives", cap);
    }

    /// <summary>
    /// Rewrites this rule so its only logical operator is <c>NAND</c>, and returns it as a new rule (ADR-0005 decision 10):
    /// <c>NOT a</c> becomes <c>a NAND a</c>, <c>a AND b</c> becomes <c>(a NAND b) NAND (a NAND b)</c> and <c>a OR b</c>
    /// becomes <c>(a NAND a) NAND (b NAND b)</c>; every other operator is first expanded to the primitive kernel (see
    /// <see cref="ExpandToPrimitives"/>).
    /// </summary>
    /// <remarks>
    /// The result evaluates to the same <see cref="TruthValue"/> for every assignment of its terms; this rule is not
    /// changed. <b>One documented boundary:</b>
    /// <c>COALESCE</c> (and therefore the inspections <c>IsTrue</c>, <c>IsFalse</c>, <c>IsUnknown</c>,
    /// <c>IsKnown</c>, which expand to it) cannot be written with <c>NAND</c>, because every <c>NAND</c> circuit is monotone
    /// in the information order and <c>COALESCE</c> is not. Such nodes stay as <c>COALESCE</c> with their operands rewritten,
    /// so a rule without them is <c>NAND</c>-only. Thresholds become a disjunction over operand subsets, so wide
    /// thresholds grow combinatorially (<c>C(n, k)</c> subsets for <c>AtLeast(k)</c> over <c>n</c> operands).
    /// <para>
    /// The result is capped at <paramref name="maxNodeCount"/> nodes (the rule's own <see cref="CompilerOptions.MaxRewriteNodeCount"/> by default), counted as a printed tree; the cost
    /// is estimated first, so an over-cap rewrite is refused without being built. The call then returns a failed
    /// <see cref="CompilationResult{TContext}"/> carrying a <see cref="Diagnostics.DiagnosticCodes.RewriteTooLarge"/> error
    /// and never throws for size. A result over <see cref="CompilerOptions.MaxNodeCount"/> (512 by default) is a valid rule here, but its printed text compiles back only when you raise <c>MaxNodeCount</c>; that cap is independent of <see cref="CompilerOptions.MaxRewriteNodeCount"/>.
    /// </para>
    /// </remarks>
    /// <param name="maxNodeCount">The most nodes, counted as a printed tree, the rewritten rule may have. It controls only that cap. <see langword="null"/> uses the <see cref="CompilerOptions.MaxRewriteNodeCount"/> this rule was compiled with; pass a larger value to allow bigger results.</param>
    /// <returns>The new rule over the same predicates whose logic is <c>NAND</c> (plus any <c>COALESCE</c> boundary), or a failure when the result would exceed the cap.</returns>
    public CompilationResult<TContext> ExpandToNand(int? maxNodeCount = null)
    {
        int cap = maxNodeCount ?? this.options.MaxRewriteNodeCount;
        return this.RewriteResult(NandNorExpander.ToNand(this.Root, cap), "ExpandToNand", cap);
    }

    /// <summary>
    /// Rewrites this rule so its only logical operator is <c>NOR</c>, and returns it as a new rule (ADR-0005 decision 10):
    /// <c>NOT a</c> becomes <c>a NOR a</c>, <c>a OR b</c> becomes <c>(a NOR b) NOR (a NOR b)</c> and <c>a AND b</c>
    /// becomes <c>(a NOR a) NOR (b NOR b)</c>; every other operator is first expanded to the primitive kernel.
    /// </summary>
    /// <remarks>
    /// Same guarantees, cost, size cap, recompile caveat (a result over <see cref="CompilerOptions.MaxNodeCount"/> compiles back only when you raise it) and <c>COALESCE</c> boundary as <see cref="ExpandToNand"/>.
    /// </remarks>
    /// <param name="maxNodeCount">The most nodes, counted as a printed tree, the rewritten rule may have. It controls only that cap. <see langword="null"/> uses the <see cref="CompilerOptions.MaxRewriteNodeCount"/> this rule was compiled with; pass a larger value to allow bigger results.</param>
    /// <returns>The new rule over the same predicates whose logic is <c>NOR</c> (plus any <c>COALESCE</c> boundary), or a failure when the result would exceed the cap.</returns>
    public CompilationResult<TContext> ExpandToNor(int? maxNodeCount = null)
    {
        int cap = maxNodeCount ?? this.options.MaxRewriteNodeCount;
        return this.RewriteResult(NandNorExpander.ToNor(this.Root, cap), "ExpandToNor", cap);
    }

    /// <summary>
    /// Rewrites this rule into readable derived operators wherever a Strong Kleene-sound pattern matches, and returns it as a
    /// new rule (ADR-0005 decision 10). It is the inverse direction of <see cref="ExpandToPrimitives"/>: the usual input is
    /// an expanded rule, but any rule is accepted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The recognised patterns are <c>OR(NOT a, b)</c> to <c>IMPLIES</c>; <c>NOT(AND(a, b))</c> and <c>OR(NOT a, NOT b)</c>
    /// to <c>NAND</c>; <c>NOT(OR(a, b))</c> and <c>AND(NOT a, NOT b)</c> to <c>NOR</c>; the exact <c>XOR</c>,
    /// <c>EQUIVALENT</c>, <c>If</c> and <c>PARITY</c> shapes <see cref="ExpandToPrimitives"/> produces; <c>AtLeast(1)</c> to
    /// <c>ANY</c>, <c>AtLeast(n)</c> to <c>ALL</c>, <c>AtMost(0)</c> to <c>NONE</c> and <c>Exactly(1)</c> to
    /// <c>ExactlyOne</c>; a matching <c>AtLeast</c>/<c>AtMost</c> pair under <c>AND</c> to <c>BETWEEN</c>; and
    /// <c>COALESCE(NOT x, False)</c> to <c>IsFalse</c>, with the
    /// <c>IsUnknown</c>/<c>IsKnown</c> pairs of those. Every pattern is an identity in Strong Kleene logic, checked against a
    /// truth-table oracle; classical-only shortcuts are never used.
    /// </para>
    /// <para>
    /// The result evaluates to the same value as this rule for every <c>True</c>/<c>False</c>/<c>Unknown</c> assignment, is
    /// never larger (in nodes) than this rule, and compressing it again changes nothing. It is not guaranteed to recover the
    /// exact rule that was expanded, only an equivalent, no larger one that uses derived operators. Operand order inside an
    /// <c>OR</c>/<c>AND</c> pattern may change, so the order predicates are invoked in may differ; results do not.
    /// </para>
    /// </remarks>
    /// <returns>A new rule over the same predicates, with derived operators where patterns matched.</returns>
    public CompiledRule<TContext> CompressToDerived()
    {
        return new CompiledRule<TContext>(Compressor.Compress(this.Root), this.registry, this.logger, this.options);
    }

    /// <summary>
    /// Rewrites this rule into its canonical form (ADR-0005 decision 10): one deterministic representation shared by every
    /// rule that is equivalent under a fixed set of Strong Kleene-sound rewrites, so rules can be compared and de-duplicated
    /// by their <see cref="CanonicalText"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rewrites, applied bottom-up and repeated until stable, are: (1) exact aliases collapse to one spelling
    /// (<c>ANY</c> and <c>AtLeast(1)</c> become <c>OR</c>, <c>ALL</c> and <c>AtLeast(n)</c> become <c>AND</c>,
    /// <c>GreaterThan(k)</c> becomes <c>AtLeast(k + 1)</c>, <c>LessThan(k)</c> becomes <c>AtMost(k - 1)</c>,
    /// <c>ExactlyOne</c> becomes <c>Exactly(1)</c>); (2) <c>NOT (NOT x)</c> becomes <c>x</c>; (3) an <c>AND</c> directly
    /// inside an <c>AND</c>, an <c>OR</c> inside an <c>OR</c> and a <c>COALESCE</c> inside a <c>COALESCE</c> are flattened; (4) the operands of the
    /// commutative operators (<c>AND</c>, <c>OR</c>, <c>XOR</c>, <c>EQUIVALENT</c>, <c>NAND</c>, <c>NOR</c>, <c>PARITY</c> and
    /// the threshold family including <c>BETWEEN</c>) are sorted by their canonical text, ordinally; (5) repeated operands of
    /// <c>AND</c>/<c>OR</c> are removed (idempotence). Operators whose operand order carries meaning (<c>COALESCE</c>,
    /// <c>IMPLIES</c>, <c>If</c>) keep it. Nothing is folded and no complement law is used: <c>a OR NOT a</c> is not
    /// <c>True</c> in Strong Kleene logic, so it stays a two-operand <c>OR</c>.
    /// </para>
    /// <para>
    /// The result evaluates to the same value as this rule for every <c>True</c>/<c>False</c>/<c>Unknown</c> assignment, is
    /// never larger than this rule, and canonicalising it again changes nothing. <b>Evaluation order is not preserved.</b>
    /// Reordering, deduplicating and flattening operands can change which predicate runs first, which predicates run at all
    /// once a short-circuit applies, and therefore which faults are reported; the value never changes. Because the order is
    /// text-based, a canonical rule is for comparison and storage keys, not for performance tuning.
    /// </para>
    /// </remarks>
    /// <returns>A new rule over the same predicates in canonical form.</returns>
    public CompiledRule<TContext> Canonicalize()
    {
        return new CompiledRule<TContext>(Canonicalizer.Canonicalize(this.Root), this.registry, this.logger, this.options);
    }

    /// <summary>
    /// Replaces this rule with an equivalent, cheaper one using only Strong Kleene-sound rewrites, and returns it as a new
    /// rule (ADR-0005 decision 10). Starts from <see cref="Canonicalize"/>, then folds and reduces until nothing changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Applied rewrites: constant folding through the K3 tables; identity and annihilator laws with constants
    /// (<c>a AND True = a</c>, <c>a AND False = False</c>, <c>a OR False = a</c>, <c>a OR True = True</c>; an
    /// <c>Unknown</c> operand is kept); idempotence, double negation and flattening (from canonicalisation); absorption
    /// (<c>a AND (a OR b) = a</c>); De Morgan and negation-pushing only where they remove nodes; <c>COALESCE</c> with a
    /// never-<c>Unknown</c> operand; inspections of constants or never-<c>Unknown</c> operands;
    /// <c>If</c> with a constant condition or equal branches; derived operators with a constant operand; and threshold
    /// operators with <c>True</c>/<c>False</c> operands. Classical-only laws are <b>never</b> applied: <c>a OR NOT a</c> is
    /// not <c>True</c>, <c>a AND NOT a</c> is not <c>False</c>, <c>a IMPLIES a</c> and <c>a EQUIVALENT a</c> are not
    /// <c>True</c>, and complement absorption (<c>a AND (NOT a OR b) = a AND b</c>) is rejected, because each fails when
    /// <c>a</c> is <c>Unknown</c>.
    /// </para>
    /// <para>
    /// The result evaluates to the same value as this rule for every <c>True</c>/<c>False</c>/<c>Unknown</c> assignment, is
    /// never larger (in nodes), and simplifying it again changes nothing. <b>Evaluation order and side effects are not
    /// preserved.</b> Operands may be reordered, merged or dropped (an annihilated <c>AND</c> never evaluates its other
    /// operands), so a predicate the original would have invoked, and any fault it would have reported, may not run;
    /// the value never changes.
    /// </para>
    /// </remarks>
    /// <returns>A new rule over the same predicates, simplified.</returns>
    public CompiledRule<TContext> Simplify()
    {
        return new CompiledRule<TContext>(Simplifier.Simplify(this.Root), this.registry, this.logger, this.options);
    }

    /// <summary>
    /// Simplifies this rule as <see cref="Simplify"/> does and also reports each change, in the order the rewrite applied it.
    /// </summary>
    /// <remarks>
    /// Each <see cref="RewriteStep"/> names the <see cref="RewriteLaw"/> and holds the canonical text of the changed
    /// subtree before and after the step. Canonicalization steps (aliases, double negation, flattening, ordering and
    /// repeated operands) come first and again between the simplification passes. The list is empty when the rule is
    /// already simple. A step shows a subtree as it stood when the rewrite reached it, so an earlier step may already have
    /// changed its operands. <see cref="Diffing.RuleDiff"/> compares two finished rules and does not say which law fired; the step
    /// list does.
    /// </remarks>
    /// <returns>The same rule as <see cref="Simplify"/> and the list of steps.</returns>
    public SimplifyResult<TContext> SimplifyWithSteps()
    {
        List<RewriteStep> steps = [];
        Expression simplified = Simplifier.Simplify(this.Root, new RewriteTrace(steps));
        return new SimplifyResult<TContext>(
            new CompiledRule<TContext>(simplified, this.registry, this.logger, this.options),
            steps
        );
    }

    /// <summary>
    /// Rewrites this rule into negation normal form (NNF): <c>NOT</c> appears only directly above a term or an atom.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>NOT</c> is pushed to the terms by the Strong Kleene De Morgan laws, and a double negation is removed. Derived
    /// operators (<c>IMPLIES</c>, <c>XOR</c>, <c>EQUIVALENT</c>, <c>NAND</c>, <c>NOR</c>, <c>PARITY</c>, <c>ANY</c>,
    /// <c>ALL</c>, <c>NONE</c>, <c>ExactlyOne</c> and <c>BETWEEN</c>) expand first. <c>COALESCE</c>, the inspections
    /// and <c>If</c> are not information-monotone, so each is an atom: no <c>NOT</c> is pushed into it, although its
    /// operands are normalized. No classical law is used: <c>a AND NOT a</c> stays as written.
    /// </para>
    /// <para>
    /// A threshold (<c>AtLeast</c>, <c>AtMost</c>, <c>Exactly</c>, <c>GreaterThan</c>, <c>LessThan</c>) other than the cheap
    /// <c>OR</c>-like and <c>AND</c>-like cases stays an atom unless <see cref="NormalFormOptions.ExpandThresholds"/> is set,
    /// because its expansion has <c>C(n, k)</c> subsets. A kept threshold adds a
    /// <see cref="Diagnostics.DiagnosticCodes.ThresholdKeptAsAtom"/> warning with the growth estimate to the result.
    /// </para>
    /// <para>
    /// The result has the same value for every <c>True</c>/<c>False</c>/<c>Unknown</c> assignment, and rewriting it again
    /// changes nothing. It can be larger than this rule. The result is capped at
    /// <see cref="CompilerOptions.MaxRewriteNodeCount"/> nodes, counted as a printed tree; a larger result is not built
    /// and the call returns a <see cref="Diagnostics.DiagnosticCodes.RewriteTooLarge"/> error. A result over <see cref="CompilerOptions.MaxNodeCount"/> (512 by default) is a valid rule here, but its printed text compiles back only when you raise <c>MaxNodeCount</c>; that cap is independent of <c>MaxRewriteNodeCount</c>. Evaluation order is not preserved.
    /// </para>
    /// </remarks>
    /// <param name="normalForm">The form options, or <see langword="null"/> for <see cref="NormalFormOptions.Default"/>.</param>
    /// <param name="maxNodeCount">The most nodes, counted as a printed tree, the rewritten rule may have. It controls only that cap. <see langword="null"/> uses the <see cref="CompilerOptions.MaxRewriteNodeCount"/> this rule was compiled with; pass a larger value to allow bigger results.</param>
    /// <returns>The rewritten rule with any warnings, or no rule and a <c>TRE0016</c> error when the cap is exceeded.</returns>
    public CompilationResult<TContext> ToNnf(NormalFormOptions? normalForm = null, int? maxNodeCount = null)
    {
        return this.NormalFormResult(NormalForms.Form.Nnf, nameof(this.ToNnf), normalForm, maxNodeCount);
    }

    /// <summary>
    /// Rewrites this rule into conjunctive normal form (CNF): an <c>AND</c> of <c>OR</c>s of literals and atoms.
    /// </summary>
    /// <remarks>
    /// The rule goes to negation normal form first (see <see cref="ToNnf"/>), then <c>OR</c> is distributed over
    /// <c>AND</c>. Distribution holds in Strong Kleene logic because <c>AND</c> and <c>OR</c> form a distributive lattice;
    /// no classical complement law is used, so <c>a OR NOT a</c> stays. A repeated literal in a clause and a repeated clause
    /// are dropped (idempotence). Distribution can grow a rule exponentially, so the result is capped as for
    /// <see cref="ToNnf"/>. Atoms, thresholds, the options, the warning, the recompile caveat for a result over <see cref="CompilerOptions.MaxNodeCount"/> and the value guarantee are the same as for
    /// <see cref="ToNnf"/>, and rewriting the result again changes nothing.
    /// </remarks>
    /// <param name="normalForm">The form options, or <see langword="null"/> for <see cref="NormalFormOptions.Default"/>.</param>
    /// <param name="maxNodeCount">The most nodes, counted as a printed tree, the rewritten rule may have. It controls only that cap. <see langword="null"/> uses the <see cref="CompilerOptions.MaxRewriteNodeCount"/> this rule was compiled with; pass a larger value to allow bigger results.</param>
    /// <returns>The rewritten rule with any warnings, or no rule and a <c>TRE0016</c> error when the cap is exceeded.</returns>
    public CompilationResult<TContext> ToCnf(NormalFormOptions? normalForm = null, int? maxNodeCount = null)
    {
        return this.NormalFormResult(NormalForms.Form.Cnf, nameof(this.ToCnf), normalForm, maxNodeCount);
    }

    /// <summary>
    /// Rewrites this rule into disjunctive normal form (DNF): an <c>OR</c> of <c>AND</c>s of literals and atoms.
    /// </summary>
    /// <remarks>
    /// The mirror of <see cref="ToCnf"/>: the rule goes to negation normal form first, then <c>AND</c> is distributed over
    /// <c>OR</c>. Size cap, recompile caveat, atoms, thresholds, options, warning and value guarantee are the same as for
    /// <see cref="ToCnf"/>.
    /// </remarks>
    /// <param name="normalForm">The form options, or <see langword="null"/> for <see cref="NormalFormOptions.Default"/>.</param>
    /// <param name="maxNodeCount">The most nodes, counted as a printed tree, the rewritten rule may have. It controls only that cap. <see langword="null"/> uses the <see cref="CompilerOptions.MaxRewriteNodeCount"/> this rule was compiled with; pass a larger value to allow bigger results.</param>
    /// <returns>The rewritten rule with any warnings, or no rule and a <c>TRE0016</c> error when the cap is exceeded.</returns>
    public CompilationResult<TContext> ToDnf(NormalFormOptions? normalForm = null, int? maxNodeCount = null)
    {
        return this.NormalFormResult(NormalForms.Form.Dnf, nameof(this.ToDnf), normalForm, maxNodeCount);
    }

    /// <summary>Prints this rule to the flat, key-discriminated JSON tree shape (ADR-0003).</summary>
    /// <returns>The JSON text.</returns>
    public string PrintJson()
    {
        return JsonTreePrinter.Print(this.Root);
    }

    /// <summary>
    /// Builds this rule's outline: its expression tree, recursively — every operator's label/description (from
    /// <see cref="OperatorInfo"/>) and every term's label/description (from its predicate's registered
    /// <see cref="PredicateSchema"/>), without exposing the underlying closed-set AST types
    /// themselves. Useful for a rule-authoring UI or a generated "what does this rule mean" report.
    /// </summary>
    /// <returns>The root outline node, with every operand outlined the same way.</returns>
    public OutlineNode Outline()
    {
        return OutlineOf(this.Root, this.registry);
    }

    /// <summary>Renders this rule's structure as Mermaid <c>flowchart</c> text, for a diagram UI.</summary>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="direction">The layout direction. Defaults to <see cref="MermaidDirection.TopDown"/>.</param>
    /// <param name="nodeShapes">Whether each node role gets its own shape. Defaults to <see langword="false"/>.</param>
    /// <param name="twoLineTermLabels">Whether a term renders as a bold label and a plain argument line. Defaults to <see langword="false"/>.</param>
    /// <param name="palette">The state, highlight and mute colors. <see langword="null"/> (the default) means <see cref="MermaidPalette.Light"/>.</param>
    /// <param name="nodeStyle">A callback that picks a style for any node, or <see langword="null"/> (the default) for none.</param>
    /// <param name="compactChainThreshold">The operand count above which a flat <c>AND</c> or <c>OR</c> chain is boxed, or <see langword="null"/> (the default) for no compaction.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public string PrintMermaid(
        bool showArgumentValues = true,
        MermaidDirection direction = MermaidDirection.TopDown,
        bool nodeShapes = false,
        bool twoLineTermLabels = false,
        MermaidPalette? palette = null,
        Func<OutlineNode, NodeStyle?>? nodeStyle = null,
        int? compactChainThreshold = null
    )
    {
        return this.PrintMermaid(
            MermaidOptions.From(
                OperatorStyle.Word,
                showArgumentValues,
                direction,
                nodeShapes,
                twoLineTermLabels,
                palette,
                nodeStyle,
                compactChainThreshold
            )
        );
    }

    /// <summary>Renders this rule's structure as Mermaid <c>flowchart</c> text, using <paramref name="options"/>.</summary>
    /// <param name="options">The diagram direction, node shapes, operator style and argument-value switch.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    public string PrintMermaid(MermaidOptions options)
    {
        return MermaidTreePrinter.Print(this.Outline(), options);
    }

    /// <summary>
    /// Renders this rule's structure as Mermaid <c>flowchart</c> text, colored by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="direction">The layout direction. Defaults to <see cref="MermaidDirection.TopDown"/>.</param>
    /// <param name="nodeShapes">Whether each node role gets its own shape. Defaults to <see langword="false"/>.</param>
    /// <param name="twoLineTermLabels">Whether a term renders as a bold label and a plain argument line. Defaults to <see langword="false"/>.</param>
    /// <param name="palette">The state, highlight and mute colors. <see langword="null"/> (the default) means <see cref="MermaidPalette.Light"/>.</param>
    /// <param name="nodeStyle">A callback that picks a style for any node, or <see langword="null"/> (the default) for none.</param>
    /// <param name="compactChainThreshold">The operand count above which a flat <c>AND</c> or <c>OR</c> chain is boxed, or <see langword="null"/> (the default) for no compaction.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    public string PrintMermaid(
        Decision decision,
        bool showArgumentValues = true,
        MermaidDirection direction = MermaidDirection.TopDown,
        bool nodeShapes = false,
        bool twoLineTermLabels = false,
        MermaidPalette? palette = null,
        Func<OutlineNode, NodeStyle?>? nodeStyle = null,
        int? compactChainThreshold = null
    )
    {
        return this.PrintMermaid(
            decision,
            MermaidOptions.From(
                OperatorStyle.Word,
                showArgumentValues,
                direction,
                nodeShapes,
                twoLineTermLabels,
                palette,
                nodeStyle,
                compactChainThreshold
            )
        );
    }

    /// <summary>
    /// Renders this rule's structure as Mermaid <c>flowchart</c> text, colored by one evaluation, using <paramref name="options"/>.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="options">The diagram direction, node shapes, operator style and argument-value switch.</param>
    /// <returns>Mermaid <c>flowchart</c> text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    public string PrintMermaid(Decision decision, MermaidOptions options)
    {
        return MermaidTreePrinter.Print(this.Outline(), RequireTraceTree(decision), options);
    }

    /// <summary>Renders this rule's structure as an indented plain-text tree.</summary>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>The indented tree text.</returns>
    public string PrintPlainText(bool showArgumentValues = true)
    {
        return PlainTextTreePrinter.Print(this.Outline(), showArgumentValues: showArgumentValues);
    }

    /// <summary>
    /// Renders this rule's structure as an indented plain-text tree, annotated by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <returns>The indented tree text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    public string PrintPlainText(Decision decision, bool showArgumentValues = true)
    {
        return PlainTextTreePrinter.Print(this.Outline(), RequireTraceTree(decision), showArgumentValues: showArgumentValues);
    }

    /// <summary>
    /// Renders this rule as a flat, single-line infix equation, for example <c>a ∧ (b ∨ c)</c>. The connectives
    /// <c>NOT</c>, <c>AND</c>, <c>OR</c>, <c>XOR</c>, <c>EQUIVALENT</c>, <c>IMPLIES</c>, <c>NAND</c> and <c>NOR</c> print
    /// as infix symbols. Every other operator prints in function-call form, for example <c>AtLeast(2, a, b, c)</c>.
    /// Parentheses appear only around a connective that is an operand of another connective. This is a read view:
    /// <see cref="CanonicalText"/> and the persisted DSL text do not change.
    /// </summary>
    /// <param name="options">The dialect and term options, or <see langword="null"/> for <see cref="EquationOptions.Default"/>.</param>
    /// <returns>The equation text.</returns>
    public string PrintEquation(EquationOptions? options = null)
    {
        return EquationPrinter.Print(this.Root, options ?? EquationOptions.Default);
    }

    /// <summary>
    /// Renders this rule as an equation with a letter for each term, and returns the legend that maps each letter back to
    /// its term. The call always uses simple-variable mode, so <see cref="EquationOptions.SimpleVariables"/> has no
    /// effect here. The legend shows each term as its full call, whatever <see cref="EquationOptions.ShowArgumentValues"/>
    /// says. The lettering is stable: the same rule always gets the same letters.
    /// </summary>
    /// <param name="options">The dialect options, or <see langword="null"/> for <see cref="EquationOptions.Default"/>.</param>
    /// <returns>The equation and its legend, as data and as text.</returns>
    public EquationWithLegend PrintEquationWithLegend(EquationOptions? options = null)
    {
        return EquationPrinter.PrintWithLegend(this.Root, options ?? EquationOptions.Default);
    }

    /// <summary>
    /// Evaluates this rule against a context. Every argument after <paramref name="context"/> is optional, so a
    /// caller supplies only what the rule needs. A variable reference (<c>from("source", "query")</c>) resolves from the
    /// matching entry of <paramref name="dataSources"/> (ADR-0006). A reference whose source is not supplied,
    /// whose query matches nothing or too much, whose result does not fit the argument, or whose source fails makes
    /// its term <see cref="TruthValue.Unknown"/> and records a <see cref="Fault"/> carrying a
    /// <see cref="VariableResolutionException"/>; the fault never contains the resolved value.
    /// </summary>
    /// <param name="context">The application-supplied evaluation context.</param>
    /// <param name="services">
    /// The service provider to resolve class-based predicates from, fresh for this call — never
    /// captured once at registration, so scoped dependencies (a <c>DbContext</c>, a scoped
    /// <c>HttpClient</c>) resolve correctly even though this <see cref="CompiledRule{TContext}"/>
    /// outlives any one scope (ADR-0002). <see langword="null"/> means an empty provider: a class-based
    /// predicate then faults (<see cref="TruthValue.Unknown"/> plus a <see cref="Fault"/>) as for any missing registration.
    /// </param>
    /// <param name="dataSources">The named data sources for this evaluation, or <see langword="null"/> when the rule has no variables.</param>
    /// <param name="options">Per-call evaluation options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>The evaluation's <see cref="Decision"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> has a <see cref="EvaluationOptions.FaultBudget"/> below 1.</exception>
    /// <exception cref="OperationCanceledException">The caller's token, or <see cref="EvaluationOptions.Timeout"/>, cancelled the evaluation. Neither is recorded as a fault.</exception>
    public async Task<Decision> EvaluateAsync(
        TContext context,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        services ??= NoServiceProvider.Instance;
        EvaluationOptions effectiveOptions = options ?? EvaluationOptions.Default;

        // A budget below 1 could never be met, so it is a caller error, not a value to interpret.
        ArgumentOutOfRangeException.ThrowIfLessThan(effectiveOptions.FaultBudget ?? 1, 1);
        if (effectiveOptions.Timeout is { } timeout)
        {
            using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            Evaluator<TContext> timedEvaluator = new(
                context,
                services,
                this.registry,
                effectiveOptions,
                timeoutSource.Token,
                this.logger,
                dataSources
            );
            return await timedEvaluator.EvaluateAsync(this.Root).ConfigureAwait(false);
        }

        Evaluator<TContext> evaluator = new(
            context,
            services,
            this.registry,
            effectiveOptions,
            cancellationToken,
            this.logger,
            dataSources
        );
        return await evaluator.EvaluateAsync(this.Root).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.CanonicalText;
    }

    private static HashSet<string> CollectPredicateNames(Expression root)
    {
        HashSet<string> names = [with(StringComparer.Ordinal)];
        Stack<Expression> pending = new([root]);
        while (pending.TryPop(out Expression? node))
        {
            if (node is TermExpression term)
            {
                names.Add(term.Identity.PredicateName);
            }
            else if (node is not ConstantExpression)
            {
                foreach (Expression operand in ExpressionShape.Of(node).Operands)
                {
                    pending.Push(operand);
                }
            }
        }

        return names;
    }

    private static OutlineNode OutlineOf(Expression node, PredicateRegistry<TContext> registry)
    {
        if (node is TermExpression term)
        {
            (string label, string description) = registry.TryGetSchema(term.Identity.PredicateName, out PredicateSchema? schema)
                ? (schema.Label, schema.Description)
                : (term.Identity.PredicateName, "An unregistered predicate (CompilationMode.Lenient).");
            return new OutlineNode(label, description, [], ArgumentText(term.Identity), OutlineNodeKind.Term);
        }

        OperatorDescriptor descriptor = OperatorInfo.Describe(node);
        IReadOnlyList<Expression> operands = (node is ConstantExpression) ? [] : ExpressionShape.Of(node).Operands;

        return new OutlineNode(
            descriptor.Label,
            descriptor.Description,
            [.. operands.Select(operand => OutlineOf(operand, registry))],
            Kind: node is ConstantExpression ? OutlineNodeKind.Constant : OutlineNodeKind.Operator
        );
    }

    /// <summary>
    /// Renders a term's rule-text arguments as comma-joined <c>name: value</c> pairs, matching the
    /// per-argument formatting <see cref="TermIdentity.ToString"/> uses for its parenthesized part, but
    /// without repeating the predicate name — that comes from the term's own
    /// <see cref="OutlineNode.Label"/> instead.
    /// </summary>
    /// <param name="identity">The term's identity.</param>
    /// <returns>The joined argument text, or <see langword="null"/> for a zero-argument term.</returns>
    private static string? ArgumentText(TermIdentity identity)
    {
        return identity.FormatArguments();
    }

    private static TraceNode RequireTraceTree(Decision decision)
    {
        return decision.TraceTree
            ?? throw new ArgumentException(
                "This decision has no TraceTree to render — it must come from EvaluateAsync on this same rule.",
                nameof(decision)
            );
    }

    /// <summary>Runs a normal-form rewrite and wraps its tree and its kept-threshold warnings as a result.</summary>
    private CompilationResult<TContext> NormalFormResult(
        NormalForms.Form form,
        string rewrite,
        NormalFormOptions? normalForm,
        int? maxNodeCount
    )
    {
        int cap = maxNodeCount ?? this.options.MaxRewriteNodeCount;
        NormalForms.Result built = NormalForms.Build(
            this.Root,
            form,
            (normalForm ?? NormalFormOptions.Default).ExpandThresholds,
            cap
        );
        CompilationResult<TContext> result = this.RewriteResult(built.Tree, rewrite, cap);
        if (!result.Succeeded)
        {
            return result;
        }

        List<Diagnostic> warnings = [];
        foreach (NormalForms.KeptThreshold kept in built.Kept)
        {
            string text = CanonicalPrinter.Print(kept.Threshold);
            warnings.Add(
                Diagnostic.Warning(
                    DiagnosticCodes.ThresholdKeptAsAtom,
                    string.Create(
                        System.Globalization.CultureInfo.InvariantCulture,
                        $"{rewrite} kept {text} as an atom. Expanding it would add about {kept.EstimatedNodes:N0} nodes."
                    ),
                    SourceSpan.None,
                    suggestion: new DiagnosticSuggestion(
                        DiagnosticSuggestionKind.Hint,
                        "Pass NormalFormOptions with ExpandThresholds set to true to expand it."
                    )
                )
            );
        }

        return result with
        {
            Diagnostics = warnings,
        };
    }

    /// <summary>
    /// Wraps a rewrite's output as a new rule, or as the <see cref="Diagnostics.DiagnosticCodes.RewriteTooLarge"/> failure
    /// when the rewrite refused (<paramref name="tree"/> is <see langword="null"/>) or its tree is over the cap.
    /// </summary>
    private CompilationResult<TContext> RewriteResult(Expression? tree, string rewrite, int cap)
    {
        if (tree is not null && ExpressionTools.Size(tree) <= cap)
        {
            return new CompilationResult<TContext>(
                new CompiledRule<TContext>(tree, this.registry, this.logger, this.options),
                []
            );
        }

        Diagnostic diagnostic = Diagnostic.Error(
            DiagnosticCodes.RewriteTooLarge,
            $"{rewrite} would produce more than the maximum of {cap} nodes.",
            SourceSpan.None,
            expected: $"at most {cap} nodes",
            found: "more nodes than that",
            suggestion: new DiagnosticSuggestion(
                DiagnosticSuggestionKind.Hint,
                "Raise the maxNodeCount argument or CompilerOptions.MaxRewriteNodeCount, or simplify the rule first."
            )
        );
        return new CompilationResult<TContext>(null, [diagnostic]);
    }
}
