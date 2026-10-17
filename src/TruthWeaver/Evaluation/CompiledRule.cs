namespace TruthWeaver.Evaluation;

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
/// reference holding the active instance. No lock is needed (ADR-0002).
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
    private readonly Lazy<NoteLookup?> noteLookup;

    /// <summary>Initializes a new instance of the <see cref="CompiledRule{TContext}"/> class.</summary>
    /// <param name="root">The compiled tree.</param>
    /// <param name="registry">The registry the tree's terms were resolved against.</param>
    /// <param name="logger">The logger for evaluation, or <see langword="null"/> to log nowhere.</param>
    /// <param name="options">The options the tree was compiled with, or <see langword="null"/> for the defaults.</param>
    /// <param name="notes">
    /// The notes by tree path, or <see langword="null"/> for none. Only the compiler passes notes: a rewrite builds a new
    /// tree whose paths no longer name the nodes the notes were written on, so its result has none.
    /// </param>
    internal CompiledRule(
        Expression root,
        PredicateRegistry<TContext> registry,
        ILogger? logger = null,
        CompilerOptions? options = null,
        IReadOnlyDictionary<string, string>? notes = null
    )
    {
        this.options = options ?? CompilerOptions.Default;
        this.Root = root;
        this.registry = registry;
        this.logger = logger ?? NullLogger.Instance;
        this.Notes = notes is { Count: > 0 } ? notes.ToFrozenDictionary(StringComparer.Ordinal) : [];
        this.canonicalText = new Lazy<string>(() => CanonicalPrinter.Print(this.Root));
        this.predicateNames = new Lazy<IReadOnlySet<string>>(() => CollectPredicateNames(this.Root));
        this.metrics = new Lazy<RuleMetrics>(() => RuleMetrics.Measure(this.Root, this.options.MaxAnalysisTerms));
        this.noteLookup = new Lazy<NoteLookup?>(() => NoteLookup.Create(this.Root, this.Notes));
    }

    /// <summary>Gets this rule's canonical printed DSL text, the form <c>RuleCompiler.Compile</c> reproduces a structurally equal tree from.</summary>
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
    /// Gets the notes written on this rule's nodes, keyed by each node's tree path: <c>$</c> for the root,
    /// <c>$.operands[1]</c> for its second operand, and so on, the same paths JSON and YAML diagnostics use. A note is a
    /// <c>//</c> or <c>/* */</c> comment in rule text, a <c>note</c> property in JSON or YAML, or
    /// <c>RuleBuilder.WithNote</c>. The dictionary is empty for a rule with no notes.
    /// </summary>
    /// <remarks>
    /// Notes sit beside the tree, not in it: a rule with notes compiles to the same tree, the same
    /// <see cref="CanonicalText"/> and the same diagnostics as the rule without them, and evaluates the same way.
    /// <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/>, <see cref="PrintJson()"/> and
    /// <c>PrintYaml</c> write them by default, so a conversion between the formats keeps them; <see cref="CanonicalText"/>
    /// never does. A rewrite (<see cref="Simplify"/>, <see cref="Canonicalize"/>, <see cref="ExpandToPrimitives"/>, the
    /// normal forms, <see cref="OptimizeOrder"/> and the rest) returns a rule with no notes, because the rewritten tree's
    /// paths no longer name the nodes the notes were written on.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Notes { get; }

    /// <summary>
    /// Gets the underlying expression tree. Internal: visible to <c>TruthWeaver.Yaml</c> via
    /// <c>InternalsVisibleTo</c>, so its YAML printer can render the same tree <see cref="CanonicalText"/>
    /// and <see cref="PrintJson()"/> render, without this package needing to know YAML exists.
    /// </summary>
    internal Expression Root { get; }

    /// <summary>
    /// Gets the notes looked up by node for the printers, or <see langword="null"/> when this rule has none. Internal and
    /// visible to <c>TruthWeaver.Yaml</c>, whose printer writes the notes the same way the JSON printer does.
    /// </summary>
    internal NoteLookup? NoteLookup => this.noteLookup.Value;

    /// <summary>
    /// Prints this rule as DSL text with the chosen grouping delimiters. <see cref="GroupingStyle.Parentheses"/> returns
    /// exactly <see cref="CanonicalText"/>; <see cref="GroupingStyle.DepthCycling"/> varies the delimiter by nesting depth
    /// for readability. Every style re-parses to a tree equal to this rule's, because the DSL treats <c>()</c>, <c>[]</c>
    /// and <c>{}</c> as the same grouping.
    /// </summary>
    /// <param name="grouping">The grouping delimiters to print with. Defaults to <see cref="GroupingStyle.Parentheses"/>.</param>
    /// <param name="operatorStyle">
    /// How to spell <c>AND</c>/<c>OR</c>/<c>NOT</c>/<c>XOR</c>/<c>EQUIVALENT</c>/<c>IMPLIES</c>/<c>NAND</c>/<c>NOR</c>.
    /// <see cref="OperatorStyle.Word"/> (the default, and the only form <see cref="CanonicalText"/> ever uses) writes
    /// the word form. <see cref="OperatorStyle.Symbolic"/> writes the mathematical symbols the DSL parser itself
    /// reads (<c>∧ ∨ ¬ ⊕ ↔ → ↑ ↓</c>), so the result still re-parses to a tree equal to this one.
    /// <see cref="OperatorStyle.CStyle"/> is rejected: the parser does not read <c>^</c> or <c>==</c>, so there is no
    /// round-trippable C-style rule text. <c>COALESCE</c> and <c>If</c> always keep their word/function-call spelling
    /// here, in every style, because the DSL reads <c>??</c> only infix and has no call syntax for it. Call
    /// <see cref="TryPrintRuleText(GroupingStyle, OperatorStyle, out string?)"/> to avoid the exception.
    /// </param>
    /// <param name="constants">
    /// How to spell <c>True</c>, <c>False</c> and <c>Unknown</c>, or <see langword="null"/> (the default) for the word form.
    /// Only a format that the parser reads back is accepted: <see cref="ConstantStyle.Word"/> or
    /// <see cref="ConstantStyle.Letters"/> (<c>T</c>, <c>F</c>, <c>U</c>), with no override, or with an override that is a
    /// letter-case variant of the constant's own word or letter (for example <c>TRUE</c> or <c>t</c>).
    /// <see cref="ConstantStyle.LogicSymbols"/> is rejected, because the parser does not read <c>⊤</c> or <c>⊥</c> at all.
    /// </param>
    /// <returns>The DSL text.</returns>
    /// <exception cref="ArgumentException"><paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>, or <paramref name="constants"/> is a format that the parser does not read back.</exception>
    public string PrintRuleText(
        GroupingStyle grouping = GroupingStyle.Parentheses,
        OperatorStyle operatorStyle = OperatorStyle.Word,
        ConstantFormat? constants = null
    )
    {
        if (operatorStyle == OperatorStyle.CStyle)
        {
            throw new ArgumentException(
                "OperatorStyle.CStyle has no round-trippable rule text: the DSL parser does not read '^' or '=='. "
                    + "Use Word or Symbolic, or call TryPrintRuleText.",
                nameof(operatorStyle)
            );
        }

        if (constants is not null && !constants.IsRuleTextSafe(out string? reason))
        {
            throw new ArgumentException($"{reason} Call TryPrintRuleText to avoid the exception.", nameof(constants));
        }

        return
            grouping == GroupingStyle.Parentheses
            && operatorStyle == OperatorStyle.Word
            && constants is null
            && this.NoteLookup is null
            ? this.CanonicalText
            : CanonicalPrinter.Print(this.Root, grouping, operatorStyle, constants, this.NoteLookup);
    }

    /// <summary>
    /// Writes this rule as DSL text to <paramref name="writer"/>. The output is exactly the text that
    /// <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/> returns, without a large string
    /// that the caller must hold.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="grouping">The grouping delimiters to print with. Defaults to <see cref="GroupingStyle.Parentheses"/>.</param>
    /// <param name="operatorStyle">How to spell the connectives, as for the string overload. Defaults to <see cref="OperatorStyle.Word"/>.</param>
    /// <param name="constants">How to spell the constants, as for the string overload, or <see langword="null"/> for the word form.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>, or <paramref name="constants"/> is a format that the parser does not read back. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintRuleText(
        TextWriter writer,
        GroupingStyle grouping = GroupingStyle.Parentheses,
        OperatorStyle operatorStyle = OperatorStyle.Word,
        ConstantFormat? constants = null
    )
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintRuleText(grouping, operatorStyle, constants));
    }

    /// <summary>
    /// Renders this rule as DSL text using <paramref name="options"/>. This is the same text as
    /// <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/> for an equivalent grouping, operator
    /// style and constants, with one addition: when <see cref="RuleTextOptions.Width"/> is set, a group that does not
    /// fit the width breaks across lines, indented by <see cref="RuleTextOptions.IndentSize"/> per nesting level. The
    /// breaking rule is documented in <c>docs/rule-text.md</c>. The result still compiles to a tree equal to
    /// <see cref="Root"/>.
    /// </summary>
    /// <param name="options">The printing options.</param>
    /// <returns>The DSL text, one line when <see cref="RuleTextOptions.Width"/> is <see langword="null"/>, otherwise possibly many.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/>'s <see cref="RuleTextOptions.OperatorStyle"/> is <see cref="OperatorStyle.CStyle"/>, or its <see cref="RuleTextOptions.Constants"/> is a format the parser does not read back.</exception>
    public string PrintRuleText(RuleTextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.OperatorStyle == OperatorStyle.CStyle)
        {
            throw new ArgumentException(
                "OperatorStyle.CStyle has no round-trippable rule text: the DSL parser does not read '^' or '=='. "
                    + "Use Word or Symbolic, or call TryPrintRuleText.",
                nameof(options)
            );
        }

        if (options.Constants is not null && !options.Constants.IsRuleTextSafe(out string? reason))
        {
            throw new ArgumentException($"{reason} Call TryPrintRuleText to avoid the exception.", nameof(options));
        }

        NoteLookup? notes = options.IncludeNotes ? this.NoteLookup : null;
        return
            options.Width is null
            && options.Grouping == GroupingStyle.Parentheses
            && options.OperatorStyle == OperatorStyle.Word
            && options.Constants is null
            && notes is null
            ? this.CanonicalText
            : CanonicalPrinter.Print(this.Root, options, notes);
    }

    /// <summary>
    /// Writes this rule as DSL text to <paramref name="writer"/> using <paramref name="options"/>. The output is exactly the
    /// text that <see cref="PrintRuleText(RuleTextOptions)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The printing options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> has an operator style or constants that the parser does not read back. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintRuleText(TextWriter writer, RuleTextOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintRuleText(options));
    }

    /// <summary>
    /// Renders this rule as DSL text, like <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/>,
    /// but reports an unsupported <paramref name="operatorStyle"/> through the return value instead of throwing.
    /// </summary>
    /// <param name="grouping">The grouping delimiters to print with.</param>
    /// <param name="operatorStyle">The operator style. <see cref="OperatorStyle.CStyle"/> is the only value this rejects.</param>
    /// <param name="text">The DSL text, or <see langword="null"/> when <paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>.</param>
    /// <returns><see langword="true"/> unless <paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/>.</returns>
    public bool TryPrintRuleText(GroupingStyle grouping, OperatorStyle operatorStyle, [NotNullWhen(true)] out string? text)
    {
        return this.TryPrintRuleText(grouping, operatorStyle, constants: null, out text);
    }

    /// <summary>
    /// Renders this rule as DSL text, like <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/>,
    /// but reports an unsupported <paramref name="operatorStyle"/> or <paramref name="constants"/> through the return
    /// value instead of throwing.
    /// </summary>
    /// <param name="grouping">The grouping delimiters to print with.</param>
    /// <param name="operatorStyle">The operator style. <see cref="OperatorStyle.CStyle"/> is rejected.</param>
    /// <param name="constants">
    /// The constant format, or <see langword="null"/> for the word form. A format that the parser does not read back
    /// is rejected (see <see cref="PrintRuleText(GroupingStyle, OperatorStyle, ConstantFormat?)"/>).
    /// </param>
    /// <param name="text">The DSL text, or <see langword="null"/> when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> unless <paramref name="operatorStyle"/> is <see cref="OperatorStyle.CStyle"/> or <paramref name="constants"/> is not read back.</returns>
    public bool TryPrintRuleText(
        GroupingStyle grouping,
        OperatorStyle operatorStyle,
        ConstantFormat? constants,
        [NotNullWhen(true)] out string? text
    )
    {
        if (operatorStyle == OperatorStyle.CStyle || constants?.IsRuleTextSafe(out _) == false)
        {
            text = null;
            return false;
        }

        text = this.PrintRuleText(grouping, operatorStyle, constants);
        return true;
    }

    /// <summary>
    /// Renders this rule as DSL text, like <see cref="PrintRuleText(RuleTextOptions)"/>, but reports an unsupported
    /// <see cref="RuleTextOptions.OperatorStyle"/> or <see cref="RuleTextOptions.Constants"/> through the return value
    /// instead of throwing.
    /// </summary>
    /// <param name="options">The printing options. Rejected the same way as <see cref="PrintRuleText(RuleTextOptions)"/>.</param>
    /// <param name="text">The DSL text, or <see langword="null"/> when this method returns <see langword="false"/>.</param>
    /// <returns><see langword="true"/> unless <paramref name="options"/> is rejected.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public bool TryPrintRuleText(RuleTextOptions options, [NotNullWhen(true)] out string? text)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.OperatorStyle == OperatorStyle.CStyle || options.Constants?.IsRuleTextSafe(out _) == false)
        {
            text = null;
            return false;
        }

        text = this.PrintRuleText(options);
        return true;
    }

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

    /// <summary>
    /// Prints this rule to the flat, key-discriminated JSON tree shape (ADR-0003), with each of its <see cref="Notes"/> as
    /// a <c>note</c> property on its node, so the JSON compiles back to the same tree and the same notes.
    /// </summary>
    /// <returns>The JSON text.</returns>
    public string PrintJson()
    {
        return this.PrintJson(includeNotes: true);
    }

    /// <summary>Prints this rule to the flat, key-discriminated JSON tree shape (ADR-0003), with or without its notes.</summary>
    /// <param name="includeNotes">
    /// <see langword="true"/> to write each of <see cref="Notes"/> as a <c>note</c> property (the lossless form that
    /// <see cref="PrintJson()"/> gives); <see langword="false"/> for the tree alone, for example to compare two rules.
    /// </param>
    /// <returns>The JSON text.</returns>
    public string PrintJson(bool includeNotes)
    {
        return JsonTreePrinter.Print(this.Root, includeNotes ? this.NoteLookup : null);
    }

    /// <summary>Writes this rule to <paramref name="writer"/> as the JSON that <see cref="PrintJson()"/> returns.</summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintJson(TextWriter writer)
    {
        this.PrintJson(writer, includeNotes: true);
    }

    /// <summary>Writes this rule to <paramref name="writer"/> as the JSON that <see cref="PrintJson(bool)"/> returns.</summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="includeNotes"><see langword="true"/> to write the notes, as for <see cref="PrintJson(bool)"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintJson(TextWriter writer, bool includeNotes)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintJson(includeNotes));
    }

    /// <summary>
    /// Builds this rule's outline: its expression tree, recursively. Every operator's label/description (from
    /// <see cref="OperatorInfo"/>) and every term's label/description (from its predicate's registered
    /// <see cref="PredicateSchema"/>), without exposing the underlying closed-set AST types
    /// themselves. Useful for a rule-authoring UI or a generated "what does this rule mean" report.
    /// </summary>
    /// <returns>The root outline node, with every operand outlined the same way.</returns>
    public OutlineNode Outline()
    {
        return OutlineOf(this.Root, this.registry);
    }

    /// <summary>
    /// Renders this rule's structure as Mermaid <c>flowchart</c> text, for a diagram UI. This overload always renders
    /// operator labels in <see cref="OperatorStyle.Word"/>. To choose another style, pass a <see cref="MermaidOptions"/>.
    /// </summary>
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
    /// Writes this rule's Mermaid <c>flowchart</c> text to <paramref name="writer"/>, with the operator labels in
    /// <see cref="OperatorStyle.Word"/>. The output is exactly the text that
    /// <see cref="PrintMermaid(bool, MermaidDirection, bool, bool, MermaidPalette?, Func{OutlineNode, NodeStyle?}?, int?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="direction">The layout direction. Defaults to <see cref="MermaidDirection.TopDown"/>.</param>
    /// <param name="nodeShapes">Whether each node role gets its own shape. Defaults to <see langword="false"/>.</param>
    /// <param name="twoLineTermLabels">Whether a term renders as a bold label and a plain argument line. Defaults to <see langword="false"/>.</param>
    /// <param name="palette">The state, highlight and mute colors, or <see langword="null"/> for <see cref="MermaidPalette.Light"/>.</param>
    /// <param name="nodeStyle">A callback that picks a style for any node, or <see langword="null"/> for none.</param>
    /// <param name="compactChainThreshold">The operand count above which a flat <c>AND</c> or <c>OR</c> chain is boxed, or <see langword="null"/> for no compaction.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintMermaid(
        TextWriter writer,
        bool showArgumentValues = true,
        MermaidDirection direction = MermaidDirection.TopDown,
        bool nodeShapes = false,
        bool twoLineTermLabels = false,
        MermaidPalette? palette = null,
        Func<OutlineNode, NodeStyle?>? nodeStyle = null,
        int? compactChainThreshold = null
    )
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(
            this.PrintMermaid(
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
    /// Writes this rule's Mermaid <c>flowchart</c> text to <paramref name="writer"/> using <paramref name="options"/>. The
    /// output is exactly the text that <see cref="PrintMermaid(MermaidOptions)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The diagram options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintMermaid(TextWriter writer, MermaidOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintMermaid(options));
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
    /// <remarks>This overload always renders operator labels in <see cref="OperatorStyle.Word"/>. To choose another style, pass a <see cref="MermaidOptions"/>.</remarks>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
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
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintMermaid(Decision decision, MermaidOptions options)
    {
        return MermaidTreePrinter.Print(this.Outline(), RequireTraceTree(decision), options);
    }

    /// <summary>
    /// Writes this rule's Mermaid <c>flowchart</c> text, colored by one evaluation, to <paramref name="writer"/>, with the
    /// operator labels in <see cref="OperatorStyle.Word"/>. The output is exactly the text that
    /// <see cref="PrintMermaid(Decision, bool, MermaidDirection, bool, bool, MermaidPalette?, Func{OutlineNode, NodeStyle?}?, int?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="direction">The layout direction. Defaults to <see cref="MermaidDirection.TopDown"/>.</param>
    /// <param name="nodeShapes">Whether each node role gets its own shape. Defaults to <see langword="false"/>.</param>
    /// <param name="twoLineTermLabels">Whether a term renders as a bold label and a plain argument line. Defaults to <see langword="false"/>.</param>
    /// <param name="palette">The state, highlight and mute colors, or <see langword="null"/> for <see cref="MermaidPalette.Light"/>.</param>
    /// <param name="nodeStyle">A callback that picks a style for any node, or <see langword="null"/> for none.</param>
    /// <param name="compactChainThreshold">The operand count above which a flat <c>AND</c> or <c>OR</c> chain is boxed, or <see langword="null"/> for no compaction.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintMermaid(
        TextWriter writer,
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
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(
            this.PrintMermaid(
                decision,
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
    /// Writes this rule's Mermaid <c>flowchart</c> text, colored by one evaluation, to <paramref name="writer"/> using
    /// <paramref name="options"/>. The output is exactly the text that <see cref="PrintMermaid(Decision, MermaidOptions)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="options">The diagram options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintMermaid(TextWriter writer, Decision decision, MermaidOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintMermaid(decision, options));
    }

    /// <summary>
    /// Renders this rule as an HTML fragment: one infix formula in nested <c>span</c> elements with the classes
    /// <c>tw-operator</c>, <c>tw-term</c> and <c>tw-constant</c>, HTML entities for the logic symbols and a <c>sub</c> element for
    /// the subscript of a simple variable. The fragment has no <c>script</c> element and no <c>style</c> attribute, and every
    /// term, argument and constant is escaped, so it is safe to put in a page.
    /// </summary>
    /// <param name="options">The HTML options, or <see langword="null"/> for <see cref="HtmlOptions.Default"/>.</param>
    /// <returns>The HTML fragment.</returns>
    public string PrintHtml(HtmlOptions? options = null)
    {
        return HtmlPrinter.Print(this.Outline(), options ?? HtmlOptions.Default);
    }

    /// <summary>
    /// Writes this rule as an HTML fragment to <paramref name="writer"/>. The output is exactly the text that
    /// <see cref="PrintHtml(HtmlOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The HTML options, or <see langword="null"/> for <see cref="HtmlOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintHtml(TextWriter writer, HtmlOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintHtml(options));
    }

    /// <summary>
    /// Renders this rule as an HTML fragment, with the state class <c>tw-true</c>, <c>tw-false</c>, <c>tw-unknown</c> or
    /// <c>tw-skipped</c> added to each node of one evaluation.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="options">The HTML options, or <see langword="null"/> for <see cref="HtmlOptions.Default"/>.</param>
    /// <returns>The HTML fragment.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintHtml(Decision decision, HtmlOptions? options = null)
    {
        return HtmlPrinter.Print(this.Outline(), RequireTraceTree(decision), options ?? HtmlOptions.Default);
    }

    /// <summary>
    /// Writes this rule as an HTML fragment, with a state class on each node of one evaluation, to
    /// <paramref name="writer"/>. The output is exactly the text that <see cref="PrintHtml(Decision, HtmlOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="options">The HTML options, or <see langword="null"/> for <see cref="HtmlOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintHtml(TextWriter writer, Decision decision, HtmlOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintHtml(decision, options));
    }

    /// <summary>
    /// Renders this rule as a Graphviz DOT <c>digraph</c>: a hexagon for each operator, a rounded box for each term and a circle
    /// for each constant, with one edge from each operator to each operand. The node ids are <c>n0</c>, <c>n1</c> and so on
    /// in depth-first order, and the labels are escaped for DOT.
    /// </summary>
    /// <param name="options">The DOT options, or <see langword="null"/> for <see cref="DotOptions.Default"/>.</param>
    /// <returns>The DOT text.</returns>
    public string PrintDot(DotOptions? options = null)
    {
        return DotPrinter.Print(this.Outline(), options ?? DotOptions.Default);
    }

    /// <summary>
    /// Writes this rule as a Graphviz DOT <c>digraph</c> to <paramref name="writer"/>. The output is exactly the text that
    /// <see cref="PrintDot(DotOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The DOT options, or <see langword="null"/> for <see cref="DotOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintDot(TextWriter writer, DotOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintDot(options));
    }

    /// <summary>
    /// Renders this rule as a Graphviz DOT <c>digraph</c>, with a fill color on each node of one evaluation: green for
    /// <c>True</c>, red for <c>False</c>, yellow for <c>Unknown</c> and gray for a node that short-circuiting skipped.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="options">The DOT options, or <see langword="null"/> for <see cref="DotOptions.Default"/>.</param>
    /// <returns>The DOT text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintDot(Decision decision, DotOptions? options = null)
    {
        return DotPrinter.Print(this.Outline(), RequireTraceTree(decision), options ?? DotOptions.Default);
    }

    /// <summary>
    /// Writes this rule as a Graphviz DOT <c>digraph</c>, with a fill color on each node of one evaluation, to
    /// <paramref name="writer"/>. The output is exactly the text that <see cref="PrintDot(Decision, DotOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="options">The DOT options, or <see langword="null"/> for <see cref="DotOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintDot(TextWriter writer, Decision decision, DotOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintDot(decision, options));
    }

    /// <summary>Renders this rule's structure as an indented plain-text tree.</summary>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="constants">How to write the constants, or <see langword="null"/> (the default) for <see cref="ConstantStyle.Word"/>.</param>
    /// <returns>The indented tree text.</returns>
    public string PrintPlainText(bool showArgumentValues = true, ConstantFormat? constants = null)
    {
        return PlainTextTreePrinter.Print(this.Outline(), showArgumentValues: showArgumentValues, constants: constants);
    }

    /// <summary>
    /// Writes this rule's structure as an indented plain-text tree to <paramref name="writer"/>. The output is exactly the
    /// text that <see cref="PrintPlainText(bool, ConstantFormat?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="constants">How to write the constants, or <see langword="null"/> (the default) for <see cref="ConstantStyle.Word"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintPlainText(TextWriter writer, bool showArgumentValues = true, ConstantFormat? constants = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintPlainText(showArgumentValues, constants));
    }

    /// <summary>
    /// Renders this rule's structure as an indented plain-text tree, annotated by one evaluation's
    /// result and short-circuit path.
    /// </summary>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="constants">How to write the constants, or <see langword="null"/> (the default) for <see cref="ConstantStyle.Word"/>.</param>
    /// <returns>The indented tree text.</returns>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule, as happens with a decision from a different rule.</exception>
    public string PrintPlainText(Decision decision, bool showArgumentValues = true, ConstantFormat? constants = null)
    {
        return PlainTextTreePrinter.Print(
            this.Outline(),
            RequireTraceTree(decision),
            showArgumentValues: showArgumentValues,
            constants: constants
        );
    }

    /// <summary>
    /// Writes this rule's structure as an indented plain-text tree, annotated by one evaluation, to
    /// <paramref name="writer"/>. The output is exactly the text that <see cref="PrintPlainText(Decision, bool, ConstantFormat?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="decision">A <see cref="Decision"/> returned from <see cref="EvaluateAsync"/> for this same rule.</param>
    /// <param name="showArgumentValues">Whether to include each term's rule-text argument values in its label. Defaults to <see langword="true"/>.</param>
    /// <param name="constants">How to write the constants, or <see langword="null"/> (the default) for <see cref="ConstantStyle.Word"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="decision"/> has no <see cref="Decision.TraceTree"/>. Nothing is written in this case.</exception>
    /// <exception cref="InvalidOperationException">The <see cref="Decision.TraceTree"/> of <paramref name="decision"/> does not match the shape of this rule. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintPlainText(
        TextWriter writer,
        Decision decision,
        bool showArgumentValues = true,
        ConstantFormat? constants = null
    )
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintPlainText(decision, showArgumentValues, constants));
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
    /// Writes this rule as a flat, single-line infix equation to <paramref name="writer"/>. The output is exactly the text
    /// that <see cref="PrintEquation(EquationOptions?)"/> returns.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The dialect and term options, or <see langword="null"/> for <see cref="EquationOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintEquation(TextWriter writer, EquationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.Write(this.PrintEquation(options));
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
    /// Writes this rule as an equation with a letter for each term, followed by its legend, to <paramref name="writer"/>.
    /// The output is the <see cref="EquationWithLegend.Equation"/> of <see cref="PrintEquationWithLegend(EquationOptions?)"/>.
    /// When the rule has at least one term, a <c>\n</c> and the <see cref="EquationWithLegend.LegendText"/> follow. The text
    /// has no trailing newline. For a rule with no term, only the equation is written.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The dialect options, or <see langword="null"/> for <see cref="EquationOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintEquationWithLegend(TextWriter writer, EquationOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        EquationWithLegend result = this.PrintEquationWithLegend(options);
        writer.Write(result.Equation);
        if (result.LegendText.Length > 0)
        {
            writer.Write('\n');
            writer.Write(result.LegendText);
        }
    }

    /// <summary>
    /// Prints the truth table of this rule: one row for each assignment of <c>False</c>, <c>Unknown</c> and <c>True</c> to
    /// its distinct terms (3<sup>n</sup> rows), or of <c>False</c> and <c>True</c> only when
    /// <see cref="TruthTableOptions.Classical"/> is set (2<sup>n</sup> rows), with the rule's value for the row. The table
    /// treats each term as a free variable. It calls no predicate and reads no data source, so a rule with variable
    /// references prints the same way. The first column is the first term to occur, and the first column changes slowest.
    /// The values run <c>False</c>, <c>Unknown</c>, <c>True</c>, a layout order and not a number. The text has no trailing line
    /// feed.
    /// </summary>
    /// <param name="options">The format and term options, or <see langword="null"/> for <see cref="TruthTableOptions.Default"/>.</param>
    /// <returns>The table text.</returns>
    /// <exception cref="InvalidOperationException">
    /// The rule has more terms than <see cref="TruthTableOptions.DefaultTermCap"/> and <see cref="TruthTableOptions.AllowLargerTable"/>
    /// is not set, or it has more terms than <see cref="TruthTableOptions.HardTermLimit"/>. Call
    /// <see cref="TryPrintTruthTable(TruthTableOptions?, out string?, out IReadOnlyList{Diagnostic})"/> to avoid the exception.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value.</exception>
    public string PrintTruthTable(TruthTableOptions? options = null)
    {
        using StringWriter writer = new(CultureInfo.InvariantCulture);
        this.PrintTruthTable(writer, options);
        return writer.ToString();
    }

    /// <summary>
    /// Writes the truth table of this rule to <paramref name="writer"/>. The output is exactly the text that
    /// <see cref="PrintTruthTable(TruthTableOptions?)"/> returns, without a large string that the caller must hold.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it.</param>
    /// <param name="options">The format and term options, or <see langword="null"/> for <see cref="TruthTableOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The table is refused, as for <see cref="PrintTruthTable(TruthTableOptions?)"/>. Nothing is written in this case.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public void PrintTruthTable(TextWriter writer, TruthTableOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        if (
            !TruthTablePrinter.TryPrint(
                this.Root,
                options ?? TruthTableOptions.Default,
                writer,
                out IReadOnlyList<Diagnostic> diagnostics
            )
        )
        {
            throw new InvalidOperationException(diagnostics[0].Message);
        }
    }

    /// <summary>
    /// Prints the truth table, like <see cref="PrintTruthTable(TruthTableOptions?)"/>, but reports a refused table through
    /// the return value and the diagnostics instead of throwing.
    /// </summary>
    /// <param name="options">The format and term options, or <see langword="null"/> for <see cref="TruthTableOptions.Default"/>.</param>
    /// <param name="text">The table text, or <see langword="null"/> when this method returns <see langword="false"/>.</param>
    /// <param name="diagnostics">
    /// A <see cref="DiagnosticCodes.TruthTableCapRaised"/> warning when the table has more terms than the default cap and the
    /// options allow it, a <see cref="DiagnosticCodes.TruthTableTooLarge"/> error when the table is refused, or empty.
    /// </param>
    /// <returns><see langword="true"/> unless the table is refused.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value.</exception>
    public bool TryPrintTruthTable(
        TruthTableOptions? options,
        [NotNullWhen(true)] out string? text,
        out IReadOnlyList<Diagnostic> diagnostics
    )
    {
        using StringWriter writer = new(CultureInfo.InvariantCulture);
        bool printed = this.TryPrintTruthTable(writer, options, out diagnostics);
        text = printed ? writer.ToString() : null;
        return printed;
    }

    /// <summary>
    /// Writes the truth table to <paramref name="writer"/>, like <see cref="PrintTruthTable(TextWriter, TruthTableOptions?)"/>,
    /// but reports a refused table through the return value and the diagnostics instead of throwing.
    /// </summary>
    /// <param name="writer">The writer to write to. This method does not flush or dispose it. Nothing is written when the table is refused.</param>
    /// <param name="options">The format and term options, or <see langword="null"/> for <see cref="TruthTableOptions.Default"/>.</param>
    /// <param name="diagnostics">As for <see cref="TryPrintTruthTable(TruthTableOptions?, out string?, out IReadOnlyList{Diagnostic})"/>.</param>
    /// <returns><see langword="true"/> unless the table is refused.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The <see cref="TruthTableOptions.Format"/> is not a defined value. Nothing is written in this case.</exception>
    /// <exception cref="IOException">The writer failed. The exception reaches the caller unchanged.</exception>
    public bool TryPrintTruthTable(TextWriter writer, TruthTableOptions? options, out IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return TruthTablePrinter.TryPrint(this.Root, options ?? TruthTableOptions.Default, writer, out diagnostics);
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
    /// The service provider to resolve class-based predicates from, fresh for this call. Never
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

    /// <summary>
    /// Evaluates this rule against each context of a sequence and yields one <see cref="Decision"/> per context, in the
    /// order of the contexts. Each context is one <see cref="EvaluateAsync"/> call with the same
    /// <paramref name="services"/>, <paramref name="dataSources"/> and <paramref name="options"/>, so
    /// <see cref="EvaluationOptions.Timeout"/> and <see cref="EvaluationOptions.FaultBudget"/> apply to each context
    /// separately. A predicate fault in one context makes only that decision <see cref="TruthValue.Unknown"/>; the
    /// other contexts are evaluated as usual.
    /// </summary>
    /// <remarks>
    /// At most <paramref name="maxDegreeOfParallelism"/> evaluations are in flight at one time, and a decision that
    /// finishes early waits for the decisions before it, so the output order never depends on timing. The sequence is
    /// read lazily, one context per free slot. The default of 1 evaluates one context at a time; use a higher value
    /// only when the predicates and <paramref name="services"/> are safe to call from several threads at once. When the
    /// consumer stops reading, or an evaluation throws, the evaluations still in flight are cancelled and awaited
    /// before the enumeration ends. Collect every decision with <c>ToArrayAsync</c> from <c>System.Linq</c>.
    /// </remarks>
    /// <param name="contexts">The contexts to evaluate, read lazily.</param>
    /// <param name="services">The service provider for every evaluation, as for <see cref="EvaluateAsync"/>. With a parallelism above 1 it is used from several threads.</param>
    /// <param name="dataSources">The data sources for every evaluation, or <see langword="null"/> when the rule has no variables. They are only read.</param>
    /// <param name="options">The options for every evaluation, or <see langword="null"/> for the defaults.</param>
    /// <param name="maxDegreeOfParallelism">The largest number of evaluations in flight at one time. The default is 1.</param>
    /// <param name="cancellationToken">A token that stops the batch. It reaches every evaluation in flight.</param>
    /// <returns>The decisions, one per context, in the order of <paramref name="contexts"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contexts"/> is <see langword="null"/>. Thrown when the method is called.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is below 1, or <paramref name="options"/> has a <see cref="EvaluationOptions.FaultBudget"/> below 1. Thrown when the method is called.</exception>
    /// <exception cref="OperationCanceledException">The token cancelled the batch, or <see cref="EvaluationOptions.Timeout"/> expired for one context. Thrown during the enumeration; neither is recorded as a fault.</exception>
    public IAsyncEnumerable<Decision> EvaluateManyAsync(
        IEnumerable<TContext> contexts,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? options = null,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(contexts);
        return this.EvaluateManyAsync(
            contexts.ToAsyncEnumerable(),
            services,
            dataSources,
            options,
            maxDegreeOfParallelism,
            cancellationToken
        );
    }

    /// <summary>
    /// Evaluates this rule against each context of an asynchronous stream and yields one <see cref="Decision"/> per
    /// context, in the order the stream produces them. The behavior is the same as for the
    /// <see cref="IEnumerable{T}"/> overload of <c>EvaluateManyAsync</c>: options per context, bounded parallelism,
    /// stable order and cancellation of the evaluations in flight when the enumeration stops.
    /// </summary>
    /// <param name="contexts">The contexts to evaluate, read lazily with <paramref name="cancellationToken"/>.</param>
    /// <param name="services">The service provider for every evaluation, as for <see cref="EvaluateAsync"/>. With a parallelism above 1 it is used from several threads.</param>
    /// <param name="dataSources">The data sources for every evaluation, or <see langword="null"/> when the rule has no variables. They are only read.</param>
    /// <param name="options">The options for every evaluation, or <see langword="null"/> for the defaults.</param>
    /// <param name="maxDegreeOfParallelism">The largest number of evaluations in flight at one time. The default is 1.</param>
    /// <param name="cancellationToken">A token that stops the batch. It reaches the stream and every evaluation in flight.</param>
    /// <returns>The decisions, one per context, in the order of <paramref name="contexts"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contexts"/> is <see langword="null"/>. Thrown when the method is called.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is below 1, or <paramref name="options"/> has a <see cref="EvaluationOptions.FaultBudget"/> below 1. Thrown when the method is called.</exception>
    /// <exception cref="OperationCanceledException">The token cancelled the batch, or <see cref="EvaluationOptions.Timeout"/> expired for one context. Thrown during the enumeration; neither is recorded as a fault.</exception>
    public IAsyncEnumerable<Decision> EvaluateManyAsync(
        IAsyncEnumerable<TContext> contexts,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? options = null,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(contexts);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, 1);

        // Checked here, not in the iterator, so a bad option fails at the call rather than at the first read.
        if (options is { FaultBudget: < 1 })
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.FaultBudget, "The fault budget must be 1 or more.");
        }

        return BatchEvaluator.EvaluateAsync(
            this,
            contexts,
            services,
            dataSources,
            options,
            maxDegreeOfParallelism,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.CanonicalText;
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

        // A quantifier is not in the closed OperatorDefinitions table (ADR-0010: a third kind, not a connective
        // with a fixed arity), so it is outlined directly instead of through OperatorInfo.Describe, and it carries
        // its bound variable and collection name for the printers to render as part of its head, not as a term's
        // parenthesized argument text.
        if (node is QuantifierExpression quantifier)
        {
            string label = quantifier.Kind == QuantifierKind.ForAll ? "FORALL" : "EXISTS";
            string description =
                quantifier.Kind == QuantifierKind.ForAll
                    ? "Universal quantifier: the generalized AND over a registered collection binding's elements (ADR-0010)."
                    : "Existential quantifier: the generalized OR over a registered collection binding's elements (ADR-0010).";
            return new OutlineNode(
                label,
                description,
                [OutlineOf(quantifier.Body, registry)],
                Kind: OutlineNodeKind.Operator,
                QuantifierVariable: quantifier.Variable,
                QuantifierCollection: quantifier.CollectionName
            );
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
    /// without repeating the predicate name. That comes from the term's own
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
                "This decision has no TraceTree to render - it must come from EvaluateAsync on this same rule.",
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
