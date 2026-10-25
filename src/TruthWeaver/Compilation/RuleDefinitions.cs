namespace TruthWeaver.Compilation;

using System.Text.Json.Nodes;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Json;
using TruthWeaver.Parsing;

/// <summary>
/// The <c>let</c> definitions and the uses of a rule document, read without expanding them. A compiled rule holds no
/// definitions: the compiler replaces each use by the definition body. Read the source with this type to show the
/// definitions, to find the definition or use that a node of the compiled rule comes from (<see cref="SourcesOf"/>), or
/// to edit the document and keep its definitions (<see cref="PrintJson"/>).
/// </summary>
/// <remarks>
/// Reading only parses the text. It does not need a predicate registry and it does not expand the uses, so it reports
/// syntax errors only: compile the text to find an unknown definition, a cycle or a wrong number of arguments. Reading
/// never throws for an authoring error; a text with a syntax error gives <see cref="Succeeded"/> <see langword="false"/>
/// and the diagnostics. Rule text and JSON are read here; <c>TruthWeaver.Yaml</c> reads YAML.
/// </remarks>
public sealed class RuleDefinitions
{
    private readonly RuleNode? root;
    private readonly Dictionary<string, List<string>> sources;

    private RuleDefinitions(
        RuleNode? root,
        EquatableArray<Diagnostic> diagnostics,
        EquatableArray<DefinitionDeclaration> definitions,
        EquatableArray<DefinitionUse> uses,
        Dictionary<string, List<string>> sources
    )
    {
        this.root = root;
        this.Diagnostics = diagnostics;
        this.Definitions = definitions;
        this.Uses = uses;
        this.sources = sources;
    }

    /// <summary>Gets a value indicating whether the text was read: it has no syntax error.</summary>
    public bool Succeeded => this.root is not null;

    /// <summary>Gets the diagnostics of reading the text: the syntax errors and warnings of the parser.</summary>
    public EquatableArray<Diagnostic> Diagnostics { get; }

    /// <summary>Gets the definitions in declaration order; empty when the text was not read or declares none.</summary>
    public EquatableArray<DefinitionDeclaration> Definitions { get; }

    /// <summary>
    /// Gets every use in source order: the uses in the definition bodies and in the rule, with the uses inside an argument
    /// after the use that holds them. It is empty when the text was not read or has no use.
    /// </summary>
    public EquatableArray<DefinitionUse> Uses { get; }

    /// <summary>Gets a value indicating whether the document declares a definition or writes a use.</summary>
    public bool HasDefinitions => this.Definitions.Count > 0 || this.Uses.Count > 0;

