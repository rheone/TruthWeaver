namespace TruthWeaver.Compilation;

using TruthWeaver.Diagnostics;
using TruthWeaver.Parsing;
using TruthWeaver.Registry;

/// <summary>
/// Expands a rule document's <c>let</c> definitions at compile time (ADR-0008 decision 1): every <see cref="UseNode"/>
/// is replaced by its definition's body with parameters substituted, before the raw tree ever reaches
/// <see cref="RuleNodeCompiler{TContext}"/>. The expanded tree contains no <see cref="DefinitionNode"/>,
/// <see cref="UseNode"/> or <see cref="ParameterReferenceNode"/>. Only the primitive and derived operator nodes
/// <c>RuleNodeCompiler</c> already understands, so the evaluator, BDD analyzer, rewrites and every printer need no
/// definitions-aware case.
/// </summary>
internal static class DefinitionExpander
{
    /// <summary>Expands every <c>let</c> use in <paramref name="document"/>.</summary>
    /// <param name="document">The parsed document, with its declarations and its single value expression.</param>
    /// <param name="registry">The predicate registry, consulted only to detect a definition name that collides with a registered predicate.</param>
    /// <param name="options">The compiler options, whose <see cref="CompilerOptions.MaxDefinitionDepth"/> and <see cref="CompilerOptions.Lints"/> this expansion enforces.</param>
    /// <returns>
    /// The expanded body (with an <see cref="ErrorNode"/> standing in for any use that could not be expanded) and every
    /// diagnostic raised while expanding.
    /// </returns>
    public static (RuleNode Body, IReadOnlyList<Diagnostic> Diagnostics) Expand<TContext>(
        RuleDocumentNode document,
        PredicateRegistry<TContext> registry,
        CompilerOptions options
    )
    {
        List<Diagnostic> diagnostics = [];
        Dictionary<string, DefinitionNode> definitionsByName = [with(StringComparer.OrdinalIgnoreCase)];
        HashSet<string> unused = [with(StringComparer.OrdinalIgnoreCase)];

        foreach (DefinitionNode definition in document.Definitions)
        {
            if (definitionsByName.ContainsKey(definition.Name))
            {
                diagnostics.Add(
                    CollisionDiagnostic(definition, "a definition already declared with this name in this document")
                );
                continue;
            }

            if (DslParser.IsReservedWord(definition.Name))
            {
                diagnostics.Add(CollisionDiagnostic(definition, "a reserved word"));
                continue;
            }

            if (registry.TryGet(definition.Name, out _))
            {
                diagnostics.Add(CollisionDiagnostic(definition, "a predicate name registered with the compiler"));
                continue;
            }

            if (DuplicateParameterName(definition.Parameters) is { } duplicateParameter)
            {
                diagnostics.Add(DuplicateParameterDiagnostic(definition, duplicateParameter));
                continue;
            }

            definitionsByName[definition.Name] = definition;
            unused.Add(definition.Name);
        }

        Expander expander = new(definitionsByName, options, diagnostics);
        RuleNode body = expander.ExpandNode(document.Body, parameters: null, chain: [], unused);

        if (options.Lints.HasFlag(LintRules.UnusedDefinition))
        {
            // Report in declaration order, not hash-set order, so the findings read the way the document was written.
            IEnumerable<DefinitionNode> stillUnused = document.Definitions.Where(d =>
                unused.Contains(d.Name) && definitionsByName.ContainsKey(d.Name)
            );
            foreach (DefinitionNode definition in stillUnused)
            {
                diagnostics.Add(
                    Diagnostic.Info(
                        DiagnosticCodes.UnusedDefinition,
                        $"Definition '{definition.Name}' is declared but never used.",
                        definition.NameSpan,
                        path: definition.NamePath
                    )
                );
            }
        }

        return (body, diagnostics);
    }

