namespace TruthWeaver.Compilation;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;

/// <summary>
/// Turns a raw <see cref="RuleNode"/> tree (produced identically by the DSL, JSON, and YAML front
/// ends) into a validated, immutable <see cref="Expression"/> tree plus diagnostics — the
/// Validate + Build stages of the compilation pipeline (ADR-0003). This is the single place that
/// resolves predicate names against a <see cref="PredicateRegistry{TContext}"/>, validates argument
/// schemas, and enforces the resource limits from <see cref="CompilerOptions"/>, so every front end
/// gets identical validation behavior for free.
/// </summary>
/// <typeparam name="TContext">The application context type predicates in <see cref="PredicateRegistry{TContext}"/> read from.</typeparam>
internal sealed class RuleNodeCompiler<TContext>
{
    private readonly PredicateRegistry<TContext> registry;
    private readonly CompilerOptions options;
    private readonly List<Diagnostic> diagnostics = [];
    private int nodeCount;
    private bool nodeLimitReported;

    private RuleNodeCompiler(PredicateRegistry<TContext> registry, CompilerOptions options)
    {
        this.registry = registry;
        this.options = options;
    }

    /// <summary>Validates and builds an expression tree from a raw parse tree.</summary>
    /// <param name="root">The raw parse tree root.</param>
    /// <param name="registry">The predicate registry to resolve term names against.</param>
    /// <param name="options">The compiler's resource limits and mode.</param>
    /// <returns>
    /// The built tree (or <see langword="null"/> if any <see cref="DiagnosticSeverity.Error"/>
    /// diagnostic was produced) and every diagnostic raised while validating.
    /// </returns>
    public static (Expression? Tree, IReadOnlyList<Diagnostic> Diagnostics) Compile(
        RuleNode root,
        PredicateRegistry<TContext> registry,
        CompilerOptions options
    )
    {
        RuleNodeCompiler<TContext> compiler = new(registry, options);
        Expression tree = compiler.Build(root, depth: 1);
        bool hasErrors = compiler.diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
        return (hasErrors ? null : tree, compiler.diagnostics);
    }

    /// <summary>Gets the path of a property of a JSON/YAML node, or <see langword="null"/> for a DSL node.</summary>
    private static string? PathOf(RuleNode node, string property)
    {
        return node.Path is null ? null : TreePath.Property(node.Path, property);
    }

    /// <summary>Phrases an operand count for a diagnostic's expected/found pair: <c>1 operand</c>, <c>3 operands</c>.</summary>
    private static string CountText(int count)
    {
        return count == 1 ? "1 operand" : $"{count} operands";
    }

    /// <summary>
    /// Looks up an operator's arity in the operator table, the one place that states how many operands it takes, so
    /// the DSL, JSON, YAML and the builder agree. A missing entry is a programming error: the table completeness
    /// test pins one definition per operator.
    /// </summary>
    private static OperatorDefinition ArityOf(string opName)
    {
        return OperatorDefinitions.TryGet(opName, out OperatorDefinition? definition)
            ? definition
            : throw new InvalidOperationException($"No operator definition for '{opName}'.");
    }

    /// <summary>Tells whether <paramref name="count"/> lies within the operator's <c>MinOperands</c> to <c>MaxOperands</c>.</summary>
    private static bool AcceptsCount(OperatorDefinition definition, int count)
    {
        return count >= definition.MinOperands && (definition.MaxOperands is not { } max || count <= max);
    }

    /// <summary>
    /// Phrases the operand count an operator accepts for a diagnostic's <c>Expected</c>: <c>2 operands</c> for a fixed
    /// arity, <c>at least 2 operands</c> for an unbounded one.
    /// </summary>
    private static string ExpectedCountText(OperatorDefinition definition)
    {
        return definition.MaxOperands == definition.MinOperands
            ? CountText(definition.MinOperands)
            : $"at least {CountText(definition.MinOperands)}";
    }

