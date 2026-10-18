namespace TruthWeaver.Evaluation;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Json;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Rewriting;

public sealed partial class CompiledRule<TContext>
{
    /// <summary>
    /// Rewrites every derived operator into the primitive kernel: <c>NOT</c>, <c>AND</c>, <c>OR</c>, <c>AtLeast</c>,
    /// <c>AtMost</c>, <c>Exactly</c> and <c>COALESCE</c>, and returns the result as a new rule (ADR-0005 decision 10). The
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
    /// <returns>
    /// A result that holds the new rule over the same predicates, with derived operators where patterns matched. It carries
    /// a <see cref="Diagnostics.DiagnosticCodes.RewriteNotConverged"/> warning when the rewrite reached its pass limit
    /// before the rule stopped changing; the rule is then still equivalent, but compressing it again can change it.
    /// </returns>
    public CompilationResult<TContext> CompressToDerived()
    {
        return this.CompressToDerivedWithPassLimit(Compressor.MaxPasses);
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
    /// <returns>
    /// A result that holds the new rule over the same predicates in canonical form. It carries a
    /// <see cref="Diagnostics.DiagnosticCodes.RewriteNotConverged"/> warning when the rewrite reached its pass limit before
    /// the rule stopped changing; the rule is then still equivalent, but canonicalising it again can change it.
    /// </returns>
    public CompilationResult<TContext> Canonicalize()
    {
        return this.CanonicalizeWithPassLimit(Canonicalizer.MaxPasses);
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
    /// <returns>
    /// A result that holds the new rule over the same predicates, simplified. It carries a
    /// <see cref="Diagnostics.DiagnosticCodes.RewriteNotConverged"/> warning when the rewrite reached its pass limit before
    /// the rule stopped changing; the rule is then still equivalent, but simplifying it again can change it.
    /// </returns>
    public CompilationResult<TContext> Simplify()
    {
        return this.SimplifyWithPassLimit(Simplifier.MaxPasses);
    }

    /// <summary>
    /// Replaces this rule with an equivalent, same-size one whose <c>AND</c>/<c>OR</c> operands are reordered cheapest
    /// first, and returns it as a new rule. Lets a rule author rely on the evaluator's left-to-right short-circuit
    /// (<see cref="EvaluationMode.ShortCircuit"/>) to skip an expensive predicate once a cheap one already decides the
    /// <c>AND</c> or <c>OR</c> it is part of.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every <c>AND</c> and <c>OR</c> node's operands are stable-sorted by subtree cost, cheapest first: the cost of an
    /// operand is the sum of <see cref="Abstractions.PredicateSchema.Cost"/> over the distinct terms it reaches - a term
    /// reached through two paths inside the same operand counts once, matching the evaluator's per-evaluation term
    /// memoization (ADR-0002). A predicate with no <see cref="Abstractions.PredicateSchema.Cost"/> hint sits at the
    /// neutral baseline of <c>0</c>; a rule whose predicates all leave it unset sorts every tie in original order
    /// (<see cref="Enumerable.OrderBy{TSource,TKey}(IEnumerable{TSource},Func{TSource,TKey})"/> is a stable sort), so this
    /// method changes nothing for it. <c>COALESCE</c>'s priority order, <c>IMPLIES</c>'s antecedent-first evaluation and
    /// <c>If</c>'s condition-first evaluation all carry meaning beyond cost, so none of their operands is reordered.
    /// </para>
    /// <para>
    /// The result evaluates to the same value as this rule for every <c>True</c>/<c>False</c>/<c>Unknown</c> assignment
    /// and has exactly the same node count. <b>Evaluation order and side effects are not preserved.</b> A predicate the
    /// original would have invoked, and any fault it would have reported, may no longer run (or may run earlier) once a
    /// short-circuit applies in the new order; the value never changes. This is the same trade <see cref="Simplify"/>
    /// already makes for operands it drops entirely.
    /// </para>
    /// <para>
    /// Same inputs (this rule's tree, the registry's <c>Cost</c> hints and <paramref name="options"/>) give the same
    /// order on every call - there is no randomness and no hidden state. Passing <see cref="OrderOptimizationOptions.MeasuredCosts"/>
    /// is the only way to change the order for a fixed rule and registry.
    /// </para>
    /// </remarks>
    /// <param name="options">
    /// The measured-cost override, or <see langword="null"/> (the default) to use only the registry's static
    /// <see cref="Abstractions.PredicateSchema.Cost"/> hints.
    /// </param>
    /// <returns>A result that holds the new rule over the same predicates, with its <c>AND</c>/<c>OR</c> operands reordered.</returns>
    public CompilationResult<TContext> OptimizeOrder(OrderOptimizationOptions? options = null)
    {
        Dictionary<string, double>? measuredCosts = (options ?? OrderOptimizationOptions.Default).MeasuredCosts is { } measured
            ? new Dictionary<string, double>(measured, StringComparer.OrdinalIgnoreCase)
            : null;

        double CostOf(TermIdentity identity)
        {
            if (measuredCosts is not null && measuredCosts.TryGetValue(identity.PredicateName, out double measuredCost))
            {
                return measuredCost;
            }

            return this.registry.TryGetSchema(identity.PredicateName, out PredicateSchema? schema) ? schema.Cost ?? 0.0 : 0.0;
        }

        Expression optimized = OrderOptimizer.Optimize(this.Root, CostOf);
        return new CompilationResult<TContext>(
            new CompiledRule<TContext>(optimized, this.registry, this.logger, this.options),
            EquatableArray<Diagnostic>.Empty
        );
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
    /// <returns>The same rule as <see cref="Simplify"/>, the list of steps, and the same non-convergence warning.</returns>
    public SimplifyResult<TContext> SimplifyWithSteps()
    {
        List<RewriteStep> steps = [];
        RewriteOutcome outcome = Simplifier.Simplify(this.Root, new RewriteTrace(steps));
        CompilationResult<TContext> result = this.ConvergedResult(outcome, nameof(this.Simplify), Simplifier.MaxPasses);
        return new SimplifyResult<TContext>(result.Rule!, steps, result.Diagnostics);
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

    /// <summary>The <c>Try</c> form of <see cref="ExpandToPrimitives"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <param name="maxNodeCount">The node cap, as for <see cref="ExpandToPrimitives"/>.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryExpandToPrimitives(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics,
        int? maxNodeCount = null
    )
    {
        return this.ExpandToPrimitives(maxNodeCount).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="ExpandToNand"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <param name="maxNodeCount">The node cap, as for <see cref="ExpandToNand"/>.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryExpandToNand(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics,
        int? maxNodeCount = null
    )
    {
        return this.ExpandToNand(maxNodeCount).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="ExpandToNor"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <param name="maxNodeCount">The node cap, as for <see cref="ExpandToNor"/>.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryExpandToNor(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics,
        int? maxNodeCount = null
    )
    {
        return this.ExpandToNor(maxNodeCount).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="CompressToDerived"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including a non-convergence warning on success.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryCompressToDerived(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics
    )
    {
        return this.CompressToDerived().TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="Canonicalize"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including a non-convergence warning on success.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryCanonicalize(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics
    )
    {
        return this.Canonicalize().TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="Simplify"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including a non-convergence warning on success.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TrySimplify([NotNullWhen(true)] out CompiledRule<TContext>? rule, out EquatableArray<Diagnostic> diagnostics)
    {
        return this.Simplify().TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="OptimizeOrder"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised by this call.</param>
    /// <param name="options">The measured-cost override, as for <see cref="OptimizeOrder"/>.</param>
    /// <returns><see langword="true"/> when a rule was produced; otherwise <see langword="false"/>.</returns>
    public bool TryOptimizeOrder(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics,
        OrderOptimizationOptions? options = null
    )
    {
        return this.OptimizeOrder(options).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="ToNnf"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <param name="normalForm">The form options, or <see langword="null"/> for <see cref="NormalFormOptions.Default"/>.</param>
    /// <param name="maxNodeCount">The node cap, as for <see cref="ToNnf"/>.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryToNnf(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics,
        NormalFormOptions? normalForm = null,
        int? maxNodeCount = null
    )
    {
        return this.ToNnf(normalForm, maxNodeCount).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="ToCnf"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <param name="normalForm">The form options, or <see langword="null"/> for <see cref="NormalFormOptions.Default"/>.</param>
    /// <param name="maxNodeCount">The node cap, as for <see cref="ToCnf"/>.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryToCnf(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics,
        NormalFormOptions? normalForm = null,
        int? maxNodeCount = null
    )
    {
        return this.ToCnf(normalForm, maxNodeCount).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>The <c>Try</c> form of <see cref="ToDnf"/>.</summary>
    /// <param name="rule">The rewritten rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <param name="normalForm">The form options, or <see langword="null"/> for <see cref="NormalFormOptions.Default"/>.</param>
    /// <param name="maxNodeCount">The node cap, as for <see cref="ToDnf"/>.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryToDnf(
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics,
        NormalFormOptions? normalForm = null,
        int? maxNodeCount = null
    )
    {
        return this.ToDnf(normalForm, maxNodeCount).TryGetRule(out rule, out diagnostics);
    }

    /// <summary>Compresses with a given pass limit, so a test can force non-convergence.</summary>
    /// <param name="maxPasses">The most passes to run.</param>
    /// <returns>The compression result.</returns>
    internal CompilationResult<TContext> CompressToDerivedWithPassLimit(int maxPasses)
    {
        return this.ConvergedResult(
            Compressor.CompressChecked(this.Root, maxPasses),
            nameof(this.CompressToDerived),
            maxPasses
        );
    }

    /// <summary>Canonicalises with a given pass limit, so a test can force non-convergence.</summary>
    /// <param name="maxPasses">The most passes to run.</param>
    /// <returns>The canonicalisation result.</returns>
    internal CompilationResult<TContext> CanonicalizeWithPassLimit(int maxPasses)
    {
        return this.ConvergedResult(
            Canonicalizer.CanonicalizeChecked(this.Root, maxPasses: maxPasses),
            nameof(this.Canonicalize),
            maxPasses
        );
    }

    /// <summary>Simplifies with a given pass limit, so a test can force non-convergence.</summary>
    /// <param name="maxPasses">The most passes of the simplification loop.</param>
    /// <returns>The simplification result.</returns>
    internal CompilationResult<TContext> SimplifyWithPassLimit(int maxPasses)
    {
        return this.ConvergedResult(Simplifier.Simplify(this.Root, maxPasses: maxPasses), nameof(this.Simplify), maxPasses);
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
                        CultureInfo.InvariantCulture,
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
            Diagnostics = new EquatableArray<Diagnostic>(warnings),
        };
    }

    /// <summary>
    /// Wraps the output of a repeat-until-stable rewrite as a new rule. A rewrite that stopped at its pass limit adds the
    /// <see cref="Diagnostics.DiagnosticCodes.RewriteNotConverged"/> warning; the rule is still equivalent.
    /// </summary>
    private CompilationResult<TContext> ConvergedResult(RewriteOutcome outcome, string rewrite, int maxPasses)
    {
        CompiledRule<TContext> rule = new(outcome.Tree, this.registry, this.logger, this.options);
        if (outcome.Converged)
        {
            return new CompilationResult<TContext>(rule, EquatableArray<Diagnostic>.Empty);
        }

        Diagnostic warning = Diagnostic.Warning(
            DiagnosticCodes.RewriteNotConverged,
            string.Create(
                CultureInfo.InvariantCulture,
                $"{rewrite} stopped after {maxPasses} passes before the rule stopped changing, so rewriting the result again can still change it."
            ),
            SourceSpan.None,
            suggestion: new DiagnosticSuggestion(
                DiagnosticSuggestionKind.Hint,
                $"Call {rewrite} on the result again until it no longer changes."
            )
        );
        return new CompilationResult<TContext>(rule, new EquatableArray<Diagnostic>([warning]));
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
                EquatableArray<Diagnostic>.Empty
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
        return new CompilationResult<TContext>(null, new EquatableArray<Diagnostic>([diagnostic]));
    }
}