    // The path is the definitions entry's own location in the JSON/YAML document (null for a DSL definition, where
    // NameSpan alone already locates it); RuleCompiler resolves it to a real span the same way it resolves any other
    // path-located diagnostic, mapping an ADR-0008 guardrail to the document location the format already reports.
    private static Diagnostic CollisionDiagnostic(DefinitionNode definition, string clash)
    {
        return Diagnostic.Error(
            DiagnosticCodes.DefinitionNameCollision,
            $"Definition name '{definition.Name}' clashes with {clash}.",
            definition.NameSpan,
            expected: "a definition name not already used in this document, not a reserved word, and not a registered predicate name",
            found: $"'{definition.Name}', which clashes with {clash}",
            path: definition.NamePath
        );
    }

    // Returns the first parameter name (as written) that repeats an earlier one in the same list, compared
    // case-insensitively, or null when every name is distinct. ExpandUse binds a use's arguments into a
    // case-insensitive dictionary keyed by parameter name (ticket 20): without this check, a repeated name such as
    // `let both(x, X) = x AND X;` would let the later parameter silently overwrite the earlier one's binding,
    // dropping an argument with no diagnostic, the same mistake TRE0032 (DuplicateArgument) already rejects for a
    // repeated predicate argument name.
    private static string? DuplicateParameterName(IReadOnlyList<string> parameters)
    {
        return parameters
            .Where((name, index) => parameters.Take(index).Contains(name, StringComparer.OrdinalIgnoreCase))
            .FirstOrDefault();
    }

    // The span and path are the definition's own declaration, not any particular use: a repeated parameter name is
    // wrong about the declaration itself, independent of whether (or how) the definition is ever used.
    private static Diagnostic DuplicateParameterDiagnostic(DefinitionNode definition, string duplicateParameter)
    {
        return Diagnostic.Error(
            DiagnosticCodes.DuplicateDefinitionParameter,
            $"Definition '{definition.Name}' repeats parameter '{duplicateParameter}'.",
            definition.NameSpan,
            expected: "a parameter list with no name repeated (compared case-insensitively)",
            found: $"'{duplicateParameter}' repeated",
            suggestion: new DiagnosticSuggestion(
                DiagnosticSuggestionKind.Hint,
                $"Rename or remove one of the '{duplicateParameter}' parameters."
            ),
            path: definition.NamePath
        );
    }

