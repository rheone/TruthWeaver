namespace TruthWeaver.Building;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Json;
using TruthWeaver.Parsing;

/// <summary>
/// A fluent, programmatic way to assemble a rule without hand-writing DSL/JSON/YAML text,
/// useful when a rule's shape comes from application logic (e.g. a dynamically built list of
/// conditions) rather than from an author typing it directly. A builder tree renders to the exact
/// flat, key-discriminated JSON tree shape ADR-0003 defines and compiles through
/// <see cref="RuleCompiler{TContext}.CompileJson(System.Text.Json.JsonElement)"/>, so a builder-assembled rule
/// receives every diagnostic a hand-written one would (unknown predicate, bad argument, invalid threshold,
/// XOR/EQUIVALENT arity, resource limits, structural tautology/contradiction). Nothing here bypasses the
/// Validate/Analyze stages of the compilation pipeline. A diagnostic located in the tree carries a path into
/// this builder's tree, never a span into the generated JSON (see <see cref="Compile{TContext}(RuleCompiler{TContext})"/>).
/// </summary>
public abstract class RuleBuilder
{
    private RuleBuilder() { }

    /// <summary>Creates a builder for the literal <c>True</c>/<c>False</c> constant.</summary>
    /// <param name="value">The constant's value.</param>
    /// <returns>A builder for the constant.</returns>
    public static RuleBuilder Constant(bool value)
    {
        return new ConstantBuilder(value ? TruthValue.True : TruthValue.False);
    }

    /// <summary>Creates a builder for a literal K3 constant, including <see cref="TruthValue.Unknown"/>.</summary>
    /// <param name="value">The constant's value.</param>
    /// <returns>A builder for the constant.</returns>
    public static RuleBuilder Constant(TruthValue value)
    {
        return new ConstantBuilder(value);
    }