    private static DiagnosticSuggestion NestingHint(string name)
    {
        return new DiagnosticSuggestion(
            DiagnosticSuggestionKind.Hint,
            $"Add parentheses (or nest {name} nodes) to say how the chained operands group."
        );
    }

    /// <summary>Names the form a raw literal was written in, for an argument-type mismatch's <c>Found</c>.</summary>
    private static string DescribeLiteral(RawLiteral literal)
    {
        return literal.Form switch
        {
            RawLiteralForm.QuotedString => $"the string \"{literal.Text}\"",
            RawLiteralForm.Number => $"the number {literal.Text}",
            RawLiteralForm.Boolean => $"the boolean {(literal.BooleanValue ? "true" : "false")}",
            RawLiteralForm.Variable => "a variable reference",
            _ => "a list",
        };
    }

    private static TermIdentity BuildUnknownIdentity(TermNode node)
    {
        List<KeyValuePair<string, LiteralValue>> args =
        [
            .. node
                .Arguments.Where(a => a.Value.Form != RawLiteralForm.Variable)
                .Select(a => new KeyValuePair<string, LiteralValue>(a.Name, LiteralConversion.Guess(a.Value))),
        ];
        List<KeyValuePair<string, VariableReference>> variables =
        [
            .. node
                .Arguments.Where(a => a.Value.Form == RawLiteralForm.Variable)
                .Select(a => new KeyValuePair<string, VariableReference>(
                    a.Name,
                    new VariableReference(a.Value.Text ?? string.Empty, a.Value.Query ?? string.Empty)
                )),
        ];
        return new TermIdentity(node.PredicateName, args, variables);
    }

    /// <summary>
    /// The threshold values that would make a comparison structurally trivial (always true or always
    /// false regardless of what the operands evaluate to) for a given operand count — e.g.
    /// <c>AtLeast(0, ...)</c> is always true, <c>AtLeast(n + 1, ...)</c> is always false. Rejecting
    /// these catches an authoring mistake at compile time rather than silently accepting a constant
    /// rule, the same rationale ticket 09 applied to the original <c>AtLeast</c> operator.
    /// </summary>
    private static (int MinK, int MaxK) ValidThresholdRange(ThresholdComparison comparison, int operandCount)
    {
        return comparison switch
        {
            ThresholdComparison.AtLeast => (1, operandCount),
            ThresholdComparison.AtMost => (0, operandCount - 1),
            ThresholdComparison.GreaterThan => (0, operandCount - 1),
            ThresholdComparison.LessThan => (1, operandCount),
            ThresholdComparison.Exactly => (0, operandCount),
            _ => throw new InvalidOperationException($"Unhandled threshold comparison '{comparison}'."),
        };
    }

    private Expression Build(RuleNode node, int depth)
    {
        this.nodeCount++;
        if (this.nodeCount > this.options.MaxNodeCount)
        {
            if (!this.nodeLimitReported)
            {
                this.nodeLimitReported = true;
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MaxNodeCountExceeded,
                        $"Rule exceeds the maximum node count of {this.options.MaxNodeCount}.",
                        node.Span,
                        expected: $"at most {this.options.MaxNodeCount} nodes",
                        found: "more nodes than that",
                        path: node.Path
                    )
                );
            }