    /// <summary>Carries one expansion pass's immutable inputs (the declared definitions and options) and mutable diagnostics sink.</summary>
    private sealed class Expander(
        Dictionary<string, DefinitionNode> definitions,
        CompilerOptions options,
        List<Diagnostic> diagnostics
    )
    {
        // Counts every node ExpandNode produces (or, past the limit, merely attempts), mirroring
        // RuleNodeCompiler.Build's own nodeCount. Without this, a shallow but high-fanout chain of 'let' uses (each
        // level AND-ing many uses of the one below) expands to fanout^depth nodes before Build ever gets to apply
        // MaxNodeCount, exhausting memory on a tiny document (ticket 04). Checking it on entry to every ExpandNode
        // call, before any recursion for that node, keeps the work past the limit to O(1) per call instead of
        // letting it keep recursing into UseNode bodies: once nodeCount is already over budget, every call this one
        // makes to expand an operand or a use's body also trips the same check immediately and returns without
        // expanding further.
        private int nodeCount;
        private bool nodeLimitReported;

        /// <summary>Recursively expands one raw node, substituting parameter references and expanding uses as they are found.</summary>
        /// <param name="node">The node to expand.</param>
        /// <param name="parameters">The current definition body's parameter bindings (name to already-expanded argument), or <see langword="null"/> outside one.</param>
        /// <param name="chain">The names of the definitions currently being expanded through, outermost first, for cycle and depth detection.</param>
        /// <param name="unused">The set of declared names not yet seen in a use; a use removes its own name.</param>
        public RuleNode ExpandNode(
            RuleNode node,
            IReadOnlyDictionary<string, RuleNode>? parameters,
            List<string> chain,
            HashSet<string> unused
        )
        {
            this.nodeCount++;
            if (this.nodeCount > options.MaxNodeCount)
            {
                if (!this.nodeLimitReported)
                {
                    this.nodeLimitReported = true;
                    diagnostics.Add(
                        Diagnostic.Error(
                            DiagnosticCodes.MaxNodeCountExceeded,
                            $"Rule exceeds the maximum node count of {options.MaxNodeCount}.",
                            node.Span,
                            expected: $"at most {options.MaxNodeCount} nodes",
                            found: "more nodes than that",
                            path: node.Path
                        )
                    );
                }

                // A placeholder, not the real expansion: once over budget, nothing further is expanded, so the
                // returned tree is incomplete. RuleNodeCompiler.Build never runs on it for real (the error
                // diagnostic above makes Compile return a null tree), so the placeholder's shape does not matter.
                return new ErrorNode(node.Span) with
                {
                    Path = node.Path,
                };
            }

            return node switch
            {
                // Only ever produced inside the owning definition's own body, so parameters is never null here in a
                // well-formed tree; the fallback keeps this method total rather than throwing on a malformed one.
                ParameterReferenceNode reference => parameters is not null
                && parameters.TryGetValue(reference.Name, out RuleNode? bound)
                    ? bound
                    : node,
                UseNode use => this.ExpandUse(use, parameters, chain, unused),

                // A term's own argument values are RawLiteral, not RuleNode: parameters substitute only at the
                // operand level (ADR-0008 decision 5), so a term's arguments never contain a use or parameter.
                ConstantNode or TermNode or ErrorNode => node,
                NotNode n => Carry(n, new NotNode(this.ExpandNode(n.Operand, parameters, chain, unused), n.Span)),
                AndNode a => Carry(a, new AndNode(this.ExpandAll(a.Operands, parameters, chain, unused), a.Span)),
                OrNode o => Carry(o, new OrNode(this.ExpandAll(o.Operands, parameters, chain, unused), o.Span)),
                XorNode x => Carry(x, new XorNode(this.ExpandAll(x.Operands, parameters, chain, unused), x.Span)),
                EquivalentNode eq => Carry(
                    eq,
                    new EquivalentNode(this.ExpandAll(eq.Operands, parameters, chain, unused), eq.Span)
                ),
                ImpliesNode im => Carry(im, new ImpliesNode(this.ExpandAll(im.Operands, parameters, chain, unused), im.Span)),
                NandNode nd => Carry(nd, new NandNode(this.ExpandAll(nd.Operands, parameters, chain, unused), nd.Span)),
                NorNode nr => Carry(nr, new NorNode(this.ExpandAll(nr.Operands, parameters, chain, unused), nr.Span)),
                ParityNode pr => Carry(pr, new ParityNode(this.ExpandAll(pr.Operands, parameters, chain, unused), pr.Span)),
                AnyNode an => Carry(an, new AnyNode(this.ExpandAll(an.Operands, parameters, chain, unused), an.Span)),
                AllNode al => Carry(al, new AllNode(this.ExpandAll(al.Operands, parameters, chain, unused), al.Span)),
                NoneNode none => Carry(none, new NoneNode(this.ExpandAll(none.Operands, parameters, chain, unused), none.Span)),
                ExactlyOneNode eo => Carry(
                    eo,
                    new ExactlyOneNode(this.ExpandAll(eo.Operands, parameters, chain, unused), eo.Span)
                ),
                ThresholdNode th => Carry(
                    th,
                    new ThresholdNode(th.Comparison, th.K, this.ExpandAll(th.Operands, parameters, chain, unused), th.Span)
                ),
                BetweenNode bt => Carry(
                    bt,
                    new BetweenNode(bt.Min, bt.Max, this.ExpandAll(bt.Operands, parameters, chain, unused), bt.Span)
                ),
                CoalesceNode co => Carry(co, new CoalesceNode(this.ExpandAll(co.Operands, parameters, chain, unused), co.Span)),
                IfNode ifNode => Carry(
                    ifNode,
                    new IfNode(this.ExpandAll(ifNode.Operands, parameters, chain, unused), ifNode.Span)
                ),
                InspectionNode ins => Carry(
                    ins,
                    new InspectionNode(ins.Kind, this.ExpandAll(ins.Operands, parameters, chain, unused), ins.Span)
                ),

                // The head's variable and collection name are plain strings; only the body holds uses (ADR-0010
                // decision 10). Expanding the body here puts a definition's terms in the quantifier's element scope, where
                // RuleNodeCompiler resolves them against that collection's element registry.
                QuantifierNode q => Carry(
                    q,
                    new QuantifierNode(
                        q.Kind,
                        q.Variable,
                        q.CollectionName,
                        this.ExpandNode(q.Body, parameters, chain, unused),
                        q.Span
                    )
                ),
                _ => throw new InvalidOperationException(
                    $"Unhandled rule node type '{node.GetType()}' during definition expansion."
                ),
            };
        }

        /// <summary>Copies <paramref name="source"/>'s JSON/YAML path and note onto the freshly built <paramref name="result"/>.</summary>
        private static RuleNode Carry(RuleNode source, RuleNode result)
        {
            RuleNode carried = result with { Path = source.Path };
            carried.Note = source.Note;
            return carried;
        }

        private IReadOnlyList<RuleNode> ExpandAll(
            IReadOnlyList<RuleNode> nodes,
            IReadOnlyDictionary<string, RuleNode>? parameters,
            List<string> chain,
            HashSet<string> unused
        )
        {
            RuleNode[] expanded = new RuleNode[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                expanded[i] = this.ExpandNode(nodes[i], parameters, chain, unused);
            }

            return expanded;
        }

        private RuleNode ExpandUse(
            UseNode use,
            IReadOnlyDictionary<string, RuleNode>? parameters,
            List<string> chain,
            HashSet<string> unused
        )
        {
            // Arguments are expanded in the scope the use itself sits in (the caller's own parameters, if any), before
            // being substituted into the definition's body. A use never sees the callee's parameters.
            RuleNode[] arguments = new RuleNode[use.Arguments.Count];
            for (int i = 0; i < use.Arguments.Count; i++)
            {
                arguments[i] = this.ExpandNode(use.Arguments[i], parameters, chain, unused);
            }

            if (!definitions.TryGetValue(use.Name, out DefinitionNode? definition))
            {
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.UnknownDefinition,
                        $"'{use.Name}' is not a definition declared in this document.",
                        use.NameSpan,
                        expected: "a name declared with 'let' earlier in this document",
                        found: $"'{use.Name}'",
                        path: use.NamePath
                    )
                );
                return new ErrorNode(use.Span);
            }

