namespace TruthWeaver.Tool.Core;

using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

/// <summary>
/// Evaluates a loaded rule with stub values in place of a host's predicates. The tool has no host registry, so it
/// registers a stand-in for each predicate that the rule uses: the stand-in gives the stub value of its term, or
/// <c>Unknown</c> when the term has no stub value.
/// </summary>
/// <remarks>
/// <para>The stand-in of a predicate declares every argument that the rule gives it, each one optional. The kind of an
/// argument comes from the predicate schema file when the file declares it, and otherwise from the literal in the rule
/// (a string, a whole number, a decimal number, a Boolean or a list of one of them). An argument that has only
/// <c>from(...)</c> references and no declared kind is a string.</para>
/// <para>A stand-in finds its stub value by the canonical text of its term, with the values that the predicate receives.
/// For a term with a <c>from("source", "query")</c> argument, that is the value that the data source gives: with
/// <c>{"region": "EU"}</c> as <c>user</c>, the term <c>inRegion(region: from("user", "$.region"))</c> takes the stub value of
/// <c>inRegion(region: "EU")</c>. A reference that does not resolve is a fault, and the term is <c>Unknown</c>.</para>
/// </remarks>
public static class StubEvaluator
{
    private const string StandInDescription = "A stand-in that gives the stub value of its term.";