            return FailedNode.Placeholder;
        }

        if (depth > this.options.MaxDepth)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MaxDepthExceeded,
                    $"Rule exceeds the maximum tree depth of {this.options.MaxDepth}.",
                    node.Span,
                    expected: $"nesting at most {this.options.MaxDepth} deep",
                    found: "deeper nesting",
                    path: node.Path
                )
            );
            return FailedNode.Placeholder;
        }

        return node switch
        {
            ConstantNode c => new ConstantExpression(c.Value),
            ErrorNode => FailedNode.Placeholder,
            TermNode t => this.BuildTerm(t),
            NotNode n => new NotExpression(this.Build(n.Operand, depth + 1)),
            AndNode a => this.BuildVariadic(
                a.Operands,
                depth,
                a,
                "And",
                operands => new AndExpression(new EquatableArray<Expression>(operands))
            ),
            OrNode o => this.BuildVariadic(
                o.Operands,
                depth,
                o,
                "Or",
                operands => new OrExpression(new EquatableArray<Expression>(operands))
            ),
            XorNode x => this.BuildXor(x, depth),
            EquivalentNode eq => this.BuildEquivalent(eq, depth),
            ImpliesNode i => this.BuildImplies(i, depth),
            NandNode nd => this.BuildNegatedBinary(nd.Operands, nd, "NAND", "Nand", depth, (l, r) => new NandExpression(l, r)),
            NorNode nr => this.BuildNegatedBinary(nr.Operands, nr, "NOR", "Nor", depth, (l, r) => new NorExpression(l, r)),
            ParityNode nx => this.BuildVariadic(
                nx.Operands,
                depth,
                nx,
                "Parity",
                operands => new ParityExpression(new EquatableArray<Expression>(operands))
            ),
            AnyNode an => this.BuildVariadic(
                an.Operands,
                depth,
                an,
                "Any",
                operands => new AnyExpression(new EquatableArray<Expression>(operands))
            ),
            AllNode al => this.BuildVariadic(
                al.Operands,
                depth,
                al,
                "All",
                operands => new AllExpression(new EquatableArray<Expression>(operands))
            ),
            NoneNode no => this.BuildVariadic(
                no.Operands,
                depth,
                no,
                "None",
                operands => new NoneExpression(new EquatableArray<Expression>(operands))
            ),
            ExactlyOneNode e => this.BuildVariadic(
                e.Operands,
                depth,
                e,
                "ExactlyOne",
                operands => new ExactlyOneExpression(new EquatableArray<Expression>(operands))
            ),
            ThresholdNode th => this.BuildThreshold(th, depth),
            BetweenNode bt => this.BuildBetween(bt, depth),
            CoalesceNode co => this.BuildVariadic(
                co.Operands,
                depth,
                co,
                "Coalesce",
                operands => new CoalesceExpression(new EquatableArray<Expression>(operands))
            ),
            IfNode ifNode => this.BuildIf(ifNode, depth),
            InspectionNode ins => this.BuildInspection(ins, depth),
            _ => throw new InvalidOperationException($"Unhandled rule node type '{node.GetType()}'."),
        };
    }

    /// <summary>Builds <c>If(condition, whenTrue, whenFalse)</c>; anything but exactly three operands is a <see cref="DiagnosticCodes.InfixArityViolation"/>.</summary>
    private Expression BuildIf(IfNode node, int depth)
    {
        OperatorDefinition arity = ArityOf("If");
        if (!AcceptsCount(arity, node.Operands.Count))
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    $"If requires exactly {arity.MinOperands} operands (condition, whenTrue, whenFalse) but found {node.Operands.Count}.",
                    node.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(node.Operands.Count),
                    path: PathOf(node, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        return new IfExpression(
            this.Build(node.Operands[0], depth + 1),
            this.Build(node.Operands[1], depth + 1),
            this.Build(node.Operands[2], depth + 1)
        );
    }

    /// <summary>Builds an inspection (<c>IsTrue</c>/<c>IsFalse</c>/<c>IsUnknown</c>/<c>IsKnown</c>); anything but one operand is a <see cref="DiagnosticCodes.InfixArityViolation"/>.</summary>
    private Expression BuildInspection(InspectionNode node, int depth)
    {
        OperatorDefinition arity = ArityOf(node.Kind.ToString());
        if (!AcceptsCount(arity, node.Operands.Count))
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    $"{node.Kind} requires exactly {CountText(arity.MinOperands)} but found {node.Operands.Count}.",
                    node.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(node.Operands.Count),
                    path: PathOf(node, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        return new InspectionExpression(node.Kind, this.Build(node.Operands[0], depth + 1));
    }

    private Expression BuildVariadic(
        IReadOnlyList<RuleNode> operands,
        int depth,
        RuleNode owner,
        string opName,
        Func<IReadOnlyList<Expression>, Expression> construct
    )
    {
        OperatorDefinition arity = ArityOf(opName);
        if (!AcceptsCount(arity, operands.Count))
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    $"This operator requires {ExpectedCountText(arity)} but found {operands.Count}.",
                    owner.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(operands.Count),
                    path: PathOf(owner, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        List<Expression> built = [with(operands.Count)];
        foreach (RuleNode operand in operands)
        {
            built.Add(this.Build(operand, depth + 1));
        }

        return construct(built);
    }

    private Expression BuildXor(XorNode node, int depth)
    {
        OperatorDefinition arity = ArityOf("Xor");
        if (!AcceptsCount(arity, node.Operands.Count))
        {
            string message =
                $"XOR is binary only; found {node.Operands.Count} operands. "
                + "Use PARITY(...) for n-ary parity (an odd number of True operands) or ExactlyOne(...) for n-ary 'exactly one'.";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    message,
                    node.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(node.Operands.Count),
                    suggestion: new DiagnosticSuggestion(
                        DiagnosticSuggestionKind.Hint,
                        "Use PARITY(...) for n-ary parity (an odd number of True operands) or ExactlyOne(...) for n-ary 'exactly one'."
                    ),
                    path: PathOf(node, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        Expression left = this.Build(node.Operands[0], depth + 1);
        Expression right = this.Build(node.Operands[1], depth + 1);
        return new XorExpression(left, right);
    }

    private Expression BuildEquivalent(EquivalentNode node, int depth)
    {
        OperatorDefinition arity = ArityOf("Equivalent");
        if (!AcceptsCount(arity, node.Operands.Count))
        {
            string message =
                $"EQUIVALENT is binary only; found {node.Operands.Count} operands. "
                + "Add parentheses (or nest EQUIVALENT nodes) to say how chained equivalences group.";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    message,
                    node.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(node.Operands.Count),
                    suggestion: NestingHint("EQUIVALENT"),
                    path: PathOf(node, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        Expression left = this.Build(node.Operands[0], depth + 1);
        Expression right = this.Build(node.Operands[1], depth + 1);
        return new EquivalentExpression(left, right);
    }

    /// <summary>
    /// Builds a strictly binary infix operator (<c>NAND</c>/<c>NOR</c>). Anything but two operands (a DSL chain or a
    /// malformed JSON/YAML node) is an <see cref="DiagnosticCodes.InfixArityViolation"/> with a parentheses hint.
    /// </summary>
    private Expression BuildNegatedBinary(
        IReadOnlyList<RuleNode> operands,
        RuleNode owner,
        string name,
        string opName,
        int depth,
        Func<Expression, Expression, Expression> construct
    )
    {
        OperatorDefinition arity = ArityOf(opName);
        if (!AcceptsCount(arity, operands.Count))
        {
            string message =
                $"{name} is binary only; found {operands.Count} operands. "
                + $"Add parentheses (or nest {name} nodes) to say how chained operations group.";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    message,
                    owner.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(operands.Count),
                    suggestion: NestingHint(name),
                    path: PathOf(owner, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        Expression left = this.Build(operands[0], depth + 1);
        Expression right = this.Build(operands[1], depth + 1);
        return construct(left, right);
    }

    private Expression BuildImplies(ImpliesNode node, int depth)
    {
        OperatorDefinition arity = ArityOf("Implies");
        if (!AcceptsCount(arity, node.Operands.Count))
        {
            string message =
                $"IMPLIES is binary only; found {node.Operands.Count} operands. "
                + "Add parentheses (or nest IMPLIES nodes) to say how chained implications group.";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    message,
                    node.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(node.Operands.Count),
                    suggestion: NestingHint("IMPLIES"),
                    path: PathOf(node, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        Expression antecedent = this.Build(node.Operands[0], depth + 1);
        Expression consequent = this.Build(node.Operands[1], depth + 1);
        return new ImpliesExpression(antecedent, consequent);
    }

    private Expression BuildThreshold(ThresholdNode node, int depth)
    {
        // The operand count comes from the operator table, but the table cannot state the threshold k range: it depends
        // on the comparison and on the operand count together, so ValidThresholdRange keeps that rule. It runs first so
        // an unknown comparison is rejected by it before any table lookup.
        (int minK, int maxK) = ValidThresholdRange(node.Comparison, node.Operands.Count);
        OperatorDefinition arity = ArityOf(node.Comparison.ToString());
        if (!AcceptsCount(arity, node.Operands.Count))
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    $"{node.Comparison} requires at least {(arity.MinOperands == 1 ? "one operand" : CountText(arity.MinOperands))}.",
                    node.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(0),
                    path: PathOf(node, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        if (node.K < minK || node.K > maxK)
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InvalidThresholdValue,
                    $"{node.Comparison}'s threshold k={node.K} must satisfy {minK} <= k <= {maxK} for {node.Operands.Count} operand(s) (any value outside that range makes the result a structural constant).",
                    node.Span,
                    expected: $"{minK} <= k <= {maxK}",
                    found: $"k={node.K}",
                    path: PathOf(node, "k")
                )
            );
            return FailedNode.Placeholder;
        }

        List<Expression> built = [with(node.Operands.Count)];
        foreach (RuleNode operand in node.Operands)
        {
            built.Add(this.Build(operand, depth + 1));
        }

        return new ThresholdExpression(node.Comparison, node.K, new EquatableArray<Expression>(built));
    }

    /// <summary>
    /// Builds <c>BETWEEN(min, max, ...)</c>. Like the threshold family it rejects bounds that would make the result
    /// a structural constant (<c>0 &lt;= min &lt;= max &lt;= n</c>; the full range <c>0..n</c> is always True), and
    /// like <c>ANY</c>/<c>ALL</c>/<c>ExactlyOne</c> it needs at least two operands (ADR-0005 decision 13).
    /// </summary>
    private Expression BuildBetween(BetweenNode node, int depth)
    {
        int operandCount = node.Operands.Count;
        OperatorDefinition arity = ArityOf("Between");
        if (!AcceptsCount(arity, operandCount))
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InfixArityViolation,
                    $"BETWEEN requires {ExpectedCountText(arity)} but found {operandCount}.",
                    node.Span,
                    expected: ExpectedCountText(arity),
                    found: CountText(operandCount),
                    path: PathOf(node, "operands")
                )
            );
            return FailedNode.Placeholder;
        }

        bool inRange = node.Min >= 0 && node.Min <= node.Max && node.Max <= operandCount;
        if (!inRange || (node.Min == 0 && node.Max == operandCount))
        {
            string reason = inRange
                ? $"the full range 0..{operandCount} is always True (a structural constant)"
                : $"it must satisfy 0 <= min <= max <= {operandCount} for {operandCount} operand(s)";
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InvalidThresholdValue,
                    $"BETWEEN's bounds min={node.Min}, max={node.Max} are invalid: {reason}.",
                    node.Span,
                    expected: $"0 <= min <= max <= {operandCount}, excluding the full range",
                    found: $"min={node.Min}, max={node.Max}",
                    path: node.Path
                )
            );
            return FailedNode.Placeholder;
        }

        List<Expression> built = [with(operandCount)];
        foreach (RuleNode operand in node.Operands)
        {
            built.Add(this.Build(operand, depth + 1));
        }

        return new BetweenExpression(node.Min, node.Max, new EquatableArray<Expression>(built));
    }

    /// <summary>
    /// The names a misspelt predicate could have meant: the registered predicates, plus the DSL's operator words when the
    /// rule is DSL text (a JSON/YAML tree names operators in its own <c>op</c> field, so an operator word there would
    /// mislead).
    /// </summary>
    private IEnumerable<string> NamesToSuggest(RuleNode node)
    {
        return node.Path is null ? this.registry.Names.Concat(DslVocabulary.Keywords) : this.registry.Names;
    }

    private Expression BuildTerm(TermNode node)
    {
        if (!this.registry.TryGet(node.PredicateName, out PredicateDescriptor<TContext>? descriptor))
        {
            if (this.options.Mode == CompilationMode.Lenient)
            {
                return new TermExpression(BuildUnknownIdentity(node), IsUnknownPredicate: true);
            }

            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.UnknownPredicate,
                    $"No predicate named '{node.PredicateName}' is registered.",
                    node.Span,
                    expected: "a registered predicate name or an operator",
                    found: $"'{node.PredicateName}'",
                    suggestion: NameSuggester.Suggest(node.PredicateName, this.NamesToSuggest(node)),
                    path: PathOf(node, "predicate")
                )
            );
            return FailedNode.Placeholder;
        }

        PredicateSchema schema = descriptor.Schema;
        int diagnosticsBefore = this.diagnostics.Count;
        Dictionary<string, LiteralValue> resolvedArgs = [];
        Dictionary<string, VariableReference> resolvedVariables = [];

        // Argument names match ignoring case, like predicate and operator names; the schema's spelling is the canonical one.
        HashSet<string> suppliedNames = [with(StringComparer.OrdinalIgnoreCase)];
        foreach (ArgumentNode arg in node.Arguments)
        {
            if (!suppliedNames.Add(arg.Name))
            {
                // A repeated name never wins silently: the author meant one value, and neither can be trusted.
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.DuplicateArgument,
                        $"Argument '{arg.Name}' of predicate '{schema.Name}' is given more than once.",
                        arg.Span,
                        expected: "each argument once",
                        found: $"'{arg.Name}' repeated",
                        suggestion: new DiagnosticSuggestion(
                            DiagnosticSuggestionKind.Hint,
                            $"Remove one '{arg.Name}' argument."
                        ),
                        path: arg.Path
                    )
                );
                continue;
            }

            PredicateArgumentSchema? argSchema = schema.Arguments.FirstOrDefault(a =>
                string.Equals(a.Name, arg.Name, StringComparison.OrdinalIgnoreCase)
            );
            if (argSchema is null)
            {
                string[] declared = [.. schema.Arguments.Select(a => a.Name)];
                string expectedArguments = declared.Length == 0 ? "no arguments" : $"one of {string.Join(", ", declared)}";

                // A near-miss gets a "did you mean"; anything else (typically an argument the predicate has
                // retired, such as EqualsConfigurable's former culture) gets advice naming it, so the author
                // knows deleting it is the fix rather than renaming it.
                DiagnosticSuggestion suggestion =
                    NameSuggester.Suggest(arg.Name, declared)
                    ?? new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, $"Remove the argument '{arg.Name}'.");
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.UnknownArgument,
                        $"Predicate '{schema.Name}' does not declare an argument named '{arg.Name}'.",
                        arg.Span,
                        expected: expectedArguments,
                        found: $"'{arg.Name}'",
                        suggestion: suggestion,
                        path: arg.Path
                    )
                );
                continue;
            }

            if (arg.Value.Form == RawLiteralForm.Variable)
            {
                // A variable's value, and so whether it fits the argument's kind, is known only at evaluation time
                // (ADR-0006 decision 2); compilation checks just that the source name was declared.
                if (this.CheckSourceDeclared(arg) && this.CheckQuery(arg))
                {
                    resolvedVariables[argSchema.Name] = new VariableReference(
                        arg.Value.Text ?? string.Empty,
                        arg.Value.Query ?? string.Empty
                    );
                }

                continue;
            }

            if (!LiteralConversion.TryConvert(arg.Value, argSchema.Type, out LiteralValue value))
            {
                // Offset-less date-time text gets the one-line fix appended; any other mismatch keeps the plain message.
                string offsetFix = LiteralConversion.IsMissingOffset(arg.Value, argSchema.Type)
                    ? $" {DateTimeText.OffsetFix}"
                    : string.Empty;
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.ArgumentTypeMismatch,
                        $"Argument '{arg.Name}' of predicate '{schema.Name}' must be of kind '{argSchema.Type}'.{offsetFix}",
                        arg.Value.Span,
                        expected: $"a value of kind '{argSchema.Type}'",
                        found: DescribeLiteral(arg.Value),
                        path: arg.Path
                    )
                );
                continue;
            }

            resolvedArgs[argSchema.Name] = value;
        }

        foreach (PredicateArgumentSchema argSchema in schema.Arguments)
        {
            if (resolvedArgs.ContainsKey(argSchema.Name) || suppliedNames.Contains(argSchema.Name))
            {
                continue;
            }

            if (argSchema.Required)
            {
                string supplied =
                    suppliedNames.Count == 0
                        ? "no arguments"
                        : $"only {string.Join(", ", suppliedNames.Order(StringComparer.Ordinal).Select(n => $"'{n}'"))}";
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MissingArgument,
                        $"Predicate '{schema.Name}' requires argument '{argSchema.Name}'.",
                        node.Span,
                        expected: $"argument '{argSchema.Name}'",
                        found: supplied,
                        path: node.Path
                    )
                );
            }
            else if (argSchema.Default is { } defaultValue)
            {
                resolvedArgs[argSchema.Name] = defaultValue;
            }
        }

        // The schema's own check over argument values runs only once every argument is present and of its kind, so it
        // never reports on top of (or because of) an argument error already raised for this call.
        if (schema.ArgumentValidator is { } validator && this.diagnostics.Count == diagnosticsBefore)
        {
            this.CheckArgumentValues(node, schema, validator, resolvedArgs);
        }

        // Reported after the argument checks so the warning never trips the "no new diagnostics" gate above.
        if (schema.Deprecation is { } deprecation)
        {
            this.ReportDeprecated(node, schema, deprecation);
        }

        TermIdentity identity = new(
            schema.Name,
            [.. resolvedArgs.Select(kv => new KeyValuePair<string, LiteralValue>(kv.Key, kv.Value))],
            [.. resolvedVariables.Select(kv => new KeyValuePair<string, VariableReference>(kv.Key, kv.Value))]
        );
        return new TermExpression(identity);
    }

    /// <summary>
    /// Reports <c>TRE0027</c>, a warning at the predicate call, because the schema is marked deprecated. The replacement,
    /// when the marker names one, is both in the message and the suggestion. The rule still compiles.
    /// </summary>
    private void ReportDeprecated(TermNode node, PredicateSchema schema, PredicateDeprecation deprecation)
    {
        string message = $"Predicate '{schema.Name}' is deprecated.";
        DiagnosticSuggestion? suggestion = null;
        if (deprecation.ReplacedBy is { Length: > 0 } replacement)
        {
            message += $" Use '{replacement}' instead.";
            suggestion = new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, $"Use '{replacement}'.");
        }

        if (deprecation.Message is { Length: > 0 } extra)
        {
            message += $" {extra}";
        }

        this.diagnostics.Add(
            Diagnostic.Warning(DiagnosticCodes.DeprecatedPredicate, message, node.Span, suggestion: suggestion, path: node.Path)
        );
    }

    /// <summary>
    /// Reports <c>TRE0026</c>, one error per problem, at the predicate call when the schema's
    /// <see cref="PredicateSchema.ArgumentValidator"/> rejects the call's literal arguments. Variable arguments are not in
    /// <paramref name="literalArgs"/>, so the validator sees only values known at compile time.
    /// </summary>
    private void CheckArgumentValues(
        TermNode node,
        PredicateSchema schema,
        Func<PredicateArguments, IReadOnlyList<PredicateArgumentProblem>> validator,
        Dictionary<string, LiteralValue> literalArgs
    )
    {
        foreach (PredicateArgumentProblem problem in validator(new PredicateArguments(literalArgs)))
        {
            DiagnosticSuggestion? suggestion = problem.Suggestion is null
                ? null
                : new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, problem.Suggestion);
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.InvalidArgumentValue,
                    $"Predicate '{schema.Name}' has invalid argument values: {problem.Message}",
                    node.Span,
                    expected: problem.Expected,
                    found: problem.Found,
                    suggestion: suggestion,
                    path: node.Path
                )
            );
        }
    }

    /// <summary>
    /// Reports <c>TRE0025</c>, one diagnostic per problem, when the source's declared <see cref="IQueryValidator"/> finds the
    /// query of the variable reference in <paramref name="arg"/> malformed. A source declared without a validator is not checked.
    /// </summary>
    /// <returns><see langword="true"/> if the query is acceptable.</returns>
    private bool CheckQuery(ArgumentNode arg)
    {
        string source = arg.Value.Text ?? string.Empty;
        if (this.options.DataSources?[source] is not { } validator)
        {
            return true;
        }

        string query = arg.Value.Query ?? string.Empty;
        IReadOnlyList<QueryProblem> problems = validator.Validate(query);

        // A JSON or YAML reference points at its own "query" member; a DSL reference at the query string within from(...).
        VariableParts? parts = arg.Value.Parts;
        SourceSpan span = parts is null ? arg.Value.Span : parts.QuerySpan;
        string? path = parts?.QueryPath ?? arg.Path;
        foreach (QueryProblem problem in problems)
        {
            string where = problem.Position is { } position ? $" (at position {position} of the query)" : string.Empty;
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.MalformedDataQuery,
                    $"The query for data source '{source}' is not valid: {problem.Message}{where}",
                    span,
                    expected: $"a query valid for data source '{source}'",
                    found: $"\"{query}\"",
                    path: path
                )
            );
        }

        return problems.Count == 0;
    }

    /// <summary>
    /// Reports <c>TRE0024</c>, with a "did you mean" when a declared name is close, if the variable reference in
    /// <paramref name="arg"/> names a data source that was not declared in <see cref="CompilerOptions.DataSources"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the source is declared.</returns>
    private bool CheckSourceDeclared(ArgumentNode arg)
    {
        string source = arg.Value.Text ?? string.Empty;
        DataSourceDeclarations? declared = this.options.DataSources;
        if (declared?.Contains(source) == true)
        {
            return true;
        }

        IReadOnlyCollection<string> names = declared?.Names ?? [];
        string expected =
            names.Count == 0
                ? "a declared data source name"
                : $"one of {string.Join(", ", names.Order(StringComparer.Ordinal))}";
        string hint =
            names.Count == 0
                ? "No data sources are declared; add the name to CompilerOptions.DataSources."
                : $"Declare '{source}' in CompilerOptions.DataSources, or use one of: {string.Join(", ", names.Order(StringComparer.Ordinal).Select(n => $"'{n}'"))}.";
        DiagnosticSuggestion suggestion =
            NameSuggester.Suggest(source, names) ?? new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, hint);

        // A JSON or YAML reference points at its own "from" member; a DSL reference at the whole from(...).
        VariableParts? parts = arg.Value.Parts;
        SourceSpan span = parts?.SourcePath is not null ? parts.SourceSpan : arg.Value.Span;
        string? path = parts?.SourcePath ?? arg.Path;
        this.diagnostics.Add(
            Diagnostic.Error(
                DiagnosticCodes.UndeclaredDataSource,
                $"No data source named '{source}' is declared.",
                span,
                expected: expected,
                found: $"'{source}'",
                suggestion: suggestion,
                path: path
            )
        );
        return false;
    }
}