            unused.Remove(use.Name);

            int cycleStart = chain.FindIndex(name => string.Equals(name, use.Name, StringComparison.OrdinalIgnoreCase));
            if (cycleStart >= 0)
            {
                List<string> cycle = [.. chain.Skip(cycleStart), use.Name];
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.DefinitionCycleDetected,
                        $"Definition cycle: {string.Join(" -> ", cycle)}.",
                        use.NameSpan,
                        expected: "no definition referencing itself through a chain of uses",
                        found: $"a cycle through {string.Join(", ", cycle)}",
                        path: use.NamePath
                    )
                );
                return new ErrorNode(use.Span);
            }

            if (use.Arguments.Count != definition.Parameters.Count)
            {
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.DefinitionArgumentCountMismatch,
                        $"'{use.Name}' takes {definition.Parameters.Count} argument(s) but this use supplies {use.Arguments.Count}.",
                        use.NameSpan,
                        expected: $"{definition.Parameters.Count} argument(s)",
                        found: $"{use.Arguments.Count} argument(s)",
                        path: use.NamePath
                    )
                );
                return new ErrorNode(use.Span);
            }

            if (chain.Count >= options.MaxDefinitionDepth)
            {
                diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MaxDefinitionDepthExceeded,
                        $"Expanding '{use.Name}' would nest more than {options.MaxDefinitionDepth} definitions deep.",
                        use.NameSpan,
                        expected: $"at most {options.MaxDefinitionDepth} definitions deep",
                        found: "a deeper chain of definitions",
                        path: use.NamePath
                    )
                );
                return new ErrorNode(use.Span);
            }

            Dictionary<string, RuleNode> bound = [with(StringComparer.OrdinalIgnoreCase)];
            for (int i = 0; i < definition.Parameters.Count; i++)
            {
                bound[definition.Parameters[i]] = arguments[i];
            }

            List<string> nextChain = [.. chain, use.Name];
            return this.ExpandNode(definition.Body, bound, nextChain, unused);
        }
    }
}