    /// <summary>
    /// Creates a builder that holds the tree of an already compiled rule, so a host can join rules with any builder
    /// operator without a JSON round trip.
    /// </summary>
    /// <remarks>
    /// The builder keeps the rule's notes (<see cref="CompiledRule{TContext}.Notes"/>) on their nodes, so they come out of
    /// the joined rule under their new paths. The source rule may come from another registry, so the join is not trusted.
    /// Compiling the joined builder validates every term again against the destination registry. A predicate missing there, or with a different
    /// schema, gives the normal compile diagnostics. Data-source declarations (<c>TRE0024</c>) and the destination
    /// <c>CompilerOptions</c> limits apply to the joined tree. A term that appears in both source rules is one term in
    /// the joined rule, so its predicate runs once per evaluation.
    /// </remarks>
    /// <example>
    /// <code>
    /// CompilationResult&lt;User&gt; joined = RuleBuilder
    ///     .And(RuleBuilder.FromCompiled(baseRule), RuleBuilder.FromCompiled(tenantRule))
    ///     .Compile(compiler);
    /// </code>
    /// </example>
    /// <typeparam name="TContext">The application context type of the rule. Joined rules share it.</typeparam>
    /// <param name="rule">The compiled rule whose tree the builder holds.</param>
    /// <returns>A builder for the rule's tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static RuleBuilder FromCompiled<TContext>(CompiledRule<TContext> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return new CompiledBuilder(rule.Root, rule.NoteLookup);
    }

    /// <summary>Creates a builder for a zero-argument predicate reference.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <returns>A builder for the term.</returns>
    public static RuleBuilder Predicate(string name)
    {
        return new PredicateBuilder(name, []);
    }

    /// <summary>Creates a builder for a predicate reference with named arguments.</summary>
    /// <param name="name">The predicate's registered name.</param>
    /// <param name="arguments">
    /// The named argument values. Supported value types are <see cref="string"/>, <see cref="bool"/>,
    /// <see cref="int"/>, <see cref="long"/>, <see cref="double"/>, <see cref="decimal"/>,
    /// <see cref="DateTimeOffset"/>, <see cref="Guid"/>, a deferred variable reference from <see cref="Arg.From"/>, and
    /// <see cref="IEnumerable{T}"/> of any of those (for an array-valued argument).
    /// </param>
    /// <returns>A builder for the term.</returns>
    public static RuleBuilder Predicate(string name, params (string Name, object Value)[] arguments)
    {
        return new PredicateBuilder(name, arguments);
    }

    /// <summary>
    /// Creates a builder for a use of a named definition (ADR-0008 decision 4): the builder equivalent of rule
    /// text's <c>name(arg1, arg2, ...)</c>, or the bare <c>name</c> for a parameterless definition. The use expands
    /// at compile time to the matching <see cref="RuleDefinition"/>'s own tree, with each argument substituted for
    /// the matching parameter; attach the definitions a use can reach with <see cref="WithDefinitions(RuleDefinition[])"/>
    /// on the tree's root builder. A use of a name no definition declares, or with the wrong argument count, is the
    /// same compile diagnostic (<c>UnknownDefinition</c>, <c>DefinitionArgumentCountMismatch</c>) rule text and
    /// JSON/YAML report.
    /// </summary>
    /// <param name="name">The definition's name, as declared with <see cref="Define(string, RuleBuilder)"/> or <see cref="Define(string, IReadOnlyList{string}, RuleBuilder)"/>.</param>
    /// <param name="arguments">The positional arguments, each itself a term, constant or sub-expression; empty for a parameterless use.</param>
    /// <returns>A builder for the use.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Use(string name, params RuleBuilder[] arguments)
    {
        return Use(name, (IEnumerable<RuleBuilder>)arguments);
    }

    /// <summary>The <see cref="IEnumerable{T}"/> form of <see cref="Use(string, RuleBuilder[])"/>, for an argument list of unknown length.</summary>
    /// <param name="name">The definition's name.</param>
    /// <param name="arguments">The positional arguments; may be empty.</param>
    /// <returns>A builder for the use.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="arguments"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Use(string name, IEnumerable<RuleBuilder> arguments)
    {
        ArgumentNullException.ThrowIfNull(name);

        return new UseBuilder(name, Materialize(arguments));
    }

    /// <summary>Creates a parameterless named definition for <see cref="WithDefinitions(RuleDefinition[])"/>.</summary>
    /// <param name="name">The definition's name, unique (case-insensitively) within the tree it is attached to; it cannot reuse a reserved word, <c>T</c>/<c>F</c>/<c>U</c>, or a predicate name registered with the compiler.</param>
    /// <param name="body">The definition's own expression.</param>
    /// <returns>The definition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="body"/> is <see langword="null"/>.</exception>
    public static RuleDefinition Define(string name, RuleBuilder body)
    {
        return Define(name, [], body);
    }

    /// <summary>
    /// Creates a named definition with parameters for <see cref="WithDefinitions(RuleDefinition[])"/>. Reference a
    /// parameter inside <paramref name="body"/> with <see cref="Predicate(string)"/> of the same name. A bare term
    /// with no arguments, named after a parameter in scope, is the parameter reference rather than a predicate call,
    /// the same disambiguation rule text and JSON/YAML make for a bare identifier (ADR-0008 decision 5).
    /// </summary>
    /// <param name="name">The definition's name, unique (case-insensitively) within the tree it is attached to; it cannot reuse a reserved word, <c>T</c>/<c>F</c>/<c>U</c>, or a predicate name registered with the compiler.</param>
    /// <param name="parameters">The definition's parameter names, in positional order; empty for a parameterless definition.</param>
    /// <param name="body">The definition's own expression, in which <paramref name="parameters"/> are referenced as described above.</param>
    /// <returns>The definition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="parameters"/> or <paramref name="body"/> is <see langword="null"/>.</exception>
    public static RuleDefinition Define(string name, IReadOnlyList<string> parameters, RuleBuilder body)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(body);

        return new RuleDefinition(name, [.. parameters], body);
    }

    /// <summary>
    /// Creates a builder for <c>FORALL(variable IN collection, body)</c> (ADR-0010): the generalized <c>AND</c>
    /// over a registered collection binding's elements, <c>True</c> for an empty collection and <c>Unknown</c>
    /// for a <see langword="null"/> one. Nesting another <see cref="ForAll"/>/<see cref="Exists"/> inside
    /// <paramref name="body"/> is allowed, up to <see cref="CompilerOptions.MaxQuantifierDepth"/>.
    /// </summary>
    /// <param name="variable">The bound variable name. Part of tree equality and the trace labels (<c>line[2]</c>).</param>
    /// <param name="collection">The registered collection name, resolved against the compiler's registry at compile time.</param>
    /// <param name="body">The element condition, checked against the collection's own element registry, not this tree's outer registry.</param>
    /// <returns>A builder for the quantifier.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="variable"/>, <paramref name="collection"/> or <paramref name="body"/> is <see langword="null"/>.</exception>
    public static RuleBuilder ForAll(string variable, string collection, RuleBuilder body)
    {
        return QuantifierBuilder.Create(QuantifierKind.ForAll, variable, collection, body);
    }

    /// <summary>
    /// Creates a builder for <c>EXISTS(variable IN collection, body)</c> (ADR-0010): the generalized <c>OR</c>
    /// over a registered collection binding's elements, <c>False</c> for an empty collection and <c>Unknown</c>
    /// for a <see langword="null"/> one. See <see cref="ForAll"/> for the shared rules (nesting, variable identity).
    /// </summary>
    /// <param name="variable">The bound variable name. Part of tree equality and the trace labels (<c>line[2]</c>).</param>
    /// <param name="collection">The registered collection name, resolved against the compiler's registry at compile time.</param>
    /// <param name="body">The element condition, checked against the collection's own element registry, not this tree's outer registry.</param>
    /// <returns>A builder for the quantifier.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="variable"/>, <paramref name="collection"/> or <paramref name="body"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Exists(string variable, string collection, RuleBuilder body)
    {
        return QuantifierBuilder.Create(QuantifierKind.Exists, variable, collection, body);
    }

    /// <summary>Creates a builder for logical conjunction.</summary>
    /// <param name="operands">The operands. An empty array is <c>Constant(TruthValue.True)</c>; one operand is itself.</param>
    /// <remarks>
    /// Fewer than two operands fold when the rule is built, the same as for the <see cref="And(IEnumerable{RuleBuilder})"/>
    /// overload: an empty array yields <c>Constant(TruthValue.True)</c>, the identity of <c>AND</c>, and a single operand yields that operand unchanged.
    /// An array and a list of the same operands give the same rule.
    /// </remarks>
    /// <returns>A builder for the <c>AND</c> expression, or the folded constant or operand.</returns>
    public static RuleBuilder And(params RuleBuilder[] operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        return operands.Length switch
        {
            0 => Constant(TruthValue.True),
            1 => operands[0],
            _ => new OperatorBuilder("And", operands),
        };
    }

    /// <summary>
    /// Creates a builder for <c>AND</c> from a sequence of operands whose length is only known at run time. It gives the same
    /// rule as the array overload <see cref="And(RuleBuilder[])"/> for the same operands: an empty sequence yields
    /// <c>Constant(TruthValue.True)</c>, a single operand yields that operand unchanged, and two or more operands build the full node. The
    /// sequence is enumerated once.
    /// </summary>
    /// <param name="operands">The operands; may be empty or hold a single item.</param>
    /// <returns>A builder for the folded or full <c>AND</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder And(IEnumerable<RuleBuilder> operands)
    {
        return And(Materialize(operands));
    }

    /// <summary>Creates a builder for logical disjunction.</summary>
    /// <param name="operands">The operands. An empty array is <c>Constant(TruthValue.False)</c>; one operand is itself.</param>
    /// <remarks>
    /// Fewer than two operands fold when the rule is built, the same as for the <see cref="Or(IEnumerable{RuleBuilder})"/>
    /// overload: an empty array yields <c>Constant(TruthValue.False)</c>, the identity of <c>OR</c>, and a single operand yields that operand unchanged.
    /// An array and a list of the same operands give the same rule.
    /// </remarks>
    /// <returns>A builder for the <c>OR</c> expression, or the folded constant or operand.</returns>
    public static RuleBuilder Or(params RuleBuilder[] operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        return operands.Length switch
        {
            0 => Constant(TruthValue.False),
            1 => operands[0],
            _ => new OperatorBuilder("Or", operands),
        };
    }

    /// <summary>
    /// Creates a builder for <c>OR</c> from a sequence of operands whose length is only known at run time. It gives the same
    /// rule as the array overload <see cref="Or(RuleBuilder[])"/> for the same operands: an empty sequence yields
    /// <c>Constant(TruthValue.False)</c>, a single operand yields that operand unchanged, and two or more operands build the full node. The
    /// sequence is enumerated once.
    /// </summary>
    /// <param name="operands">The operands; may be empty or hold a single item.</param>
    /// <returns>A builder for the folded or full <c>OR</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Or(IEnumerable<RuleBuilder> operands)
    {
        return Or(Materialize(operands));
    }

    /// <summary>Creates a builder for logical negation.</summary>
    /// <param name="operand">The negated sub-expression.</param>
    /// <returns>A builder for the <c>NOT</c> expression.</returns>
    public static RuleBuilder Not(RuleBuilder operand)
    {
        return new OperatorBuilder("Not", [operand]);
    }

    /// <summary>Creates a builder for binary exclusive-or.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>XOR</c> expression.</returns>
    public static RuleBuilder Xor(RuleBuilder left, RuleBuilder right)
    {
        return new OperatorBuilder("Xor", [left, right]);
    }

    /// <summary>Creates a builder for the binary logical biconditional (<c>EQUIVALENT</c>, also written <c>IFF</c> or <c>↔</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>EQUIVALENT</c> expression.</returns>
    public static RuleBuilder Equivalent(RuleBuilder left, RuleBuilder right)
    {
        return new OperatorBuilder("Equivalent", [left, right]);
    }

    /// <summary>
    /// Creates a builder for the biconditional under its pre-ADR-0005 name. Forwards to <see cref="Equivalent"/>
    /// (same rule, same canonical text); kept so existing callers keep compiling.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>EQUIVALENT</c> expression.</returns>
    public static RuleBuilder Xnor(RuleBuilder left, RuleBuilder right)
    {
        return Equivalent(left, right);
    }

    /// <summary>Creates a builder for material implication (<c>NOT antecedent OR consequent</c>).</summary>
    /// <param name="antecedent">The "if" operand.</param>
    /// <param name="consequent">The "then" operand.</param>
    /// <returns>A builder for the <c>IMPLIES</c> expression.</returns>
    public static RuleBuilder Implies(RuleBuilder antecedent, RuleBuilder consequent)
    {
        return new OperatorBuilder("Implies", [antecedent, consequent]);
    }

    /// <summary>Creates a builder for negated conjunction (<c>NOT (left AND right)</c>, also written <c>NAND</c> or <c>↑</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>NAND</c> expression.</returns>
    public static RuleBuilder Nand(RuleBuilder left, RuleBuilder right)
    {
        return new OperatorBuilder("Nand", [left, right]);
    }

    /// <summary>Creates a builder for negated disjunction (<c>NOT (left OR right)</c>, also written <c>NOR</c> or <c>↓</c>).</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>A builder for the <c>NOR</c> expression.</returns>
    public static RuleBuilder Nor(RuleBuilder left, RuleBuilder right)
    {
        return new OperatorBuilder("Nor", [left, right]);
    }

    /// <summary>Creates a builder for n-ary parity (<c>PARITY(a, b, ...)</c>): <c>Unknown</c> if any operand is <c>Unknown</c>, otherwise <c>True</c> for an odd number of <c>True</c> operands.</summary>
    /// <param name="operands">The operands (at least two).</param>
    /// <remarks>
    /// <c>PARITY</c> has no identity constant, so a list with fewer than two operands is not folded. It builds a node that
    /// the rule compiler rejects (<c>InfixArityViolation</c>), the same as the <see cref="Parity(IEnumerable{RuleBuilder})"/> overload.
    /// </remarks>
    /// <returns>A builder for the <c>PARITY</c> expression.</returns>
    public static RuleBuilder Parity(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("Parity", operands);
    }

    /// <summary>
    /// Creates a builder for <c>PARITY</c> from a sequence of operands whose length is only known at run time. It builds the
    /// same node and gets the same compile diagnostic as the array overload <see cref="Parity(RuleBuilder[])"/>. A short or empty
    /// sequence is <b>not</b> folded, because <c>PARITY</c> has no identity constant. The sequence is enumerated once.
    /// </summary>
    /// <param name="operands">The operands (at least two, otherwise a compile diagnostic).</param>
    /// <returns>A builder for the <c>PARITY</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Parity(IEnumerable<RuleBuilder> operands)
    {
        return Parity(Materialize(operands));
    }

    /// <summary>Creates a builder for <c>ANY(...)</c>: at least one operand is true (<c>AtLeast(1, ...)</c>).</summary>
    /// <param name="operands">The operands. An empty array is <c>Constant(TruthValue.False)</c>; one operand is itself.</param>
    /// <remarks>
    /// Fewer than two operands fold when the rule is built, the same as for the <see cref="Any(IEnumerable{RuleBuilder})"/>
    /// overload: an empty array yields <c>Constant(TruthValue.False)</c>, the identity of <c>ANY</c>, and a single operand yields that operand unchanged.
    /// An array and a list of the same operands give the same rule.
    /// </remarks>
    /// <returns>A builder for the <c>ANY</c> expression, or the folded constant or operand.</returns>
    public static RuleBuilder Any(params RuleBuilder[] operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        return operands.Length switch
        {
            0 => Constant(TruthValue.False),
            1 => operands[0],
            _ => new OperatorBuilder("Any", operands),
        };
    }

    /// <summary>
    /// Creates a builder for <c>ANY</c> from a sequence of operands whose length is only known at run time. It gives the same
    /// rule as the array overload <see cref="Any(RuleBuilder[])"/> for the same operands: an empty sequence yields
    /// <c>Constant(TruthValue.False)</c>, a single operand yields that operand unchanged, and two or more operands build the full node. The
    /// sequence is enumerated once.
    /// </summary>
    /// <param name="operands">The operands; may be empty or hold a single item.</param>
    /// <returns>A builder for the folded or full <c>ANY</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Any(IEnumerable<RuleBuilder> operands)
    {
        return Any(Materialize(operands));
    }

    /// <summary>Creates a builder for <c>ALL(...)</c>: every operand is true (<c>AtLeast(n, ...)</c>).</summary>
    /// <param name="operands">The operands. An empty array is <c>Constant(TruthValue.True)</c>; one operand is itself.</param>
    /// <remarks>
    /// Fewer than two operands fold when the rule is built, the same as for the <see cref="All(IEnumerable{RuleBuilder})"/>
    /// overload: an empty array yields <c>Constant(TruthValue.True)</c>, the identity of <c>ALL</c>, and a single operand yields that operand unchanged.
    /// An array and a list of the same operands give the same rule.
    /// </remarks>
    /// <returns>A builder for the <c>ALL</c> expression, or the folded constant or operand.</returns>
    public static RuleBuilder All(params RuleBuilder[] operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        return operands.Length switch
        {
            0 => Constant(TruthValue.True),
            1 => operands[0],
            _ => new OperatorBuilder("All", operands),
        };
    }

    /// <summary>
    /// Creates a builder for <c>ALL</c> from a sequence of operands whose length is only known at run time. It gives the same
    /// rule as the array overload <see cref="All(RuleBuilder[])"/> for the same operands: an empty sequence yields
    /// <c>Constant(TruthValue.True)</c>, a single operand yields that operand unchanged, and two or more operands build the full node. The
    /// sequence is enumerated once.
    /// </summary>
    /// <param name="operands">The operands; may be empty or hold a single item.</param>
    /// <returns>A builder for the folded or full <c>ALL</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder All(IEnumerable<RuleBuilder> operands)
    {
        return All(Materialize(operands));
    }

    /// <summary>Creates a builder for <c>NONE(...)</c>: no operand is true (<c>AtMost(0, ...)</c>).</summary>
    /// <param name="operands">The operands. An empty array is <c>Constant(TruthValue.True)</c>; one operand is its negation.</param>
    /// <remarks>
    /// Fewer than two operands fold when the rule is built, the same as for the <see cref="None(IEnumerable{RuleBuilder})"/>
    /// overload: an empty array yields <c>Constant(TruthValue.True)</c>, the identity of <c>NONE</c>, and a single operand yields its negation.
    /// An array and a list of the same operands give the same rule.
    /// </remarks>
    /// <returns>A builder for the <c>NONE</c> expression, or the folded constant or operand.</returns>
    public static RuleBuilder None(params RuleBuilder[] operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        return operands.Length switch
        {
            0 => Constant(TruthValue.True),
            1 => Not(operands[0]),
            _ => new OperatorBuilder("None", operands),
        };
    }

    /// <summary>
    /// Creates a builder for <c>NONE</c> from a sequence of operands whose length is only known at run time. It gives the same
    /// rule as the array overload <see cref="None(RuleBuilder[])"/> for the same operands: an empty sequence yields
    /// <c>Constant(TruthValue.True)</c>, a single operand yields its negation, and two or more operands build the full node. The
    /// sequence is enumerated once.
    /// </summary>
    /// <param name="operands">The operands; may be empty or hold a single item.</param>
    /// <returns>A builder for the folded or full <c>NONE</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder None(IEnumerable<RuleBuilder> operands)
    {
        return None(Materialize(operands));
    }

    /// <summary>Creates a builder for the n-ary "exactly one of these is true" operator.</summary>
    /// <param name="operands">The operands (at least two).</param>
    /// <remarks>
    /// <c>EXACTLYONE</c> has no identity constant, so a list with fewer than two operands is not folded. It builds a node that
    /// the rule compiler rejects (<c>InfixArityViolation</c>), the same as the <see cref="ExactlyOne(IEnumerable{RuleBuilder})"/> overload.
    /// </remarks>
    /// <returns>A builder for the <c>ExactlyOne</c> expression.</returns>
    public static RuleBuilder ExactlyOne(params RuleBuilder[] operands)
    {
        return new OperatorBuilder("ExactlyOne", operands);
    }

    /// <summary>
    /// Creates a builder for <c>EXACTLYONE</c> from a sequence of operands whose length is only known at run time. It builds the
    /// same node and gets the same compile diagnostic as the array overload <see cref="ExactlyOne(RuleBuilder[])"/>. A short or empty
    /// sequence is <b>not</b> folded, because <c>EXACTLYONE</c> has no identity constant. The sequence is enumerated once.
    /// </summary>
    /// <param name="operands">The operands (at least two, otherwise a compile diagnostic).</param>
    /// <returns>A builder for the <c>EXACTLYONE</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder ExactlyOne(IEnumerable<RuleBuilder> operands)
    {
        return ExactlyOne(Materialize(operands));
    }

    /// <summary>
    /// Creates a builder for <c>COALESCE(...)</c>: the first operand that is not <c>Unknown</c> (<c>True</c> and
    /// <c>False</c> pass through).
    /// </summary>
    /// <param name="operands">The operands in priority order. An empty array is <c>Constant(TruthValue.Unknown)</c>; one operand is itself.</param>
    /// <remarks>
    /// Fewer than two operands fold when the rule is built, the same as for the <see cref="Coalesce(IEnumerable{RuleBuilder})"/>
    /// overload, because <c>Unknown</c> is the identity of <c>COALESCE</c> on both sides: an empty array yields
    /// <c>Constant(TruthValue.Unknown)</c> and a single operand yields that operand unchanged. An array and a list of the same
    /// operands give the same rule.
    /// </remarks>
    /// <returns>A builder for the <c>COALESCE</c> expression, or the folded constant or operand.</returns>
    public static RuleBuilder Coalesce(params RuleBuilder[] operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        return operands.Length switch
        {
            0 => Constant(TruthValue.Unknown),
            1 => operands[0],
            _ => new OperatorBuilder("Coalesce", operands),
        };
    }

    /// <summary>
    /// Creates a builder for <c>COALESCE</c> from a sequence of operands whose length is only known at run time. It builds the
    /// same rule as the array overload <see cref="Coalesce(RuleBuilder[])"/>: an empty sequence yields
    /// <c>Constant(TruthValue.Unknown)</c> and a single operand yields that operand unchanged. The sequence is enumerated once.
    /// </summary>
    /// <param name="operands">The operands in priority order.</param>
    /// <returns>A builder for the <c>COALESCE</c> expression, or the folded constant or operand.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Coalesce(IEnumerable<RuleBuilder> operands)
    {
        return Coalesce(Materialize(operands));
    }

    /// <summary>
    /// Creates a builder for <c>IsTrue(operand)</c>: <c>True</c> iff the operand is <c>True</c>, otherwise <c>False</c>
    /// (never <c>Unknown</c>).
    /// </summary>
    /// <param name="operand">The expression to inspect.</param>
    /// <returns>A builder for the <c>IsTrue</c> expression.</returns>
    public static RuleBuilder IsTrue(RuleBuilder operand)
    {
        return new OperatorBuilder("IsTrue", [operand]);
    }

    /// <summary>
    /// Creates a builder for <c>IsFalse(operand)</c>: <c>True</c> iff the operand is <c>False</c>, otherwise <c>False</c>
    /// (never <c>Unknown</c>).
    /// </summary>
    /// <param name="operand">The expression to inspect.</param>
    /// <returns>A builder for the <c>IsFalse</c> expression.</returns>
    public static RuleBuilder IsFalse(RuleBuilder operand)
    {
        return new OperatorBuilder("IsFalse", [operand]);
    }

    /// <summary>
    /// Creates a builder for <c>IsUnknown(operand)</c>: <c>True</c> iff the operand is <c>Unknown</c>, otherwise <c>False</c>
    /// (never <c>Unknown</c>).
    /// </summary>
    /// <param name="operand">The expression to inspect.</param>
    /// <returns>A builder for the <c>IsUnknown</c> expression.</returns>
    public static RuleBuilder IsUnknown(RuleBuilder operand)
    {
        return new OperatorBuilder("IsUnknown", [operand]);
    }

    /// <summary>
    /// Creates a builder for <c>IsKnown(operand)</c>: <c>True</c> iff the operand is <c>True</c> or <c>False</c>, otherwise
    /// <c>False</c> (never <c>Unknown</c>).
    /// </summary>
    /// <param name="operand">The expression to inspect.</param>
    /// <returns>A builder for the <c>IsKnown</c> expression.</returns>
    public static RuleBuilder IsKnown(RuleBuilder operand)
    {
        return new OperatorBuilder("IsKnown", [operand]);
    }

    /// <summary>
    /// Creates a builder for <c>If(condition, whenTrue, whenFalse)</c>: <paramref name="whenTrue"/> when the condition is
    /// <c>True</c>, <paramref name="whenFalse"/> when it is <c>False</c>, and for an <c>Unknown</c> condition the branch value
    /// only if both branches are the same definite value, otherwise <c>Unknown</c>.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <param name="whenTrue">The result when the condition is <c>True</c>.</param>
    /// <param name="whenFalse">The result when the condition is <c>False</c>.</param>
    /// <returns>A builder for the <c>If</c> expression.</returns>
    public static RuleBuilder If(RuleBuilder condition, RuleBuilder whenTrue, RuleBuilder whenFalse)
    {
        return new OperatorBuilder("If", [condition, whenTrue, whenFalse]);
    }

    /// <summary>
    /// Creates a builder for <c>BETWEEN(min, max, ...)</c>: the number of true operands lies in the inclusive range
    /// <c>[min, max]</c> (<c>AtLeast(min, ...) AND AtMost(max, ...)</c>).
    /// </summary>
    /// <param name="min">The inclusive lower bound (at least 0).</param>
    /// <param name="max">The inclusive upper bound (at least <paramref name="min"/>, at most the operand count).</param>
    /// <param name="operands">The operands (at least two).</param>
    /// <returns>A builder for the <c>BETWEEN</c> expression.</returns>
    public static RuleBuilder Between(int min, int max, params RuleBuilder[] operands)
    {
        return new BetweenBuilder(min, max, operands);
    }

    /// <summary>
    /// Creates a builder for <c>BETWEEN(min, max, ...)</c> from a sequence of operands. It is meant for a sequence whose length is only known at run time; it builds the same node and goes through the same count validation: a short or empty sequence is <b>not</b> folded (a counted operator has no identity constant), so an unmeetable count such as <c>AtLeast(2, [])</c> is the same compile diagnostic as with <c>params</c>. An empty sequence is therefore probably a bug. The sequence is enumerated once.
    /// </summary>
    /// <param name="min">The inclusive lower bound (at least 0).</param>
    /// <param name="max">The inclusive upper bound (at least <paramref name="min"/>, at most the operand count).</param>
    /// <param name="operands">The operands (at least two, otherwise a compile diagnostic).</param>
    /// <returns>A builder for the <c>BETWEEN</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Between(int min, int max, IEnumerable<RuleBuilder> operands)
    {
        return Between(min, max, Materialize(operands));
    }

    /// <summary>Creates a builder for "at least <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>AtLeast</c> expression.</returns>
    public static RuleBuilder AtLeast(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("AtLeast", k, operands);
    }

    /// <summary>
    /// Creates a builder for "at least <paramref name="k"/> of these operands are true" from a sequence of operands. It is meant for a sequence whose length is only known at run time; it builds the same node and goes through the same count validation: a short or empty sequence is <b>not</b> folded (a counted operator has no identity constant), so an unmeetable count such as <c>AtLeast(2, [])</c> is the same compile diagnostic as with <c>params</c>. An empty sequence is therefore probably a bug. The sequence is enumerated once.
    /// </summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>AtLeast</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder AtLeast(int k, IEnumerable<RuleBuilder> operands)
    {
        return AtLeast(k, Materialize(operands));
    }

    /// <summary>Creates a builder for "at most <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>AtMost</c> expression.</returns>
    public static RuleBuilder AtMost(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("AtMost", k, operands);
    }

    /// <summary>
    /// Creates a builder for "at most <paramref name="k"/> of these operands are true" from a sequence of operands. It is meant for a sequence whose length is only known at run time; it builds the same node and goes through the same count validation: a short or empty sequence is <b>not</b> folded (a counted operator has no identity constant), so an unmeetable count such as <c>AtLeast(2, [])</c> is the same compile diagnostic as with <c>params</c>. An empty sequence is therefore probably a bug. The sequence is enumerated once.
    /// </summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>AtMost</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder AtMost(int k, IEnumerable<RuleBuilder> operands)
    {
        return AtMost(k, Materialize(operands));
    }

    /// <summary>Creates a builder for "more than <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>GreaterThan</c> expression.</returns>
    public static RuleBuilder GreaterThan(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("GreaterThan", k, operands);
    }

    /// <summary>
    /// Creates a builder for "more than <paramref name="k"/> of these operands are true" from a sequence of operands. It is meant for a sequence whose length is only known at run time; it builds the same node and goes through the same count validation: a short or empty sequence is <b>not</b> folded (a counted operator has no identity constant), so an unmeetable count is the same compile diagnostic as with <c>params</c>. An empty sequence is therefore probably a bug. The sequence is enumerated once.
    /// </summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>GreaterThan</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder GreaterThan(int k, IEnumerable<RuleBuilder> operands)
    {
        return GreaterThan(k, Materialize(operands));
    }

    /// <summary>Creates a builder for "fewer than <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>LessThan</c> expression.</returns>
    public static RuleBuilder LessThan(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("LessThan", k, operands);
    }

    /// <summary>
    /// Creates a builder for "fewer than <paramref name="k"/> of these operands are true" from a sequence of operands. It is meant for a sequence whose length is only known at run time; it builds the same node and goes through the same count validation: a short or empty sequence is <b>not</b> folded (a counted operator has no identity constant), so an unmeetable count is the same compile diagnostic as with <c>params</c>. An empty sequence is therefore probably a bug. The sequence is enumerated once.
    /// </summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>LessThan</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder LessThan(int k, IEnumerable<RuleBuilder> operands)
    {
        return LessThan(k, Materialize(operands));
    }

    /// <summary>Creates a builder for "exactly <paramref name="k"/> of these operands are true".</summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>Exactly</c> expression.</returns>
    public static RuleBuilder Exactly(int k, params RuleBuilder[] operands)
    {
        return new ThresholdBuilder("Exactly", k, operands);
    }

    /// <summary>
    /// Creates a builder for "exactly <paramref name="k"/> of these operands are true" from a sequence of operands. It is meant for a sequence whose length is only known at run time; it builds the same node and goes through the same count validation: a short or empty sequence is <b>not</b> folded (a counted operator has no identity constant), so an unmeetable count such as <c>AtLeast(2, [])</c> is the same compile diagnostic as with <c>params</c>. An empty sequence is therefore probably a bug. The sequence is enumerated once.
    /// </summary>
    /// <param name="k">The threshold.</param>
    /// <param name="operands">The operands.</param>
    /// <returns>A builder for the <c>Exactly</c> expression.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public static RuleBuilder Exactly(int k, IEnumerable<RuleBuilder> operands)
    {
        return Exactly(k, Materialize(operands));
    }

    /// <summary>
    /// Puts a note on this builder's node: free text for the people who read the rule, kept beside the compiled tree as
    /// one of <see cref="CompiledRule{TContext}.Notes"/> and written by the text, JSON and YAML printers. A note never
    /// changes the compiled tree or how the rule evaluates.
    /// </summary>
    /// <remarks>
    /// The note is normalized when the rule compiles: line endings become a line feed, each line loses its trailing
    /// whitespace and the whole note is trimmed, so a note of only whitespace is no note. Calling this method on a builder
    /// that already has a note replaces that note. An operator that folds (<c>And()</c> with one operand is that operand)
    /// has no node of its own, so put the note on the operand.
    /// </remarks>
    /// <param name="note">The note text.</param>
    /// <returns>A builder for the same node, with the note.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="note"/> is <see langword="null"/>.</exception>
    public RuleBuilder WithNote(string note)
    {
        ArgumentNullException.ThrowIfNull(note);

        return new NotedBuilder(this, note);
    }

    /// <summary>
    /// Attaches named definitions (ADR-0008 decision 4) to this builder's tree, so a <see cref="Use(string, RuleBuilder[])"/>
    /// anywhere in it (including inside another definition's own body) can expand through them. Call it once, on the
    /// root builder of the tree passed to <see cref="Compile{TContext}(RuleCompiler{TContext})"/> or <see cref="ToJson"/>.
    /// A definition is document-scoped, the same as a rule text <c>let</c> or a JSON/YAML <c>definitions</c> section,
    /// not a property of the particular node it happens to be called on.
    /// </summary>
    /// <remarks>
    /// Compiles through the same <c>definitions</c>/<c>use</c> JSON shape ADR-0008 decision 3 gives the JSON and YAML
    /// forms, so a builder tree with definitions compiles to the identical tree the equivalent rule text or JSON/YAML
    /// document would. Every guardrail diagnostic those forms report (a cycle, a name collision, a chain deeper than
    /// <c>CompilerOptions.MaxDefinitionDepth</c>, a wrong argument count, an unknown definition, and, opted into, the
    /// unused-definition lint) is reported here too, located by the builder path (never a span into generated text,
    /// matching every other builder diagnostic). A second call replaces the definitions of the first, the same policy
    /// <see cref="WithNote"/> uses for a second note.
    /// </remarks>
    /// <param name="definitions">The definitions; may be empty.</param>
    /// <returns>A builder for the same tree, with the definitions attached.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="definitions"/> is <see langword="null"/>.</exception>
    public RuleBuilder WithDefinitions(params RuleDefinition[] definitions)
    {
        return this.WithDefinitions((IEnumerable<RuleDefinition>)definitions);
    }

    /// <summary>The <see cref="IEnumerable{T}"/> form of <see cref="WithDefinitions(RuleDefinition[])"/>, for a definition list of unknown length.</summary>
    /// <param name="definitions">The definitions; may be empty.</param>
    /// <returns>A builder for the same tree, with the definitions attached.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="definitions"/> is <see langword="null"/>.</exception>
    public RuleBuilder WithDefinitions(IEnumerable<RuleDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        return new DefinitionsBuilder(this, [.. definitions]);
    }

    /// <summary>Renders this builder's tree to the flat JSON tree shape text (ADR-0003).</summary>
    /// <returns>The JSON text.</returns>
    /// <exception cref="ArgumentException">An argument value has a type that JSON cannot carry, or is a <see langword="double"/> that is <c>NaN</c> or an infinity. <see cref="Compile{TContext}(RuleCompiler{TContext})"/> reports it as <see cref="DiagnosticCodes.ArgumentTypeMismatch"/> instead.</exception>
    /// <exception cref="InvalidOperationException">
    /// A predicate in the tree is given the same argument name twice. JSON cannot hold both, and keeping one would hide the
    /// mistake; <see cref="Compile{TContext}(RuleCompiler{TContext})"/> reports it as <see cref="DiagnosticCodes.DuplicateArgument"/> instead.
    /// </exception>
    public string ToJson()
    {
        List<Diagnostic> problems = [];
        this.CollectProblems(TreePath.Root, problems);
        if (problems.Find(d => d.Code == DiagnosticCodes.DuplicateArgument) is { } duplicate)
        {
            throw new InvalidOperationException(duplicate.Message);
        }

        return this.ToNode().ToJsonString();
    }

    /// <summary>
    /// Compiles this builder's tree through the exact same Validate → Analyze → Build pipeline any
    /// other rule source (DSL/JSON/YAML text) uses.
    /// </summary>
    /// <remarks>
    /// A diagnostic located in this tree (an unknown predicate, a bad argument, an arity violation, and the
    /// rest) carries a path into this builder's tree, the same <c>$.operands[1].op</c> shape a hand-written
    /// JSON or YAML rule gets, but never a <see cref="Diagnostic.Span"/> into generated text: this method
    /// compiles through <see cref="RuleCompiler{TContext}.CompileJson(JsonElement)"/>, which, unlike
    /// <see cref="RuleCompiler{TContext}.CompileJson(string)"/>, never re-reads rendered JSON to locate a path
    /// as a span (ADR-0005 decision 11), because the caller never sees that JSON and a span into it would not
    /// mean anything in the builder tree the caller wrote.
    /// </remarks>
    /// <typeparam name="TContext">The application context type the compiler's registry evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate and build this tree with.</param>
    /// <returns>
    /// The compilation result. A tree so deep that even the raised JSON nesting cap
    /// (<see cref="JsonTreeParser.DocumentMaxDepth"/>, derived from <paramref name="compiler"/>'s
    /// <c>CompilerOptions.MaxDepth</c>) cannot represent it reports <see cref="DiagnosticCodes.MalformedTree"/>
    /// rather than throwing: this method never throws for an authoring error.
    /// </returns>
    public CompilationResult<TContext> Compile<TContext>(RuleCompiler<TContext> compiler)
    {
        // A mistake that the JSON shape cannot carry (a repeated argument name) is reported here, before rendering.
        List<Diagnostic> problems = [];
        this.CollectProblems(TreePath.Root, problems);
        if (problems.Count > 0)
        {
            return new CompilationResult<TContext>(null, new EquatableArray<Diagnostic>(problems));
        }

        // CompileJson(JsonElement), not CompileJson(string): the element overload never re-reads generated text
        // for a diagnostic's span, so every diagnostic here carries only its builder path and Diagnostic.Span.None.
        // The parse uses the same raised depth cap CompileJson(string) does (JsonTreeParser.DocumentMaxDepth), so a
        // builder tree at this compiler's own CompilerOptions.MaxDepth round-trips through JSON instead of hitting
        // JsonDocument's unrelated default cap of 64.
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(
                this.ToJson(),
                new JsonDocumentOptions { MaxDepth = JsonTreeParser.DocumentMaxDepth(compiler.Options.MaxDepth) }
            );
        }
        catch (JsonException ex)
        {
            return new CompilationResult<TContext>(null, new EquatableArray<Diagnostic>([MalformedJson(ex)]));
        }

        using (document)
        {
            return compiler.CompileJson(document.RootElement);
        }
    }

    /// <summary>Compiles this builder's tree without a result object: the <c>Try</c> form of <see cref="Compile{TContext}(RuleCompiler{TContext})"/>.</summary>
    /// <typeparam name="TContext">The application context type the compiler's registry evaluates against.</typeparam>
    /// <param name="compiler">The compiler to validate and build this tree with.</param>
    /// <param name="rule">The compiled rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic raised, including warnings on success.</param>
    /// <returns><see langword="true"/> when a rule was produced (warnings alone do not fail it); otherwise <see langword="false"/>.</returns>
    public bool TryCompile<TContext>(
        RuleCompiler<TContext> compiler,
        [NotNullWhen(true)] out CompiledRule<TContext>? rule,
        out EquatableArray<Diagnostic> diagnostics
    )
    {
        return this.Compile(compiler).TryGetRule(out rule, out diagnostics);
    }

    private protected abstract JsonNode ToNode();

    /// <summary>
    /// Collects the mistakes in this tree that the JSON rendering would hide, each as an error located by its path in the
    /// rendered tree. The default tree has none.
    /// </summary>
    /// <param name="path">The path of this builder's node in the rendered tree.</param>
    /// <param name="problems">Receives the diagnostics.</param>
    private protected virtual void CollectProblems(string path, List<Diagnostic> problems) { }

    private static void CollectOperandProblems(string path, IReadOnlyList<RuleBuilder> operands, List<Diagnostic> problems)
    {
        string operandsPath = TreePath.Property(path, "operands");
        for (int i = 0; i < operands.Count; i++)
        {
            operands[i].CollectProblems(TreePath.Index(operandsPath, i), problems);
        }
    }

    // Mirrors JsonTreeParser.SyntaxError's message shape, but a builder diagnostic never carries a Span (there is no
    // source text the caller can see to locate) or a path past the document root: the generated JSON is itself
    // beyond rendering at this point, not a specific builder node.
    private static Diagnostic MalformedJson(JsonException ex)
    {
        string reason = ex.Message;
        int detail = reason.IndexOf(" Path:", StringComparison.Ordinal);
        if (detail >= 0)
        {
            reason = reason[..detail];
        }

        return Diagnostic.Error(
            DiagnosticCodes.MalformedTree,
            $"Malformed JSON: {ex.Message}",
            SourceSpan.None,
            expected: "well-formed JSON",
            found: reason,
            path: TreePath.Root
        );
    }

    /// <summary>Rejects a null sequence and materialises it once, without folding, for the counted operators.</summary>
    private static RuleBuilder[] Materialize(IEnumerable<RuleBuilder> operands)
    {
        ArgumentNullException.ThrowIfNull(operands);

        return [.. operands];
    }

    private static JsonNode ValueToNode(object? value)
    {
        return value switch
        {
            string s => JsonValue.Create(s),
            bool b => JsonValue.Create(b),
            int i => JsonValue.Create(i),
            long l => JsonValue.Create(l),
            double d => JsonValue.Create(d),
            decimal m => JsonValue.Create(m),
            DateTimeOffset dto => JsonValue.Create(dto.ToString("O", CultureInfo.InvariantCulture)),
            Guid g => JsonValue.Create(g.ToString()),
            VariableReference reference => new JsonObject { ["from"] = reference.Source, ["query"] = reference.Query },
            System.Collections.IEnumerable items => ArrayToNode(items),
            _ => throw new ArgumentException($"Unsupported argument value type '{value?.GetType()}'.", nameof(value)),
        };
    }

    /// <summary>
    /// Converts any sequence, including value-type arrays such as <c>long[]</c>, which do not bind
    /// <c>IEnumerable&lt;object&gt;</c> because array covariance excludes value types. Each element goes through
    /// <see cref="ValueToNode"/>, so an unsupported element type throws <see cref="ArgumentException"/>.
    /// </summary>
    private static JsonArray ArrayToNode(System.Collections.IEnumerable items)
    {
        JsonArray array = [];
        foreach (object item in items)
        {
            array.Add(ValueToNode(item));
        }

        return array;
    }

    private static JsonArray OperandsNode(IReadOnlyList<RuleBuilder> operands)
    {
        JsonArray array = [];
        foreach (RuleBuilder operand in operands)
        {
            array.Add(operand.ToNode());
        }

        return array;
    }

    private sealed class CompiledBuilder(Expression root, NoteLookup? notes) : RuleBuilder
    {
        private readonly Expression root = root;
        private readonly NoteLookup? notes = notes;

        private protected override JsonNode ToNode()
        {
            // Same printer as CompiledRule.PrintJson, so the tree and its notes re-enter the pipeline in the JSON shape.
            return JsonTreePrinter.ToNode(this.root, this.notes);
        }
    }

    /// <summary>A node with a note: the inner builder's node with a <c>note</c> property added (or replaced).</summary>
    private sealed class NotedBuilder(RuleBuilder inner, string note) : RuleBuilder
    {
        private readonly RuleBuilder inner = inner;
        private readonly string note = note;

        private protected override void CollectProblems(string path, List<Diagnostic> problems)
        {
            // The note does not move the node, so the inner node keeps its path.
            this.inner.CollectProblems(path, problems);
        }

        private protected override JsonNode ToNode()
        {
            JsonNode node = this.inner.ToNode();
            node["note"] = this.note;
            return node;
        }
    }

    private sealed class ConstantBuilder(TruthValue value) : RuleBuilder
    {
        private readonly TruthValue value = value;

        private protected override JsonNode ToNode()
        {
            // Same shape JsonTreePrinter writes: booleans for True/False, the string "unknown" otherwise.
            return this.value switch
            {
                TruthValue.True => new JsonObject { ["const"] = true },
                TruthValue.False => new JsonObject { ["const"] = false },
                _ => new JsonObject { ["const"] = TruthValueText.TreeFormat(this.value) },
            };
        }
    }

    private sealed class PredicateBuilder(string name, (string Name, object Value)[] arguments) : RuleBuilder
    {
        private readonly string name = name;
        private readonly (string Name, object Value)[] arguments = arguments;

        private protected override void CollectProblems(string path, List<Diagnostic> problems)
        {
            // The compiler matches argument names ignoring case, so two spellings of one name are one argument given twice.
            HashSet<string> seen = [with(StringComparer.OrdinalIgnoreCase)];
            foreach ((string argName, object? argValue) in this.arguments)
            {
                string argumentPath = TreePath.Property(TreePath.Property(path, "args"), argName);

                // A value the JSON shape cannot carry is a rule mistake, so it is a diagnostic and never an exception from Compile.
                if (UnsupportedValueType(argValue) is { } typeName)
                {
                    problems.Add(
                        Diagnostic.Error(
                            DiagnosticCodes.ArgumentTypeMismatch,
                            $"Argument '{argName}' of predicate '{this.name}' has an unsupported value type '{typeName}'.",
                            SourceSpan.None,
                            expected: "a string, bool, int, long, double, decimal, DateTimeOffset, Guid, Arg.From(...) or a sequence of those",
                            found: $"'{typeName}'",
                            path: argumentPath
                        )
                    );
                }
                else if (HasNonFiniteDouble(argValue))
                {
                    // JSON has no NaN or infinity, so the JSON writer would throw from ToJson; report it as a value of the wrong kind.
                    problems.Add(
                        Diagnostic.Error(
                            DiagnosticCodes.ArgumentTypeMismatch,
                            $"Argument '{argName}' of predicate '{this.name}' holds a double that is not a finite number (NaN or an infinity).",
                            SourceSpan.None,
                            expected: "a finite number",
                            found: "NaN or an infinity",
                            path: argumentPath
                        )
                    );
                }
                else if (HasLoneSurrogate(argValue))
                {
                    // The JSON writer replaces a lone surrogate with U+FFFD, so the rule would silently hold a different
                    // value. JSON text with a raw lone surrogate is MalformedTree, and the builder reports it the same way.
                    problems.Add(
                        Diagnostic.Error(
                            DiagnosticCodes.MalformedTree,
                            $"Argument '{argName}' of predicate '{this.name}' holds a string that is not valid UTF-16 (a lone surrogate).",
                            SourceSpan.None,
                            expected: "valid UTF-16 text",
                            found: "a lone surrogate",
                            path: argumentPath
                        )
                    );
                }

                if (!seen.Add(argName))
                {
                    problems.Add(
                        Diagnostic.Error(
                            DiagnosticCodes.DuplicateArgument,
                            $"Argument '{argName}' of predicate '{this.name}' is given more than once.",
                            SourceSpan.None,
                            expected: "each argument once",
                            found: $"'{argName}' repeated",
                            suggestion: new DiagnosticSuggestion(
                                DiagnosticSuggestionKind.Hint,
                                $"Remove one '{argName}' argument."
                            ),
                            path: argumentPath
                        )
                    );
                }
            }
        }

        private protected override JsonNode ToNode()
        {
            JsonObject node = new() { ["predicate"] = this.name };
            if (this.arguments.Length > 0)
            {
                JsonObject args = [];
                foreach ((string argName, object argValue) in this.arguments)
                {
                    args[argName] = ValueToNode(argValue);
                }

                node["args"] = args;
            }

            return node;
        }

        /// <summary>
        /// Finds the first value, at any depth of a sequence, that <see cref="ValueToNode"/> cannot render.
        /// </summary>
        /// <returns>The offending type's name (<c>null</c> for a null value), or <see langword="null"/> when every value is supported.</returns>
        /// <summary>
        /// Finds a string, at any depth of a sequence or in either part of an <c>Arg.From</c> reference, that holds a
        /// high surrogate with no low surrogate after it, or a low surrogate with no high surrogate before it.
        /// </summary>
        private static bool HasLoneSurrogate(object? value)
        {
            return value switch
            {
                string text => !IsValidUtf16(text),
                VariableReference reference => !IsValidUtf16(reference.Source) || !IsValidUtf16(reference.Query),
                System.Collections.IEnumerable items => items.Cast<object?>().Any(HasLoneSurrogate),
                _ => false,
            };
        }

        /// <summary>Finds a <see langword="double"/>, at any depth of a sequence, that is <c>NaN</c> or an infinity.</summary>
        private static bool HasNonFiniteDouble(object? value)
        {
            return value switch
            {
                double number => !double.IsFinite(number),
                string => false,
                System.Collections.IEnumerable items => items.Cast<object?>().Any(HasNonFiniteDouble),
                _ => false,
            };
        }

        private static bool IsValidUtf16(string text)
        {
            // Rune.DecodeFromUtf16 reports InvalidData for exactly the lone surrogates; a valid pair decodes as one rune.
            ReadOnlySpan<char> rest = text;
            while (!rest.IsEmpty)
            {
                if (Rune.DecodeFromUtf16(rest, out _, out int consumed) != OperationStatus.Done)
                {
                    return false;
                }

                rest = rest[consumed..];
            }

            return true;
        }

        private static string? UnsupportedValueType(object? value)
        {
            switch (value)
            {
                case null:
                    return "null";
                case string or bool or int or long or double or decimal or DateTimeOffset or Guid or VariableReference:
                    return null;
                case System.Collections.IEnumerable items:
                    foreach (object? item in items)
                    {
                        if (UnsupportedValueType(item) is { } nested)
                        {
                            return nested;
                        }
                    }

                    return null;
                default:
                    return value.GetType().ToString();
            }
        }
    }

    private sealed class OperatorBuilder(string op, IReadOnlyList<RuleBuilder> operands) : RuleBuilder
    {
        private readonly string op = op;
        private readonly IReadOnlyList<RuleBuilder> operands = operands;

        private protected override void CollectProblems(string path, List<Diagnostic> problems)
        {
            CollectOperandProblems(path, this.operands, problems);
        }

        private protected override JsonNode ToNode()
        {
            return new JsonObject
            {
                ["op"] = TreeFormatOpNames.ToTreeFormat(this.op),
                ["operands"] = OperandsNode(this.operands),
            };
        }
    }

    private sealed class BetweenBuilder(int min, int max, IReadOnlyList<RuleBuilder> operands) : RuleBuilder
    {
        private readonly int min = min;
        private readonly int max = max;
        private readonly IReadOnlyList<RuleBuilder> operands = operands;

        private protected override void CollectProblems(string path, List<Diagnostic> problems)
        {
            CollectOperandProblems(path, this.operands, problems);
        }

        private protected override JsonNode ToNode()
        {
            return new JsonObject
            {
                ["op"] = TreeFormatOpNames.ToTreeFormat("Between"),
                ["min"] = this.min,
                ["max"] = this.max,
                ["operands"] = OperandsNode(this.operands),
            };
        }
    }

    private sealed class ThresholdBuilder(string op, int k, IReadOnlyList<RuleBuilder> operands) : RuleBuilder
    {
        private readonly string op = op;
        private readonly int k = k;
        private readonly IReadOnlyList<RuleBuilder> operands = operands;

        private protected override void CollectProblems(string path, List<Diagnostic> problems)
        {
            CollectOperandProblems(path, this.operands, problems);
        }

        private protected override JsonNode ToNode()
        {
            return new JsonObject
            {
                ["op"] = TreeFormatOpNames.ToTreeFormat(this.op),
                ["k"] = this.k,
                ["operands"] = OperandsNode(this.operands),
            };
        }
    }

    /// <summary>
    /// A <c>FORALL</c>/<c>EXISTS</c> quantifier (ADR-0010 decision 2): renders to the
    /// <c>{"op": "forall" | "exists", "var": ..., "in": ..., "operands": [&lt;body&gt;]}</c> shape decision 2
    /// defines, with the body as the one item of <c>operands</c>, so body diagnostics get the path
    /// <c>$.operands[0]...</c> like every other node.
    /// </summary>
    private sealed class QuantifierBuilder(QuantifierKind kind, string variable, string collection, RuleBuilder body)
        : RuleBuilder
    {
        public static RuleBuilder Create(QuantifierKind kind, string variable, string collection, RuleBuilder body)
        {
            ArgumentNullException.ThrowIfNull(variable);
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(body);

            return new QuantifierBuilder(kind, variable, collection, body);
        }

        private protected override void CollectProblems(string path, List<Diagnostic> problems)
        {
            body.CollectProblems(TreePath.Index(TreePath.Property(path, "operands"), 0), problems);
        }

        private protected override JsonNode ToNode()
        {
            return new JsonObject
            {
                ["op"] = kind == QuantifierKind.ForAll ? "forall" : "exists",
                ["var"] = variable,
                ["in"] = collection,
                ["operands"] = new JsonArray(body.ToNode()),
            };
        }
    }

    /// <summary>A use of a named definition (ADR-0008 decision 4): renders to the <c>{"op": "use", ...}</c> shape ADR-0008 decision 3 defines for JSON/YAML.</summary>
    private sealed class UseBuilder(string name, IReadOnlyList<RuleBuilder> arguments) : RuleBuilder
    {
        private readonly string name = name;
        private readonly IReadOnlyList<RuleBuilder> arguments = arguments;

        private protected override void CollectProblems(string path, List<Diagnostic> problems)
        {
            CollectOperandProblems(path, this.arguments, problems);
        }

        private protected override JsonNode ToNode()
        {
            // "use" is not an entry in OperatorDefinitions/TreeFormatOpNames: it is the one node shape ADR-0008 adds
            // outside the operator catalog, so the literal lower-case name goes straight into the node, matching
            // what TreeFormatReader.ReadUse compares against (case-insensitively).
            JsonObject node = new() { ["op"] = "use", ["name"] = this.name };
            if (this.arguments.Count > 0)
            {
                node["operands"] = OperandsNode(this.arguments);
            }

            return node;
        }
    }

    /// <summary>
    /// Attaches a <c>definitions</c> section (ADR-0008 decision 3) to an inner builder's rendered node. A duplicate
    /// definition name is reported here, before rendering, because the JSON object the definitions render into would
    /// otherwise silently keep only the last one, the same reason <c>PredicateBuilder</c> pre-checks a duplicate
    /// argument name instead of letting it reach <c>CompileJson</c> invisibly.
    /// </summary>
    private sealed class DefinitionsBuilder(RuleBuilder body, IReadOnlyList<RuleDefinition> definitions) : RuleBuilder
    {
        private readonly RuleBuilder body = body;
        private readonly IReadOnlyList<RuleDefinition> definitions = definitions;

        private protected override void CollectProblems(string path, List<Diagnostic> problems)
        {
            this.body.CollectProblems(path, problems);

            string definitionsPath = TreePath.Property(path, "definitions");
            HashSet<string> seen = [with(StringComparer.OrdinalIgnoreCase)];
            foreach (RuleDefinition definition in this.definitions)
            {
                string entryPath = TreePath.Property(definitionsPath, definition.Name);
                if (!seen.Add(definition.Name))
                {
                    problems.Add(
                        Diagnostic.Error(
                            DiagnosticCodes.DefinitionNameCollision,
                            $"Definition name '{definition.Name}' clashes with a definition already declared with this name in this document.",
                            SourceSpan.None,
                            expected: "a definition name not already used in this document",
                            found: $"'{definition.Name}' repeated",
                            suggestion: new DiagnosticSuggestion(
                                DiagnosticSuggestionKind.Hint,
                                $"Remove or rename one of the '{definition.Name}' definitions."
                            ),
                            path: entryPath
                        )
                    );
                    continue;
                }

                definition.Body.CollectProblems(TreePath.Property(entryPath, "body"), problems);
            }
        }

        private protected override JsonNode ToNode()
        {
            JsonNode bodyNode = this.body.ToNode();

            JsonObject definitionsNode = [];
            foreach (RuleDefinition definition in this.definitions)
            {
                JsonObject entry = new() { ["body"] = definition.Body.ToNode() };
                if (definition.Parameters.Count > 0)
                {
                    JsonArray parameters = [];
                    foreach (string parameter in definition.Parameters)
                    {
                        parameters.Add(ValueToNode(parameter));
                    }

                    entry["parameters"] = parameters;
                }

                // CollectProblems already reported a repeated name as DefinitionNameCollision before Compile or
                // ToJson ever renders this node, so overwriting here never silently hides one.
                definitionsNode[definition.Name] = entry;
            }

            bodyNode["definitions"] = definitionsNode;
            return bodyNode;
        }
    }
}
