namespace TruthWeaver.Testing;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;

/// <summary>
/// Compiles a rule over stand-in predicates that give stub values. A rule author can then evaluate the rule with a
/// <see cref="StubValues"/> as the context, with no host and no registry of real predicates.
/// </summary>
/// <remarks>
/// <para>The compiler first reads the rule permissively: any predicate name and any argument is accepted
/// (<see cref="CompilationMode.Lenient"/> over an empty registry), and any <c>from("source", "query")</c> source is
/// declared. It then registers one stand-in for each predicate that the rule uses, with each argument that the rule gives
/// it, all optional, and compiles the rule again over these stand-ins.</para>
/// <para>The kind of an argument comes from the literal in the rule: a string, a whole number, a decimal number, a Boolean
/// or a list of one of them. An argument that has only <c>from(...)</c> references is a string, so a data value of another
/// kind is a fault.</para>
/// <para>A stand-in answers with the value of its term in the <see cref="StubValues"/>, found by the canonical text of the
/// term with the values that the predicate receives. For <c>inRegion(region: from("user", "$.region"))</c> with
/// <c>{"region": "EU"}</c> as <c>user</c>, that is the value of <c>inRegion(region: "EU")</c>. A term with no value is
/// <see cref="TruthValue.Unknown"/>, and a reference that does not resolve is a fault.</para>
/// </remarks>
public static class StubRuleCompiler
{
    private const string StandInDescription = "A stand-in that gives the stub value of its term.";

    /// <summary>Compiles a rule over stand-in predicates.</summary>
    /// <param name="source">The rule in <paramref name="format"/>.</param>
    /// <param name="format">The format of <paramref name="source"/>.</param>
    /// <param name="options">
    /// The compile limits, or <see langword="null"/> for <see cref="CompilerOptions.Default"/>. The mode, the lints and the
    /// data source declarations of the options are not used.
    /// </param>
    /// <returns>The compiled rule, which evaluates with a <see cref="StubValues"/> context, or the diagnostics.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static CompilationResult<StubValues> Compile(string source, RuleFormat format, CompilerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        CompilerOptions permissiveOptions = (options ?? CompilerOptions.Default) with
        {
            Mode = CompilationMode.Lenient,
            Lints = LintRules.None,
            DataSources = null,
        };
        RuleCompiler<StubValues> permissive = new(PredicateRegistry<StubValues>.CreateBuilder().Build(), permissiveOptions);
        CompilationResult<StubValues> read = permissive.CompileAs(source, format);
        if (!read.TryGetRule(out CompiledRule<StubValues>? compiled, out _))
        {
            return read;
        }

        // The JSON tree names each predicate with the literal and variable arguments of each call, so the stand-ins can
        // declare exactly what the rule gives them without the internal expression types.
        Shapes shapes = new();
        shapes.Visit(JsonNode.Parse(compiled.PrintJson(includeNotes: false)));

        RuleCompiler<StubValues> standIns = new(
            shapes.BuildRegistry(),
            permissiveOptions with
            {
                Mode = CompilationMode.Strict,
                DataSources = shapes.Sources,
            }
        );
        return standIns.CompileAs(source, format);
    }

    /// <summary>Compiles a rule over stand-in predicates without a result object: the <c>Try</c> form of <see cref="Compile"/>.</summary>
    /// <param name="source">The rule in <paramref name="format"/>.</param>
    /// <param name="format">The format of <paramref name="source"/>.</param>
    /// <param name="rule">The compiled rule when this method returns <see langword="true"/>; otherwise <see langword="null"/>.</param>
    /// <param name="diagnostics">Every diagnostic, including warnings on success.</param>
    /// <param name="options">The compile limits, or <see langword="null"/> for <see cref="CompilerOptions.Default"/>.</param>
    /// <returns><see langword="true"/> when a rule was produced.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static bool TryCompile(
        string source,
        RuleFormat format,
        [NotNullWhen(true)] out CompiledRule<StubValues>? rule,
        out EquatableArray<Diagnostic> diagnostics,
        CompilerOptions? options = null
    )
    {
        return Compile(source, format, options).TryGetRule(out rule, out diagnostics);
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

        public PredicateRegistry<StubValues> BuildRegistry()
        {
            PredicateRegistryBuilder<StubValues> builder = PredicateRegistry<StubValues>.CreateBuilder();
            foreach (Predicate predicate in this.predicates.Values)
            {
                List<PredicateArgumentSchema> arguments =
                [
                    .. predicate.Arguments.Select(argument => new PredicateArgumentSchema(
                        argument.Key,
                        StandInDescription,
                        argument.Value ?? LiteralKind.String,
                        Required: false
                    )),
                ];
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
        private static Func<StubValues, PredicateArguments, CancellationToken, ValueTask<TruthValue>> StandIn(
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
