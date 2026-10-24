namespace TruthWeaver.Parsing;

using System.Diagnostics.CodeAnalysis;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;

/// <summary>
/// Reads the flat, key-discriminated rule tree shape (ADR-0003) into the same raw <see cref="RuleNode"/> tree the DSL parser
/// produces, so every front end funnels through identical validation. A node is discriminated by which key it has:
/// <c>const</c>, <c>predicate</c> or <c>op</c>. The reader owns that dispatch, the op-name to node mapping, the <c>k</c>,
/// <c>min</c>, <c>max</c> and operand-count checks and the <c>Collapse</c>, <c>NXOR</c> and <c>Project</c> rejections; a
/// format contributes only an <see cref="ITreeNodeCursor"/> over its document model. Never throws for malformed input: it
/// reports diagnostics located by their path from the root (<c>$.operands[1].op</c>). A structural problem is <see cref="DiagnosticCodes.MalformedTree"/>; a bad count, bound or
/// operator name has the code the DSL gives it.
/// </summary>
internal sealed class TreeFormatReader
{
    private readonly TreeFormatVocabulary words;
    private readonly List<Diagnostic> diagnostics = [];

    // Guards ReadNode's recursion (operands, a use's arguments, a quantifier's body) against an unbounded JSON or
    // YAML tree: JsonDocument caps JSON nesting at 64 by default, but a caller that raises JsonDocumentOptions.MaxDepth
    // can still hand CompileJson(JsonElement) a deeper element, and nothing bounds a YamlNode graph at all. Without
    // this, a deeply nested document overflows the call stack with an uncatchable StackOverflowException before
    // RuleNodeCompiler.Build ever gets to apply CompilerOptions.MaxDepth to the finished tree.
    private readonly int maxDepth;

    // Set by ReadUse: unlike the DSL (where ParseUse is reachable only for a name already scanned as a 'let'
    // declaration, so a bare use never appears without a document), a JSON/YAML 'use' node is its own explicit shape,
    // legal even when the document declared no 'definitions' at all (ADR-0008 decision 3 says "wherever a tree node is
    // expected", not "only when a definitions section exists"). Read uses this, at no extra tree walk, to decide
    // whether the parsed tree still needs to go through DefinitionExpander: a use with nothing declared is an
    // UnknownDefinition diagnostic, not a node RuleNodeCompiler.Build would otherwise crash on.
    private bool sawUse;

    // The declared parameter names of the definition whose body is being read right now, or null outside one
    // (ReadDefinitionEntry sets and restores it around its own ReadNode call). A single mutable field, rather than
    // threading the scope through every recursive Read* method, is safe because definitions never nest (ADR-0008
    // decision 5): reading one body never re-enters ReadDefinitionEntry for another.
    private IReadOnlySet<string>? activeParameters;
    private int depth;
    private bool maxDepthReported;

    private TreeFormatReader(TreeFormatVocabulary words, int maxDepth)
    {
        this.words = words;
        this.maxDepth = maxDepth;
    }