    /// <summary>Reads the definitions and uses of rule text.</summary>
    /// <param name="text">The rule text.</param>
    /// <param name="options">
    /// The resource limits to read by: <see cref="CompilerOptions.MaxDepth"/> bounds the parse, and
    /// <see cref="CompilerOptions.MaxDefinitionDepth"/> and <see cref="CompilerOptions.MaxNodeCount"/> bound the walk
    /// that maps compiled nodes back to their sources (<see cref="SourcesOf"/>), the same way they bound an actual
    /// compile. Defaults to <see cref="CompilerOptions.Default"/>, so a reader that supplies none still cannot be
    /// made to hang or exhaust memory by an untrusted document (ticket 04).
    /// </param>
    /// <returns>The definitions and uses, or the syntax diagnostics when the text cannot be read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    public static RuleDefinitions ReadText(string text, CompilerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        CompilerOptions effective = options ?? CompilerOptions.Default;
        (RuleNode root, IReadOnlyList<Diagnostic> diagnostics) = DslParser.Parse(text, effective.MaxDepth);
        return FromParsed(root, diagnostics, null, effective);
    }

    /// <summary>Reads the definitions and uses of a rule in the JSON tree format.</summary>
    /// <param name="json">The JSON tree text.</param>
    /// <param name="options">
    /// The resource limits to read by, matching <see cref="ReadText"/>: <see cref="CompilerOptions.MaxDepth"/> bounds
    /// the parse, and <see cref="CompilerOptions.MaxDefinitionDepth"/> and <see cref="CompilerOptions.MaxNodeCount"/>
    /// bound the <see cref="SourcesOf"/> walk. Defaults to <see cref="CompilerOptions.Default"/>, so a caller that
    /// compiles with a different <see cref="CompilerOptions.MaxDefinitionDepth"/> can pass the same options here and
    /// get a source map that agrees with that compile (ticket 23), instead of one truncated at the default depth.
    /// </param>
    /// <returns>The definitions and uses, or the diagnostics when the JSON cannot be read as a rule tree.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static RuleDefinitions ReadJson(string json, CompilerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        CompilerOptions effective = options ?? CompilerOptions.Default;
        (RuleNode? root, IReadOnlyList<Diagnostic> diagnostics) = JsonTreeParser.Parse(
            json,
            effective.MaxDepth,
            effective.MaxNodeCount
        );
        return FromParsed(root, diagnostics, JsonSpanLocator.Create(json), effective);
    }

    /// <summary>
    /// Gets where a node of the compiled rule comes from: the paths, in the JSON tree format of the document
    /// (<see cref="PrintJson"/>), of the source nodes that sit at that place after expansion. The outermost comes first: a
    /// use, then the node of the definition body that the use expands to, and so on until the node that holds the
    /// operator, term or constant itself. A parameter is not listed: the argument that the use supplies for it is.
    /// </summary>
    /// <param name="path">The path of the node in the compiled rule, such as <c>$.operands[0]</c>.</param>
    /// <returns>The source paths, outermost first; empty when the compiled rule has no node at the path or the text was not read.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public EquatableArray<string> SourcesOf(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return this.sources.TryGetValue(path, out List<string>? found)
            ? new EquatableArray<string>(found)
            : EquatableArray<string>.Empty;
    }

    /// <summary>
    /// Prints the document in the JSON tree format without expanding it: a <c>definitions</c> object beside the members of
    /// the root node, and a <c>{"op": "use", "name": ..., "operands": [...]}</c> node for each use. The text compiles to
    /// the same rule as the source. Notes on nodes are kept; comments that rule text attaches to a <c>let</c> declaration
    /// as a whole are not.
    /// </summary>
    /// <returns>The JSON text on one line, or <see langword="null"/> when the text was not read.</returns>
    public string? PrintJson()
    {
        JsonNodeWriter writer = new();
        return this.WriteTree(writer) ? writer.Result!.ToJsonString() : null;
    }

    /// <summary>
    /// Builds the result of a parse. Internal, and visible to <c>TruthWeaver.Yaml</c>, so the YAML front end reads YAML the
    /// same way.
    /// </summary>
    /// <param name="root">The raw parse tree, or <see langword="null"/> when the text could not be parsed.</param>
    /// <param name="diagnostics">The diagnostics of the parse.</param>
    /// <param name="locator">
    /// Finds the span of a JSON path, for a front end whose nodes carry no span (JSON), or <see langword="null"/>.
    /// </param>
    /// <param name="options">
    /// The resource limits the source-mapping walk (<see cref="Reader.Expand"/>) enforces, matching whatever a real
    /// compile of the same document would use. Defaults to <see cref="CompilerOptions.Default"/> when a caller
    /// supplies none, so a reader can never be made to hang or exhaust memory on an untrusted document (ticket 04).
    /// </param>
    /// <returns>The result.</returns>
    internal static RuleDefinitions FromParsed(
        RuleNode? root,
        IReadOnlyList<Diagnostic> diagnostics,
        JsonSpanLocator? locator,
        CompilerOptions? options = null
    )
    {
        if (root is null || diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return new RuleDefinitions(
                null,
                new EquatableArray<Diagnostic>(diagnostics),
                EquatableArray<DefinitionDeclaration>.Empty,
                EquatableArray<DefinitionUse>.Empty,
                []
            );
        }

        Reader reader = new(locator, options ?? CompilerOptions.Default);
        reader.Read(root);
        return new RuleDefinitions(
            root,
            new EquatableArray<Diagnostic>([.. diagnostics, .. reader.Diagnostics]),
            new EquatableArray<DefinitionDeclaration>(reader.Definitions),
            new EquatableArray<DefinitionUse>(reader.Uses),
            reader.Sources
        );
    }

    /// <summary>
    /// Writes the document in the tree format through a format adapter, as <see cref="PrintJson"/> describes. Internal, and
    /// visible to <c>TruthWeaver.Yaml</c>, which prints the same tree as YAML.
    /// </summary>
    /// <param name="writer">The format adapter.</param>
    /// <returns><see langword="true"/> when the tree was written; <see langword="false"/> when the text was not read.</returns>
    internal bool WriteTree(Printing.ITreeWriter writer)
    {
        if (this.root is null)
        {
            return false;
        }

        RawTreeEmitter.EmitDocument(this.root, writer);
        return true;
    }

    /// <summary>
    /// Reads one parsed document: lists its definitions and uses, and walks the rule as the expander expands it so that
    /// each node of the compiled rule gets the source paths it comes from.
    /// </summary>
    private sealed class Reader(JsonSpanLocator? locator, CompilerOptions options)
    {
        // The first declaration of each name, as the expander keeps it; a repeated name is a collision the compiler reports.
        private readonly Dictionary<string, (DefinitionNode Node, string Path)> byName =
        [
            with(StringComparer.OrdinalIgnoreCase),
        ];

        private readonly List<(UseNode Node, string? Definition, string Path)> uses = [];
        private readonly Dictionary<string, List<string>> expandedPathsOfUse = [with(StringComparer.Ordinal)];

        // Counts every node Expand visits, mirroring DefinitionExpander's own nodeCount (ticket 04): this walk
        // follows the same 'let' uses a real compile would expand, so a shallow but high-fanout chain of definitions
        // can make it recurse just as explosively. Checked on entry to Expand, before it records anything or
        // recurses into an operand or a use's body, so work past the limit stays O(1) per call.
        private int nodeCount;
        private bool nodeLimitReported;

        public List<DefinitionDeclaration> Definitions { get; } = [];

        public List<DefinitionUse> Uses { get; } = [];

        public Dictionary<string, List<string>> Sources { get; } = [with(StringComparer.Ordinal)];

        public List<Diagnostic> Diagnostics { get; } = [];

        public void Read(RuleNode root)
        {
            IReadOnlyList<DefinitionNode> declared = root is RuleDocumentNode document ? document.Definitions : [];
            RuleNode body = root is RuleDocumentNode withBody ? withBody.Body : root;
            string definitionsPath = TreePath.Property(TreePath.Root, "definitions");
            foreach (DefinitionNode definition in declared)
            {
                string path = TreePath.Property(definitionsPath, definition.Name);
                if (!this.byName.TryAdd(definition.Name, (definition, path)))
                {
                    continue;
                }

                this.Definitions.Add(
                    new DefinitionDeclaration(
                        definition.Name,
                        new EquatableArray<string>(definition.Parameters),
                        path,
                        this.Locate(definition.Span, path),
                        this.Locate(definition.NameSpan, path)
                    )
                );
                this.CollectUses(definition.Body, definition.Name, TreePath.Property(path, "body"));
            }

            this.CollectUses(body, null, TreePath.Root);
            this.Expand(body, TreePath.Root, TreePath.Root, null, []);
            foreach ((UseNode use, string? owner, string path) in this.uses)
            {
                this.Uses.Add(
                    new DefinitionUse(
                        use.Name,
                        use.Arguments.Count,
                        owner,
                        path,
                        new EquatableArray<string>(this.expandedPathsOfUse.GetValueOrDefault(path) ?? []),
                        this.Locate(use.Span, path),
                        this.Locate(use.NameSpan, TreePath.Property(path, "name"))
                    )
                );
            }
        }

        // The child of a raw node at operand position i, in the JSON tree format: every node keeps its children, the
        // arguments of a use and the body of a quantifier under "operands".
        private static string OperandPath(string path, int index)
        {
            return TreePath.Index(TreePath.Property(path, "operands"), index);
        }

        private SourceSpan Locate(SourceSpan span, string path)
        {
            return span == SourceSpan.None && locator is not null ? locator.Locate(path) : span;
        }

        // Lists the uses under a node in source order: a use, then the uses inside its arguments.
        private void CollectUses(RuleNode node, string? owner, string path)
        {
            if (node is UseNode use)
            {
                this.uses.Add((use, owner, path));
            }

            IReadOnlyList<RuleNode> operands = RuleNode.OperandsOf(node);
            for (int i = 0; i < operands.Count; i++)
            {
                this.CollectUses(operands[i], owner, OperandPath(path, i));
            }
        }

        /// <summary>Walks a raw node the way <see cref="DefinitionExpander"/> expands it, recording the source path of each compiled node.</summary>
        /// <param name="node">The raw node.</param>
        /// <param name="path">The path of the raw node in the JSON tree format of the document.</param>
        /// <param name="expanded">The path of the compiled node that the raw node becomes.</param>
        /// <param name="parameters">The bindings of the definition body being walked (name to argument, its path and its own bindings), or <see langword="null"/>.</param>
        /// <param name="chain">The definitions being expanded through, for cycle and depth detection.</param>
        private void Expand(
            RuleNode node,
            string path,
            string expanded,
            IReadOnlyDictionary<string, Binding>? parameters,
            List<string> chain
        )
        {
            this.nodeCount++;
            if (this.nodeCount > options.MaxNodeCount)
            {
                if (!this.nodeLimitReported)
                {
                    this.nodeLimitReported = true;
                    this.Diagnostics.Add(
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

                return;
            }

            if (node is ParameterReferenceNode reference)
            {
                // The argument takes the place of the parameter, in the scope of the use that supplied it.
                if (parameters is not null && parameters.TryGetValue(reference.Name, out Binding? bound))
                {
                    this.Expand(bound.Node, bound.Path, expanded, bound.Scope, chain);
                    return;
                }

                this.Record(expanded, path);
                return;
            }

            this.Record(expanded, path);
            if (node is UseNode use)
            {
                this.ExpandUse(use, path, expanded, parameters, chain);
                return;
            }

            IReadOnlyList<RuleNode> operands = RuleNode.OperandsOf(node);
            for (int i = 0; i < operands.Count; i++)
            {
                this.Expand(operands[i], OperandPath(path, i), OperandPath(expanded, i), parameters, chain);
            }
        }

        private void ExpandUse(
            UseNode use,
            string path,
            string expanded,
            IReadOnlyDictionary<string, Binding>? parameters,
            List<string> chain
        )
        {
            if (!this.expandedPathsOfUse.TryGetValue(path, out List<string>? places))
            {
                places = [];
                this.expandedPathsOfUse[path] = places;
            }

            places.Add(expanded);

            // A use that the expander refuses (unknown, a cycle, a wrong count, too deep) becomes an error, not a body.
            if (
                !this.byName.TryGetValue(use.Name, out (DefinitionNode Node, string Path) found)
                || chain.Contains(use.Name, StringComparer.OrdinalIgnoreCase)
                || use.Arguments.Count != found.Node.Parameters.Count
                || chain.Count >= options.MaxDefinitionDepth
            )
            {
                return;
            }

            Dictionary<string, Binding> bound = [with(StringComparer.OrdinalIgnoreCase)];
            for (int i = 0; i < use.Arguments.Count; i++)
            {
                bound[found.Node.Parameters[i]] = new Binding(use.Arguments[i], OperandPath(path, i), parameters);
            }

            this.Expand(found.Node.Body, TreePath.Property(found.Path, "body"), expanded, bound, [.. chain, use.Name]);
        }

        private void Record(string expanded, string path)
        {
            if (!this.Sources.TryGetValue(expanded, out List<string>? list))
            {
                list = [];
                this.Sources[expanded] = list;
            }

            list.Add(path);
        }
    }

    /// <summary>The argument bound to one parameter: the raw argument, its path, and the bindings of the scope it was written in.</summary>
    private sealed record Binding(RuleNode Node, string Path, IReadOnlyDictionary<string, Binding>? Scope);

    /// <summary>
    /// Writes a raw, unexpanded parse tree in the JSON tree format, with its <c>definitions</c> section and its
    /// <c>use</c> nodes. <see cref="Printing.TreeFormatEmitter"/> writes a compiled tree, which has neither.
    /// </summary>
    private static class RawTreeEmitter
    {
        public static void EmitDocument(RuleNode root, Printing.ITreeWriter writer)
        {
            if (root is not RuleDocumentNode document)
            {
                Emit(root, writer, null);
                return;
            }

            // The definitions section is written first, inside the object of the root node.
            Emit(
                document.Body,
                writer,
                () =>
                {
                    writer.Property("definitions");
                    writer.BeginObject();
                    HashSet<string> written = [with(StringComparer.OrdinalIgnoreCase)];
                    foreach (DefinitionNode definition in document.Definitions.Where(d => written.Add(d.Name)))
                    {
                        writer.Property(definition.Name);
                        writer.BeginObject();
                        if (definition.Parameters.Count > 0)
                        {
                            writer.Property("parameters");
                            writer.BeginArray();
                            foreach (string parameter in definition.Parameters)
                            {
                                writer.String(parameter, quoted: false);
                            }

                            writer.EndArray();
                        }

                        writer.Property("body");
                        Emit(definition.Body, writer, null);
                        writer.EndObject();
                    }

                    writer.EndObject();
                }
            );
        }

        // Writes one node. The prefix, when given, writes members first inside the object of the node.
        private static void Emit(RuleNode node, Printing.ITreeWriter writer, Action? prefix)
        {
            writer.BeginObject();
            prefix?.Invoke();
            switch (node)
            {
                case ConstantNode constant:
                    writer.Property("const");
                    WriteConstant(constant.Value, writer);
                    break;
                case TermNode term:
                    WriteTerm(term, writer);
                    break;
                case ParameterReferenceNode parameter:
                    // A bare predicate with no arguments inside a body is the parameter of that name.
                    writer.Property("predicate");
                    writer.String(parameter.Name, quoted: false);
                    break;
                case UseNode use:
                    writer.Property("op");
                    writer.String("use", quoted: false);
                    writer.Property("name");
                    writer.String(use.Name, quoted: false);
                    if (use.Arguments.Count > 0)
                    {
                        WriteOperands(use.Arguments, writer);
                    }

                    break;
                case QuantifierNode quantifier:
                    writer.Property("op");
                    writer.String(quantifier.Kind == QuantifierKind.ForAll ? "forall" : "exists", quoted: false);
                    writer.Property("var");
                    writer.String(quantifier.Variable, quoted: false);
                    writer.Property("in");
                    writer.String(quantifier.CollectionName, quoted: false);
                    WriteOperands([quantifier.Body], writer);
                    break;
                default:
                    WriteOperator(node, writer);
                    break;
            }

            if (node.Note is { } note)
            {
                writer.Property("note");
                writer.String(note, quoted: true);
            }

            writer.EndObject();
        }

        private static void WriteOperator(RuleNode node, Printing.ITreeWriter writer)
        {
            string opName = node switch
            {
                NotNode => "Not",
                AndNode => "And",
                OrNode => "Or",
                XorNode => "Xor",
                EquivalentNode => "Equivalent",
                ImpliesNode => "Implies",
                NandNode => "Nand",
                NorNode => "Nor",
                ParityNode => "Parity",
                AnyNode => "Any",
                AllNode => "All",
                NoneNode => "None",
                ExactlyOneNode => "ExactlyOne",
                ThresholdNode threshold => threshold.Comparison.ToString(),
                BetweenNode => "Between",
                CoalesceNode => "Coalesce",
                IfNode => "If",
                InspectionNode inspection => inspection.Kind.ToString(),
                _ => throw new InvalidOperationException($"Unhandled rule node type '{node.GetType()}'."),
            };
            writer.Property("op");
            writer.String(TreeFormatOpNames.ToTreeFormat(opName), quoted: false);
            WriteOperands(RuleNode.OperandsOf(node), writer);
            if (node is BetweenNode between)
            {
                writer.Property("min");
                writer.Int64(between.Min);
                writer.Property("max");
                writer.Int64(between.Max);
            }
            else if (node is ThresholdNode threshold)
            {
                writer.Property("k");
                writer.Int64(threshold.K);
            }
        }

        private static void WriteOperands(IReadOnlyList<RuleNode> operands, Printing.ITreeWriter writer)
        {
            writer.Property("operands");
            writer.BeginArray();
            foreach (RuleNode operand in operands)
            {
                Emit(operand, writer, null);
            }

            writer.EndArray();
        }

        private static void WriteConstant(TruthValue value, Printing.ITreeWriter writer)
        {
            switch (value)
            {
                case TruthValue.True:
                    writer.Boolean(true);
                    break;
                case TruthValue.False:
                    writer.Boolean(false);
                    break;
                default:
                    writer.String(TruthValueText.TreeFormat(value), quoted: false);
                    break;
            }
        }

        private static void WriteTerm(TermNode term, Printing.ITreeWriter writer)
        {
            writer.Property("predicate");
            writer.String(term.PredicateName, quoted: false);
            if (term.Arguments.Count == 0)
            {
                return;
            }

            writer.Property("args");
            writer.BeginObject();
            foreach (ArgumentNode argument in term.Arguments)
            {
                writer.Property(argument.Name);
                WriteLiteral(argument.Value, writer);
            }

            writer.EndObject();
        }

        private static void WriteLiteral(RawLiteral literal, Printing.ITreeWriter writer)
        {
            switch (literal.Form)
            {
                case RawLiteralForm.Boolean:
                    writer.Boolean(literal.BooleanValue);
                    break;
                case RawLiteralForm.Number
                    when long.TryParse(literal.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out long whole):
                    writer.Int64(whole);
                    break;
                case RawLiteralForm.Number
                    when decimal.TryParse(literal.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal fraction):
                    writer.Decimal(fraction);
                    break;
                case RawLiteralForm.Array:
                    writer.BeginArray();
                    foreach (RawLiteral element in literal.Elements ?? [])
                    {
                        WriteLiteral(element, writer);
                    }

                    writer.EndArray();
                    break;
                case RawLiteralForm.Variable:
                    writer.BeginObject();
                    writer.Property("from");
                    writer.String(literal.Text ?? string.Empty, quoted: true);
                    writer.Property("query");
                    writer.String(literal.Query ?? string.Empty, quoted: true);
                    writer.EndObject();
                    break;
                default:
                    // A quoted string, or a number too large for either numeric kind, which stays as written.
                    writer.String(literal.Text ?? string.Empty, quoted: true);
                    break;
            }
        }
    }
}
