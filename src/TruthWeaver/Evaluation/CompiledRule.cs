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
public sealed partial class CompiledRule<TContext>
{
    private readonly IReadOnlyDictionary<Expression, string>? libraryLocators;
    private readonly PredicateRegistry<TContext> registry;
    private readonly ILogger logger;
    private readonly Lazy<string> canonicalText;
    private readonly Lazy<IReadOnlySet<string>> predicateNames;
    private readonly Lazy<IReadOnlySet<TermIdentity>> termIdentities;
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
    /// <param name="libraryLocators">
    /// The library locator text of each node that a library rule reference expanded to, keyed by the node itself (by
    /// reference), or <see langword="null"/> for none. The evaluator adds it to the trace entry of the node. Like the
    /// notes, only the compiler passes them.
    /// </param>
    internal CompiledRule(
        Expression root,
        PredicateRegistry<TContext> registry,
        ILogger? logger = null,
        CompilerOptions? options = null,
        IReadOnlyDictionary<string, string>? notes = null,
        IReadOnlyDictionary<Expression, string>? libraryLocators = null
    )
    {
        this.libraryLocators = libraryLocators;
        this.options = options ?? CompilerOptions.Default;
        this.Root = root;
        this.registry = registry;
        this.logger = logger ?? NullLogger.Instance;
        this.Notes = notes is { Count: > 0 } ? notes.ToFrozenDictionary(StringComparer.Ordinal) : [];
        this.canonicalText = new Lazy<string>(() => CanonicalPrinter.Print(this.Root));
        this.predicateNames = new Lazy<IReadOnlySet<string>>(() => CollectPredicateNames(this.Root));
        this.termIdentities = new Lazy<IReadOnlySet<TermIdentity>>(() => CollectTermIdentities(this.Root));
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
    /// Gets the identity of each term this rule references, once each, plus one synthetic identity
    /// <c>TermIdentity(collectionName, [])</c> per collection a quantifier of this rule ranges over. Internal:
    /// visible to <c>TruthWeaver.Policies</c> via <c>InternalsVisibleTo</c>, which attributes a fault recorded for
    /// a shared <see cref="EvaluationState"/> back to every rule of a policy set whose own terms (or, for a
    /// quantifier, whose own collection references) include it, since a term or collection fault raised once for a
    /// shared state records its fault once there, not once per rule that references it (feature backlog 08). The
    /// collection identities use the same <c>TermIdentity(collectionName, [])</c> shape a quantifier's own fault is
    /// recorded against (a throwing selector, <see cref="EvaluationOptions.MaxElementEvaluations"/>, or an
    /// unregistered collection), so that fault is attributed here the same way a predicate term's is.
    /// </summary>
    internal IReadOnlySet<TermIdentity> TermIdentities => this.termIdentities.Value;

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

    private static HashSet<TermIdentity> CollectTermIdentities(Expression root)
    {
        HashSet<TermIdentity> identities = [];
        Stack<Expression> pending = new([root]);
        while (pending.TryPop(out Expression? node))
        {
            if (node is TermExpression term)
            {
                identities.Add(term.Identity);
            }
            else if (node is not ConstantExpression)
            {
                // A quantifier's own fault (a throwing selector, MaxElementEvaluations, or an unregistered
                // collection) is recorded against TermIdentity(CollectionName, []), not against a TermExpression
                // in its body (CollectionBinding.cs, Evaluator.cs). Adding that identity here, alongside the
                // body's own term identities, is what lets PolicySet's per-rule fault filter (which matches
                // TermIdentities) recognize such a fault as this rule's own.
                if (node is QuantifierExpression quantifier)
                {
                    identities.Add(new TermIdentity(quantifier.CollectionName, []));
                }

                foreach (Expression operand in ExpressionShape.Of(node).Operands)
                {
                    pending.Push(operand);
                }
            }
        }

        return identities;
    }
}
