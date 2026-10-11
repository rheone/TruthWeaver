namespace TruthWeaver.Parsing;

using TruthWeaver.Abstractions;
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

    private TreeFormatReader(TreeFormatVocabulary words)
    {
        this.words = words;
    }

    /// <summary>Reads a tree document into a raw <see cref="RuleNode"/> tree plus any diagnostics.</summary>
    /// <param name="root">The cursor positioned on the document's root node.</param>
    /// <param name="words">The format's own words for diagnostics.</param>
    /// <returns>
    /// The parsed root node (or <see langword="null"/> when the tree was malformed) and the diagnostics raised while reading.
    /// </returns>
    public static (RuleNode? Root, IReadOnlyList<Diagnostic> Diagnostics) Read(ITreeNodeCursor root, TreeFormatVocabulary words)
    {
        TreeFormatReader reader = new(words);
        RuleNode? node = reader.ReadNode(root, TreePath.Root);
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
        RuleNode? parsed = this.ReadNodeCore(node, path);
        return parsed is null ? null : parsed with { Path = path, Span = node.Span };
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
            if (!this.CheckKeys(node, path, "a constant", ["const"]))
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
        if (!this.CheckKeys(node, path, "a predicate", ["predicate", "args"]))
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
            "AtLeast" or "AtMost" or "GreaterThan" or "LessThan" or "Exactly" => ["op", "operands", "k"],
            "Between" => ["op", "operands", "min", "max"],
            _ => ["op", "operands"],
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
}