    /// <summary>Compiles a loaded rule over stand-in predicates and evaluates it with stub values.</summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="StubEvaluationOptions.Default"/>.</param>
    /// <param name="cancellationToken">Cancels the evaluation.</param>
    /// <returns>The rule and its decision, or no value and the diagnostics when the rule does not compile or the schema file is not usable.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The evaluation options have a fault budget below 1.</exception>
    /// <exception cref="OperationCanceledException">The token, or the timeout of the evaluation options, cancelled the evaluation.</exception>
    public static async Task<ToolResult<StubEvaluation>> EvaluateAsync(
        LoadedRule rule,
        StubEvaluationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(rule);
        options ??= StubEvaluationOptions.Default;

        CompilationResult<StubAssignment> permissive = RuleConverter.Compile(rule, options.Settings);
        if (!permissive.TryGetRule(out CompiledRule<StubAssignment>? compiled, out EquatableArray<Diagnostic> diagnostics))
        {
            return ToolResults.Failure<StubEvaluation>(diagnostics);
        }

        PredicateSchemaFile? schema = options.PredicateSchema;
        if (schema is null && options.Settings.SchemaFile is { } schemaPath)
        {
            ToolResult<PredicateSchemaFile> loaded = PredicateSchemaFile.Load(schemaPath);
            if (!loaded.TryGetValue(out schema, out EquatableArray<Diagnostic> schemaDiagnostics))
            {
                return ToolResults.Failure<StubEvaluation>(schemaDiagnostics);
            }
        }

        // The JSON tree names each predicate with the literal and variable arguments of each call, so the stand-ins can
        // declare exactly what the rule gives them without the internal expression types.
        Shapes shapes = new();
        shapes.Visit(JsonNode.Parse(compiled.PrintJson(includeNotes: false)));

        CompilerOptions standInOptions = options.Settings.ToCompilerOptions() with
        {
            Lints = LintRules.None,
            DataSources = shapes.Sources,
        };
        RuleCompiler<StubAssignment> compiler = new(shapes.BuildRegistry(schema), standInOptions);
        CompilationResult<StubAssignment> standIn = compiler.CompileAs(rule.Text, rule.Format);
        if (!standIn.TryGetRule(out CompiledRule<StubAssignment>? evaluable, out EquatableArray<Diagnostic> standInDiagnostics))
        {
            return ToolResults.Failure<StubEvaluation>(
                diagnostics.Concat(standInDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            );
        }

        Decision decision = await evaluable
            .EvaluateAsync(
                options.Stubs,
                dataSources: options.DataSources,
                options: options.Evaluation,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return ToolResults.Success(new StubEvaluation(evaluable, decision, options.Evaluation), diagnostics);
    }

    /// <summary>Collects, for each predicate of a JSON rule tree, its arguments and their kinds, and the data source names.</summary>
    private sealed class Shapes
    {
        // Predicate and argument names match ignoring case, as in the compiler; the first spelling in the rule is kept.
        private readonly Dictionary<string, Predicate> predicates = [with(StringComparer.OrdinalIgnoreCase)];

        public DataSourceDeclarations Sources { get; } = [];

        public void Visit(JsonNode? node)
        {
            if (node is not JsonObject tree)
            {
                return;
            }

            if (tree["predicate"] is JsonValue name)
            {
                this.VisitTerm(name.GetValue<string>(), tree["args"] as JsonObject ?? []);
                return;
            }

            foreach (JsonNode? operand in tree["operands"] as JsonArray ?? [])
            {
                this.Visit(operand);
            }
        }

        public PredicateRegistry<StubAssignment> BuildRegistry(PredicateSchemaFile? schema)
        {
            PredicateRegistryBuilder<StubAssignment> builder = PredicateRegistry<StubAssignment>.CreateBuilder();
            foreach (Predicate predicate in this.predicates.Values)
            {
                PredicateSchema? declared = schema?.Predicates.FirstOrDefault(p =>
                    string.Equals(p.Name, predicate.Name, StringComparison.OrdinalIgnoreCase)
                );
                List<PredicateArgumentSchema> arguments = [];
                foreach ((string argument, LiteralKind? kind) in predicate.Arguments)
                {
                    LiteralKind? declaredKind = declared
                        ?.Arguments.FirstOrDefault(a => string.Equals(a.Name, argument, StringComparison.OrdinalIgnoreCase))
                        ?.Type;
                    arguments.Add(
                        new PredicateArgumentSchema(
                            argument,
                            StandInDescription,
                            declaredKind ?? kind ?? LiteralKind.String,
                            Required: false
                        )
                    );
                }

                _ = builder.Add(
                    new PredicateSchema(predicate.Name, predicate.Name, StandInDescription, arguments),
                    StandIn(predicate.Name, [.. predicate.Arguments.Keys])
                );
            }

            return builder.Build();
        }

        /// <summary>Gives the list kind of a single kind, such as <see cref="LiteralKind.StringArray"/> for <see cref="LiteralKind.String"/>.</summary>
        private static LiteralKind ArrayOf(LiteralKind kind)
        {
            return kind switch
            {
                LiteralKind.Int64 => LiteralKind.Int64Array,
                LiteralKind.Decimal => LiteralKind.DecimalArray,
                LiteralKind.Boolean => LiteralKind.BooleanArray,
                LiteralKind.DateTimeOffset => LiteralKind.DateTimeOffsetArray,
                LiteralKind.Guid => LiteralKind.GuidArray,
                _ => LiteralKind.StringArray,
            };
        }

        /// <summary>Gives the kind of a literal in the JSON tree, as the permissive compiler reads the same literal.</summary>
        private static LiteralKind KindOf(JsonNode? value)
        {
            if (value is JsonArray items)
            {
                return items.Count == 0 ? LiteralKind.StringArray : ArrayOf(KindOf(items[0]));
            }

            if (value is JsonValue scalar && scalar.TryGetValue(out bool _))
            {
                return LiteralKind.Boolean;
            }

            if (value is JsonValue number && !number.TryGetValue(out string? _))
            {
                // A number with a fraction or an exponent is a decimal, as the rule-text reader decides from the digits.
                string text = number.ToJsonString();
                return text.AsSpan().IndexOfAny(".eE") >= 0 ? LiteralKind.Decimal : LiteralKind.Int64;
            }

            return LiteralKind.String;
        }

        /// <summary>Joins the kinds of two uses of one argument: a whole number and a decimal number are both decimals.</summary>
        private static LiteralKind Join(LiteralKind first, LiteralKind second)
        {
            return (first, second) switch
            {
                (LiteralKind.Int64, LiteralKind.Decimal) or (LiteralKind.Decimal, LiteralKind.Int64) => LiteralKind.Decimal,
                (LiteralKind.Int64Array, LiteralKind.DecimalArray) or (LiteralKind.DecimalArray, LiteralKind.Int64Array) =>
                    LiteralKind.DecimalArray,
                _ => first,
            };
        }

        /// <summary>
        /// Creates the stand-in evaluation of one predicate: the stub value of the term with the values the predicate
        /// receives, found by the canonical text of that term.
        /// </summary>
        private static Func<StubAssignment, PredicateArguments, CancellationToken, ValueTask<TruthValue>> StandIn(
            string predicateName,
            IReadOnlyList<string> argumentNames
        )
        {
            return (stubs, arguments, _) =>
            {
                List<KeyValuePair<string, LiteralValue>> given = [];
                foreach (string name in argumentNames)
                {
                    if (arguments.TryGetRaw(name, out LiteralValue value))
                    {
                        given.Add(new KeyValuePair<string, LiteralValue>(name, value));
                    }
                }

                return ValueTask.FromResult(stubs.ValueOf(new TermIdentity(predicateName, given).ToString()));
            };
        }

        private void VisitTerm(string name, JsonObject arguments)
        {
            if (!this.predicates.TryGetValue(name, out Predicate? predicate))
            {
                predicate = new Predicate(name);
                this.predicates[name] = predicate;
            }

            foreach ((string argument, JsonNode? value) in arguments)
            {
                // A from(...) reference shows no kind; its value is known only when the data source answers.
                LiteralKind? kind = null;
                if (value is JsonObject reference && reference["from"] is JsonValue source)
                {
                    string sourceName = source.GetValue<string>();
                    if (!this.Sources.Names.Contains(sourceName))
                    {
                        this.Sources.Add(sourceName);
                    }
                }
                else
                {
                    kind = KindOf(value);
                }

                predicate.Arguments[argument] = (predicate.Arguments.GetValueOrDefault(argument), kind) switch
                {
                    (null, var next) => next,
                    (var known, null) => known,
                    ({ } known, { } next) => Join(known, next),
                };
            }
        }

        /// <summary>One predicate: its first spelling, and each argument with its kind, or <see langword="null"/> when only references give it.</summary>
        private sealed class Predicate(string name)
        {
            public string Name { get; } = name;

            public Dictionary<string, LiteralKind?> Arguments { get; } = [with(StringComparer.OrdinalIgnoreCase)];
        }
    }
}