    /// <summary>Reads a tree document into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="root">The cursor positioned on the document's root node.</param>
    /// <param name="words">The format's own words for diagnostics.</param>
    /// <param name="maxDepth">
    /// The nesting bound, matching <see cref="TruthWeaver.Compilation.CompilerOptions.MaxDepth"/>'s default.
    /// Exceeding it is reported as <see cref="DiagnosticCodes.MaxDepthExceeded"/>, the same code
    /// <c>RuleNodeCompiler.Build</c> reports for a built tree that is too deep.
    /// </param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> when the tree was malformed) and the diagnostics raised while reading.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Read(
        ITreeNodeCursor root,
        TreeFormatVocabulary words,
        int maxDepth = 32
    )
    {
        TreeFormatReader reader = new(words, maxDepth);

        // A 'definitions' section sits alongside the document's own root node (ADR-0008 decision 3), never on a
        // nested node, so it is recognized here, once, rather than as a case of the generic node reader.
        if (root.Shape == TreeNodeShape.Mapping && root.TryGetChild("definitions", out ITreeNodeCursor? definitionsNode))
        {
            IReadOnlyList<DefinitionNode>? definitions = reader.ReadDefinitions(
                definitionsNode,
                TreePath.Property(TreePath.Root, "definitions")
            );
            RuleNode? body = reader.ReadNode(new KeyExcludingCursor(root, "definitions"), TreePath.Root);
            if (definitions is null || body is null)
            {
                return (null, reader.diagnostics);
            }

            RuleNode document = new RuleDocumentNode(definitions, body, root.Span) { Path = TreePath.Root };
            return (document, reader.diagnostics);
        }

        RuleNode? node = reader.ReadNode(root, TreePath.Root);
        if (node is not null && reader.sawUse)
        {
            // No 'definitions' section, but at least one 'use' was still written: wrap with an empty definitions list
            // so DefinitionExpander still runs and reports it (UnknownDefinition), rather than letting it reach
            // RuleNodeCompiler.Build, which has no case for a node definitions never expanded away.
            RuleNode document = new RuleDocumentNode([], node, node.Span) { Path = TreePath.Root };
            return (document, reader.diagnostics);
        }

        return (node, reader.diagnostics);
    }

    private static string FoundOperands(int count)
    {
        return count == 1 ? "1 operand" : $"{count} operands";
    }

    private static string FormatKeys(string[] keys)
    {
        return string.Join(", ", keys.Select(k => $"'{k}'"));
    }

    private void Report(string message, SourceSpan span, string expected, string found, string path)
    {
        this.Report(DiagnosticCodes.MalformedTree, message, span, expected, found, path);
    }

    // The code is a parameter because the same mistake has one code on every surface: a bad count or bound keeps the
    // code the DSL gives it, and only a tree shape problem is TRE0014.
    private void Report(string code, string message, SourceSpan span, string expected, string found, string path)
    {
        this.diagnostics.Add(Diagnostic.Error(code, message, span, expected, found, path: path));
    }

    // Rejects every key of a node that its kind does not define (the published schema sets additionalProperties to false).
    // A key that belongs to another kind of node, such as "op" next to "predicate", is called a conflict, because the
    // author wrote two nodes into one.
    private bool CheckKeys(ITreeNodeCursor node, string path, string kind, string[] allowed)
    {
        bool valid = true;
        foreach ((string? name, ITreeNodeCursor value, ITreeNodeCursor? key) in node.Members)
        {
            if (name is null)
            {
                this.Report(
                    $"A key of {kind} node must be a {this.words.StringNoun}.",
                    key!.Span,
                    $"a {this.words.StringNoun}",
                    key.Describe(),
                    path
                );
                valid = false;
            }
            else if (!allowed.Contains(name, StringComparer.Ordinal))
            {
                bool conflict = name is "const" or "predicate" or "op";
                string message = conflict
                    ? $"The key '{name}' conflicts with the other keys: {kind} node takes only {FormatKeys(allowed)}."
                    : $"Unknown key '{name}' in {kind} node, which takes only {FormatKeys(allowed)}.";
                DiagnosticSuggestion? suggestion = conflict
                    ? new DiagnosticSuggestion(DiagnosticSuggestionKind.Hint, $"Remove '{name}', or split the node in two.")
                    : NameSuggester.Suggest(name, allowed);
                this.diagnostics.Add(
                    Diagnostic.Error(
                        DiagnosticCodes.MalformedTree,
                        message,
                        value.Span,
                        expected: $"only {FormatKeys(allowed)}",
                        found: $"'{name}'",
                        suggestion: suggestion,
                        path: TreePath.Property(path, name)
                    )
                );
                valid = false;
            }
        }

        return valid;
    }

    private RuleNode? ReadNode(ITreeNodeCursor node, string path)
    {
        this.depth++;
        if (this.depth > this.maxDepth)
        {
            if (!this.maxDepthReported)
            {
                this.maxDepthReported = true;
                this.Report(
                    DiagnosticCodes.MaxDepthExceeded,
                    $"Rule exceeds the maximum tree depth of {this.maxDepth}.",
                    node.Span,
                    $"nesting at most {this.maxDepth} deep",
                    "deeper nesting",
                    path
                );
            }

            this.depth--;
            return null;
        }

        try
        {
            RuleNode? parsed = this.ReadNodeCore(node, path);
            if (parsed is null || !this.TryReadNote(node, path, out string? note))
            {
                return null;
            }

            return parsed with
            {
                Path = path,
                Span = node.Span,
                Note = note,
            };
        }
        finally
        {
            this.depth--;
        }
    }

    /// <summary>
    /// Reads the optional <c>note</c> key every node kind accepts. Its value must be a string; it is kept in normal form
    /// (<see cref="NoteText.Normalize"/>), so an empty note is no note.
    /// </summary>
    /// <returns><see langword="false"/> when the key is present but not a string; the error is reported.</returns>
    private bool TryReadNote(ITreeNodeCursor node, string path, out string? note)
    {
        note = null;
        if (!node.TryGetChild("note", out ITreeNodeCursor? noteNode))
        {
            return true;
        }

        if (noteNode.Shape != TreeNodeShape.Scalar || noteNode.StringValue is not { } text)
        {
            this.Report(
                $"'note' must be a {this.words.StringNoun}.",
                noteNode.Span,
                $"a {this.words.StringNoun}",
                noteNode.Describe(),
                TreePath.Property(path, "note")
            );
            return false;
        }

        note = NoteText.Normalize(text);
        return true;
    }

    private RuleNode? ReadNodeCore(ITreeNodeCursor node, string path)
    {
        if (node.Shape != TreeNodeShape.Mapping)
        {
            this.Report(
                $"Expected a {this.words.MappingNoun} node but found {node.KindName}.",
                node.Span,
                $"a {this.words.MappingNoun}",
                node.Describe(),
                path
            );
            return null;
        }

        if (node.TryGetChild("const", out ITreeNodeCursor? constNode))
        {
            if (!this.CheckKeys(node, path, "a constant", ["const", "note"]))
            {
                return null;
            }

            if (constNode.TryGetTruthValue(out TruthValue constValue))
            {
                return new ConstantNode(constValue, SourceSpan.None);
            }

            this.Report(
                this.words.ConstMessage,
                constNode.Span,
                this.words.ConstExpected,
                constNode.DescribeValue(),
                TreePath.Property(path, "const")
            );
            return null;
        }

        if (node.TryGetChild("predicate", out ITreeNodeCursor? predicateNode))
        {
            // A bare '{"predicate": name}' with no 'args', where 'name' is one of the enclosing definition's own
            // declared parameters, is that parameter used wherever a term, constant or sub-expression could appear
            // (ADR-0008 decision 5), the same disambiguation the DSL makes for a bare identifier. An explicit 'args'
            // still makes it a real term: that shadows the parameter, exactly as the DSL treats a parameter name
            // followed by '(' as a call, not a reference.
            if (
                this.activeParameters is { } scope
                && predicateNode.StringValue is { } parameterName
                && scope.Contains(parameterName)
                && !node.TryGetChild("args", out _)
            )
            {
                return this.CheckKeys(node, path, "a parameter reference", ["predicate", "note"])
                    ? new ParameterReferenceNode(parameterName, SourceSpan.None)
                    : null;
            }

            return this.ReadTerm(node, predicateNode, path);
        }

        if (node.TryGetChild("op", out ITreeNodeCursor? opNode))
        {
            if (opNode.StringValue is { } op)
            {
                return this.ReadOperator(node, opNode, op, path);
            }

            this.Report(
                $"'op' must be a {this.words.StringNoun} naming an operator.",
                opNode.Span,
                $"a {this.words.StringNoun}",
                opNode.Describe(),
                TreePath.Property(path, "op")
            );
            return null;
        }

        this.Report(
            "A tree node must have a 'const', 'predicate', or 'op' key.",
            node.Span,
            "a 'const', 'predicate' or 'op' key",
            "no such key",
            path
        );
        return null;
    }

    private RuleNode? ReadTerm(ITreeNodeCursor node, ITreeNodeCursor predicateNode, string path)
    {
        if (!this.CheckKeys(node, path, "a predicate", ["predicate", "args", "note"]))
        {
            return null;
        }

        if (predicateNode.StringValue is not { } predicateName)
        {
            this.Report(
                $"'predicate' must be a {this.words.StringNoun}.",
                predicateNode.Span,
                $"a {this.words.StringNoun}",
                predicateNode.Describe(),
                TreePath.Property(path, "predicate")
            );
            return null;
        }

        List<ArgumentNode> arguments = [];
        if (node.TryGetChild("args", out ITreeNodeCursor? argsNode))
        {
            string argsPath = TreePath.Property(path, "args");
            if (argsNode.Shape != TreeNodeShape.Mapping)
            {
                this.Report(
                    $"'args' must be a {this.words.MappingNoun}.",
                    argsNode.Span,
                    $"a {this.words.MappingNoun}",
                    argsNode.Describe(),
                    argsPath
                );
                return null;
            }

            foreach ((string? name, ITreeNodeCursor value, ITreeNodeCursor? key) in argsNode.Members)
            {
                // A format whose keys can be non-strings (YAML) hands over the key node so it is reported where it sits.
                if (name is null)
                {
                    this.Report(
                        $"An argument name must be a {this.words.StringNoun}.",
                        key!.Span,
                        $"a {this.words.StringNoun}",
                        key.Describe(),
                        argsPath
                    );
                    return null;
                }

                string argumentPath = TreePath.Property(argsPath, name);
                RawLiteral? literal =
                    value.Shape == TreeNodeShape.Mapping
                        ? this.ReadVariable(value, argumentPath)
                        : this.ReadLiteral(value, argumentPath);
                if (literal is null)
                {
                    return null;
                }

                arguments.Add(new ArgumentNode(name, literal, value.Span) { Path = argumentPath });
            }
        }

        return new TermNode(predicateName, arguments, SourceSpan.None);
    }

    private RuleNode? ReadOperator(ITreeNodeCursor node, ITreeNodeCursor opNode, string op, string path)
    {
        // A declared Collapse is rejected up front: it is no longer an operator, and a plain "unknown operator" would not
        // tell the author where collapse went.
        if (string.Equals(op, "collapse", StringComparison.OrdinalIgnoreCase))
        {
            this.diagnostics.Add(CollapseRejection.Create(DiagnosticCodes.UnknownPredicate, opNode.Span, path));
            return null;
        }

        if (string.Equals(op, "nxor", StringComparison.OrdinalIgnoreCase))
        {
            this.diagnostics.Add(NxorRejection.Create(DiagnosticCodes.UnknownPredicate, opNode.Span, "parity", path));
            return null;
        }

        if (string.Equals(op, "project", StringComparison.OrdinalIgnoreCase))
        {
            this.diagnostics.Add(ProjectRejection.Create(DiagnosticCodes.UnknownPredicate, opNode.Span, path));
            return null;
        }

        // 'use' (ADR-0008 decision 3) is sugar for a definition's expansion, not an operator in OperatorDefinitions, so
        // it is recognized here, ahead of the operator-name lookup below, the same way the rejections above are.
        if (string.Equals(op, "use", StringComparison.OrdinalIgnoreCase))
        {
            return this.ReadUse(node, path);
        }

        // 'forall'/'exists' (ADR-0010) are a quantifier, a third kind alongside the connectives and cardinality
        // operators, not an entry in OperatorDefinitions: their head carries 'var' and 'in' alongside 'operands',
        // and 'operands' holds exactly the one body node (not a variadic list), so they are read here rather than
        // falling into the generic op-name dispatch below.
        if (string.Equals(op, "forall", StringComparison.OrdinalIgnoreCase))
        {
            return this.ReadQuantifier(node, QuantifierKind.ForAll, path);
        }

        if (string.Equals(op, "exists", StringComparison.OrdinalIgnoreCase))
        {
            return this.ReadQuantifier(node, QuantifierKind.Exists, path);
        }

        // The operator name is checked before its operands so a typo is reported on its own, with its suggestion.
        if (!TreeFormatOpNames.TryFromTreeFormat(op, out string? canonicalOpName))
        {
            this.diagnostics.Add(
                Diagnostic.Error(
                    DiagnosticCodes.UnknownPredicate,
                    $"Unknown operator '{op}'.",
                    opNode.Span,
                    expected: "a known operator",
                    found: $"'{op}'",
                    suggestion: NameSuggester.Suggest(op, TreeFormatOpNames.ReadableNames),
                    path: TreePath.Property(path, "op")
                )
            );
            return null;
        }

        string[] operatorKeys = canonicalOpName switch
        {
            "AtLeast" or "AtMost" or "GreaterThan" or "LessThan" or "Exactly" => ["op", "operands", "k", "note"],
            "Between" => ["op", "operands", "min", "max", "note"],
            _ => ["op", "operands", "note"],
        };
        if (!this.CheckKeys(node, path, $"a '{op}' operator", operatorKeys))
        {
            return null;
        }

        string operandsPath = TreePath.Property(path, "operands");
        string operandsMessage = $"Operator node '{op}' requires an 'operands' {this.words.SequenceNoun}.";
        string operandsExpected = $"an 'operands' {this.words.SequenceNoun}";
        if (!node.TryGetChild("operands", out ITreeNodeCursor? operandsNode))
        {
            this.Report(operandsMessage, node.Span, operandsExpected, $"no 'operands' {this.words.KeyNoun}", path);
            return null;
        }

        if (operandsNode.Shape != TreeNodeShape.Sequence)
        {
            this.Report(operandsMessage, operandsNode.Span, operandsExpected, operandsNode.Describe(), operandsPath);
            return null;
        }

        List<RuleNode> operands = [];
        int index = 0;
        foreach (ITreeNodeCursor operandNode in operandsNode.Elements)
        {
            RuleNode? operand = this.ReadNode(operandNode, TreePath.Index(operandsPath, index++));
            if (operand is null)
            {
                return null;
            }

            operands.Add(operand);
        }

        switch (canonicalOpName)
        {
            case "And":
                return new AndNode(operands, SourceSpan.None);
            case "Or":
                return new OrNode(operands, SourceSpan.None);
            case "Not":
                if (operands.Count != 1)
                {
                    this.Report(
                        DiagnosticCodes.InfixArityViolation,
                        "'not' requires exactly one operand.",
                        operandsNode.Span,
                        "1 operand",
                        FoundOperands(operands.Count),
                        operandsPath
                    );
                    return null;
                }

                return new NotNode(operands[0], SourceSpan.None);
            case "Xor":
                return new XorNode(operands, SourceSpan.None);
            case "Equivalent":
                return new EquivalentNode(operands, SourceSpan.None);
            case "Implies":
                return new ImpliesNode(operands, SourceSpan.None);
            case "Nand":
                return new NandNode(operands, SourceSpan.None);
            case "Nor":
                return new NorNode(operands, SourceSpan.None);
            case "Parity":
                return new ParityNode(operands, SourceSpan.None);
            case "Any":
                return new AnyNode(operands, SourceSpan.None);
            case "All":
                return new AllNode(operands, SourceSpan.None);
            case "None":
                return new NoneNode(operands, SourceSpan.None);
            case "Coalesce":
                return new CoalesceNode(operands, SourceSpan.None);
            case "If":
                return new IfNode(operands, SourceSpan.None);
            case "IsTrue":
                return new InspectionNode(InspectionKind.IsTrue, operands, SourceSpan.None);
            case "IsFalse":
                return new InspectionNode(InspectionKind.IsFalse, operands, SourceSpan.None);
            case "IsUnknown":
                return new InspectionNode(InspectionKind.IsUnknown, operands, SourceSpan.None);
            case "IsKnown":
                return new InspectionNode(InspectionKind.IsKnown, operands, SourceSpan.None);
            case "ExactlyOne":
                return new ExactlyOneNode(operands, SourceSpan.None);
            case "AtLeast":
                return this.ReadThreshold(node, op, path, ThresholdComparison.AtLeast, operands);
            case "AtMost":
                return this.ReadThreshold(node, op, path, ThresholdComparison.AtMost, operands);
            case "GreaterThan":
                return this.ReadThreshold(node, op, path, ThresholdComparison.GreaterThan, operands);
            case "LessThan":
                return this.ReadThreshold(node, op, path, ThresholdComparison.LessThan, operands);
            case "Exactly":
                return this.ReadThreshold(node, op, path, ThresholdComparison.Exactly, operands);
            case "Between":
                return this.ReadBetween(node, op, path, operands);
            default:
                throw new InvalidOperationException($"Unhandled canonical op-name '{canonicalOpName}'.");
        }
    }

    private RuleNode? ReadThreshold(
        ITreeNodeCursor node,
        string op,
        string path,
        ThresholdComparison comparison,
        List<RuleNode> operands
    )
    {
        bool present = node.TryGetChild("k", out ITreeNodeCursor? kNode);
        if (!present || !kNode!.TryGetInt32(out int k))
        {
            this.Report(
                DiagnosticCodes.InvalidThresholdValue,
                $"'{op}' requires a numeric 'k'.",
                present ? kNode!.Span : node.Span,
                "an integer",
                present ? kNode!.DescribeValue() : $"no 'k' {this.words.KeyNoun}",
                present ? TreePath.Property(path, "k") : path
            );
            return null;
        }

        return new ThresholdNode(comparison, k, operands, SourceSpan.None);
    }

    private RuleNode? ReadBetween(ITreeNodeCursor node, string op, string path, List<RuleNode> operands)
    {
        if (!this.TryReadBound(node, op, "min", path, out int min) || !this.TryReadBound(node, op, "max", path, out int max))
        {
            return null;
        }

        return new BetweenNode(min, max, operands, SourceSpan.None);
    }

    // Reports the first bound that is missing or not an integer, at that bound's own path.
    private bool TryReadBound(ITreeNodeCursor node, string op, string key, string path, out int value)
    {
        value = 0;
        bool present = node.TryGetChild(key, out ITreeNodeCursor? child);
        if (present && child!.TryGetInt32(out value))
        {
            return true;
        }

        this.Report(
            DiagnosticCodes.InvalidThresholdValue,
            $"'{op}' requires integer 'min' and 'max'.",
            present ? child!.Span : node.Span,
            "an integer",
            present ? child!.DescribeValue() : $"no '{key}' {this.words.KeyNoun}",
            present ? TreePath.Property(path, key) : path
        );
        return false;
    }

    /// <summary>
    /// Reads a <c>{"op": "use", "name": ..., "operands": [...]}</c> node (ADR-0008 decision 3): a use of a document-scoped
    /// definition, valid wherever a tree node is expected. <c>operands</c> supplies the positional arguments and may be
    /// omitted or empty for a parameterless use, unlike every other operator node, which requires it.
    /// </summary>
    private RuleNode? ReadUse(ITreeNodeCursor node, string path)
    {
        this.sawUse = true;
        if (!this.CheckKeys(node, path, "a 'use' node", ["op", "name", "operands", "note"]))
        {
            return null;
        }

        bool hasName = node.TryGetChild("name", out ITreeNodeCursor? nameNode);
        if (!hasName || nameNode!.StringValue is not { } name)
        {
            this.Report(
                $"A 'use' node requires a 'name' {this.words.KeyNoun} naming the definition.",
                hasName ? nameNode!.Span : node.Span,
                $"a {this.words.StringNoun}",
                hasName ? nameNode!.Describe() : $"no 'name' {this.words.KeyNoun}",
                hasName ? TreePath.Property(path, "name") : path
            );
            return null;
        }

        List<RuleNode> arguments = [];
        if (node.TryGetChild("operands", out ITreeNodeCursor? operandsNode))
        {
            if (operandsNode.Shape != TreeNodeShape.Sequence)
            {
                this.Report(
                    $"'operands' of a 'use' node must be a {this.words.SequenceNoun}.",
                    operandsNode.Span,
                    $"a {this.words.SequenceNoun}",
                    operandsNode.Describe(),
                    TreePath.Property(path, "operands")
                );
                return null;
            }

            string operandsPath = TreePath.Property(path, "operands");
            int index = 0;
            foreach (ITreeNodeCursor operandNode in operandsNode.Elements)
            {
                RuleNode? operand = this.ReadNode(operandNode, TreePath.Index(operandsPath, index++));
                if (operand is null)
                {
                    return null;
                }

                arguments.Add(operand);
            }
        }

        return new UseNode(name, arguments, SourceSpan.None, nameNode.Span) { NamePath = TreePath.Property(path, "name") };
    }

    /// <summary>
    /// Reads a <c>{"op": "forall" | "exists", "var": ..., "in": ..., "operands": [&lt;body&gt;]}</c> node
    /// (ADR-0010 decision 2): <c>var</c> is the bound variable name, <c>in</c> the collection name, and
    /// <c>operands</c> holds exactly the one body node. A <see cref="DiagnosticCodes.MalformedTree"/> otherwise,
    /// so body diagnostics get the path <c>$.operands[0]...</c> like every other node.
    /// </summary>
    private RuleNode? ReadQuantifier(ITreeNodeCursor node, QuantifierKind kind, string path)
    {
        string op = kind == QuantifierKind.ForAll ? "forall" : "exists";
        if (!this.CheckKeys(node, path, $"a '{op}' quantifier", ["op", "var", "in", "operands", "note"]))
        {
            return null;
        }

        bool hasVariable = node.TryGetChild("var", out ITreeNodeCursor? variableNode);
        if (!hasVariable || variableNode!.StringValue is not { } variable)
        {
            this.Report(
                $"A '{op}' quantifier requires a 'var' {this.words.KeyNoun} naming the bound variable.",
                hasVariable ? variableNode!.Span : node.Span,
                $"a {this.words.StringNoun}",
                hasVariable ? variableNode!.Describe() : $"no 'var' {this.words.KeyNoun}",
                hasVariable ? TreePath.Property(path, "var") : path
            );
            return null;
        }

        bool hasCollection = node.TryGetChild("in", out ITreeNodeCursor? collectionNode);
        if (!hasCollection || collectionNode!.StringValue is not { } collection)
        {
            this.Report(
                $"A '{op}' quantifier requires an 'in' {this.words.KeyNoun} naming the collection.",
                hasCollection ? collectionNode!.Span : node.Span,
                $"a {this.words.StringNoun}",
                hasCollection ? collectionNode!.Describe() : $"no 'in' {this.words.KeyNoun}",
                hasCollection ? TreePath.Property(path, "in") : path
            );
            return null;
        }

        string operandsPath = TreePath.Property(path, "operands");
        string operandsMessage =
            $"A '{op}' quantifier requires an 'operands' {this.words.SequenceNoun} with exactly one item (the body).";
        string operandsExpected = $"an 'operands' {this.words.SequenceNoun} with exactly one item";
        if (!node.TryGetChild("operands", out ITreeNodeCursor? operandsNode))
        {
            this.Report(operandsMessage, node.Span, operandsExpected, $"no 'operands' {this.words.KeyNoun}", path);
            return null;
        }

        if (operandsNode.Shape != TreeNodeShape.Sequence)
        {
            this.Report(operandsMessage, operandsNode.Span, operandsExpected, operandsNode.Describe(), operandsPath);
            return null;
        }

        List<ITreeNodeCursor> operandNodes = [.. operandsNode.Elements];
        if (operandNodes.Count != 1)
        {
            this.Report(
                DiagnosticCodes.MalformedTree,
                operandsMessage,
                operandsNode.Span,
                operandsExpected,
                FoundOperands(operandNodes.Count),
                operandsPath
            );
            return null;
        }

        RuleNode? body = this.ReadNode(operandNodes[0], TreePath.Index(operandsPath, 0));
        if (body is null)
        {
            return null;
        }

        return new QuantifierNode(kind, variable, collection, body, SourceSpan.None);
    }

    /// <summary>Reads the document root's <c>definitions</c> section (ADR-0008 decision 3) into one <see cref="DefinitionNode"/> per entry.</summary>
    /// <returns>The definitions, or <see langword="null"/> when the section itself was malformed (a diagnostic was reported).</returns>
    private List<DefinitionNode>? ReadDefinitions(ITreeNodeCursor definitionsNode, string definitionsPath)
    {
        if (definitionsNode.Shape != TreeNodeShape.Mapping)
        {
            this.Report(
                $"'definitions' must be a {this.words.MappingNoun}, keyed by definition name.",
                definitionsNode.Span,
                $"a {this.words.MappingNoun}",
                definitionsNode.Describe(),
                definitionsPath
            );
            return null;
        }

        List<DefinitionNode> definitions = [];
        foreach ((string? name, ITreeNodeCursor entry, ITreeNodeCursor? key) in definitionsNode.Members)
        {
            if (name is null)
            {
                this.Report(
                    $"A definition name must be a {this.words.StringNoun}.",
                    key!.Span,
                    $"a {this.words.StringNoun}",
                    key.Describe(),
                    definitionsPath
                );
                return null;
            }

            DefinitionNode? definition = this.ReadDefinitionEntry(name, entry, TreePath.Property(definitionsPath, name), key);
            if (definition is null)
            {
                return null;
            }

            definitions.Add(definition);
        }

        return definitions;
    }

    /// <summary>Reads one <c>definitions</c> entry: <c>{"parameters": [...], "body": &lt;node&gt;}</c>.</summary>
    private DefinitionNode? ReadDefinitionEntry(string name, ITreeNodeCursor entry, string path, ITreeNodeCursor? keyNode)
    {
        if (entry.Shape != TreeNodeShape.Mapping)
        {
            this.Report(
                $"A definition entry must be a {this.words.MappingNoun} with 'parameters' and 'body'.",
                entry.Span,
                $"a {this.words.MappingNoun}",
                entry.Describe(),
                path
            );
            return null;
        }

        if (!this.CheckKeys(entry, path, "a definition", ["parameters", "body"]))
        {
            return null;
        }

        List<string> parameters = [];
        if (entry.TryGetChild("parameters", out ITreeNodeCursor? parametersNode))
        {
            if (parametersNode.Shape != TreeNodeShape.Sequence)
            {
                this.Report(
                    $"'parameters' must be a {this.words.SequenceNoun} of {this.words.StringNoun}s.",
                    parametersNode.Span,
                    $"a {this.words.SequenceNoun} of {this.words.StringNoun}s",
                    parametersNode.Describe(),
                    TreePath.Property(path, "parameters")
                );
                return null;
            }

            string parametersPath = TreePath.Property(path, "parameters");
            int index = 0;
            foreach (ITreeNodeCursor item in parametersNode.Elements)
            {
                if (item.StringValue is not { } parameterName)
                {
                    this.Report(
                        $"Each parameter name must be a {this.words.StringNoun}.",
                        item.Span,
                        $"a {this.words.StringNoun}",
                        item.Describe(),
                        TreePath.Index(parametersPath, index)
                    );
                    return null;
                }

                parameters.Add(parameterName);
                index++;
            }
        }

        if (!entry.TryGetChild("body", out ITreeNodeCursor? bodyNode))
        {
            this.Report(
                "A definition requires a 'body' node.",
                entry.Span,
                "a 'body' node",
                $"no 'body' {this.words.KeyNoun}",
                path
            );
            return null;
        }

        IReadOnlySet<string>? enclosingParameters = this.activeParameters;
        this.activeParameters = new HashSet<string>(parameters, StringComparer.OrdinalIgnoreCase);
        RuleNode? body = this.ReadNode(bodyNode, TreePath.Property(path, "body"));
        this.activeParameters = enclosingParameters;
        if (body is null)
        {
            return null;
        }

        return new DefinitionNode(name, parameters, body, entry.Span, keyNode?.Span ?? SourceSpan.None) { NamePath = path };
    }

    // A mapping in argument position is a variable reference (ADR-0006 decision 10): exactly a "from" source name and a
    // "query" string. Anything else is reported at the member that is wrong, so the author sees which part to fix.
    private RawLiteral? ReadVariable(ITreeNodeCursor node, string path)
    {
        string? source = null;
        string? query = null;
        ITreeNodeCursor? sourceNode = null;
        ITreeNodeCursor? queryNode = null;
        foreach ((string? name, ITreeNodeCursor value, ITreeNodeCursor? key) in node.Members)
        {
            switch (name)
            {
                case "from":
                    sourceNode = value;
                    source = value.StringValue;
                    break;
                case "query":
                    queryNode = value;
                    query = value.StringValue;
                    break;
                default:
                    this.Report(
                        $"A variable reference has only 'from' and 'query' members, but found {(name is null ? key!.DescribeValue() : $"'{name}'")}.",
                        (name is null ? key! : value).Span,
                        "only 'from' and 'query'",
                        name is null ? key!.DescribeValue() : $"'{name}'",
                        name is null ? path : TreePath.Property(path, name)
                    );
                    return null;
            }
        }

        string sourcePath = TreePath.Property(path, "from");
        string queryPath = TreePath.Property(path, "query");
        if (!this.CheckVariableMember(node, sourceNode, source, "from", "source name", sourcePath, path))
        {
            return null;
        }

        if (!this.CheckVariableMember(node, queryNode, query, "query", "query", queryPath, path))
        {
            return null;
        }

        return RawLiteral.OfVariable(
            source!,
            query!,
            node.Span,
            new VariableParts(sourceNode!.Span, queryNode!.Span, sourcePath, queryPath)
        );
    }

    // A variable reference member must be present and a string; reports the missing or mistyped member and returns false.
    private bool CheckVariableMember(
        ITreeNodeCursor reference,
        ITreeNodeCursor? member,
        string? text,
        string key,
        string meaning,
        string memberPath,
        string referencePath
    )
    {
        if (text is not null)
        {
            return true;
        }

        if (member is null)
        {
            this.Report(
                $"A variable reference requires a '{key}' {this.words.KeyNoun} holding the {meaning}.",
                reference.Span,
                $"a '{key}' {this.words.KeyNoun}",
                $"no '{key}' {this.words.KeyNoun}",
                referencePath
            );
            return false;
        }

        this.Report(
            $"The '{key}' of a variable reference must be a {this.words.StringNoun} holding the {meaning}.",
            member.Span,
            $"a {this.words.StringNoun}",
            member.Describe(),
            memberPath
        );
        return false;
    }

    private RawLiteral? ReadLiteral(ITreeNodeCursor node, string path)
    {
        switch (node.Shape)
        {
            case TreeNodeShape.Scalar when node.ReadScalarLiteral() is { } scalar:
                return scalar;
            case TreeNodeShape.Sequence:
                List<RawLiteral> items = [];
                int index = 0;
                foreach (ITreeNodeCursor item in node.Elements)
                {
                    RawLiteral? converted = this.ReadLiteral(item, TreePath.Index(path, index++));
                    if (converted is null)
                    {
                        return null;
                    }

                    items.Add(converted);
                }

                return RawLiteral.OfArray(items, node.Span);
            default:
                this.Report(node.UnsupportedLiteralMessage, node.Span, this.words.LiteralExpected, node.Describe(), path);
                return null;
        }
    }

    /// <summary>
    /// A view of <paramref name="inner"/> with one top-level key hidden from <see cref="Members"/>, so the document
    /// root's own node can be read as if its <c>definitions</c> sibling (ADR-0008 decision 3) were never there,
    /// without <see cref="CheckKeys"/> flagging it as unknown, and without a second, definitions-aware copy of every
    /// node-shape check.
    /// </summary>
    private readonly struct KeyExcludingCursor(ITreeNodeCursor inner, string excludedKey) : ITreeNodeCursor
    {
        public TreeNodeShape Shape => inner.Shape;

        public SourceSpan Span => inner.Span;

        public string KindName => inner.KindName;

        public string? StringValue => inner.StringValue;

        public IEnumerable<ITreeNodeCursor> Elements => inner.Elements;

        public IEnumerable<TreeMember> Members => Filtered(inner.Members, excludedKey);

        public string UnsupportedLiteralMessage => inner.UnsupportedLiteralMessage;

        public string Describe()
        {
            return inner.Describe();
        }

        public string DescribeValue()
        {
            return inner.DescribeValue();
        }

        public bool TryGetChild(string key, [NotNullWhen(true)] out ITreeNodeCursor? child)
        {
            return inner.TryGetChild(key, out child);
        }

        public bool TryGetInt32(out int value)
        {
            return inner.TryGetInt32(out value);
        }

        public bool TryGetTruthValue(out TruthValue value)
        {
            return inner.TryGetTruthValue(out value);
        }

        public RawLiteral? ReadScalarLiteral()
        {
            return inner.ReadScalarLiteral();
        }

        private static IEnumerable<TreeMember> Filtered(IEnumerable<TreeMember> members, string excludedKey)
        {
            return members.Where(member => !string.Equals(member.Name, excludedKey, StringComparison.Ordinal));
        }
    }
}
