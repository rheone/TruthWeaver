namespace TruthWeaver.Tests;

using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Testing;
using TruthWeaver.Tests.DocExamples;
using TruthWeaver.Tests.TestSupport;
using EvaluationResults = global::Json.Schema.EvaluationResults;
using JsonSchema = global::Json.Schema.JsonSchema;

/// <summary>
/// <see cref="DecisionRecord"/>: a decision written as JSON for an audit log and read back for replay, with the rule
/// identity, the evaluation options and the redaction of literal argument values.
/// </summary>
public sealed class DecisionRecordTests
{
    private const string FaultingRule =
        "hasRole(role: \"admin\") AND check(n: 5, d: 1.50, b: true, at: \"2025-01-02T03:04:05+02:00\", "
        + "id: \"3fa85f64-5717-4562-b3fc-2c963f66afa6\", list: [1, 2], names: [\"x\", \"y\"]) "
        + "AND takesInt64(v: from(\"user\", \"$.age\"))";

    private static readonly JsonSchema Schema = JsonSchema.FromFile(
        Path.Combine(AppContext.BaseDirectory, "TestSupport", "decision-record.schema.json")
    );

    /// <summary>A decision with faults of every literal kind, a variable failure, a trace and a trace tree reads back equal to the written record when the values are included.</summary>
    [Fact]
    public async Task TryParseJson_RecordWrittenWithArgumentValues_ReadsBackAnEqualRecord_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(FaultingRule);
        EvaluationOptions options = new(FaultBudget: 5, Mode: EvaluationMode.Exhaustive, Timeout: TimeSpan.FromSeconds(2.5));
        DecisionRecord written = new(await EvaluateAsync(rule, options), RuleIdentity.Of(rule, "access", "3.1"), options);

        DecisionRecord record = Read(written.PrintJson(new DecisionJsonOptions(IncludeArgumentValues: true)));

        Assert.Equal(2, record.Decision.Faults.Count);
        Assert.NotNull(record.Decision.Trace);
        Assert.NotNull(record.Decision.TraceTree);
        Assert.Equal(written.Decision, record.Decision);
        Assert.Equal(written, record);
    }

    /// <summary>A short-circuited decision keeps its skipped nodes (no result, no children) through the round trip.</summary>
    [Fact]
    public async Task TryParseJson_ShortCircuitedDecision_KeepsTheSkippedNodes_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(
            "hasRole(role: \"admin\") OR (check(n: 1, d: 1, b: false, at: \"2025-01-01T00:00:00Z\", id: \"3fa85f64-5717-4562-b3fc-2c963f66afa6\", list: [], names: []) AND hasRole(role: \"x\"))"
        );
        DecisionRecord written = new(await EvaluateAsync(rule, EvaluationOptions.Default));

        DecisionRecord record = Read(written.PrintJson());

        Assert.Equal(written, record);
        Assert.Contains(record.Decision.Trace!.Entries, entry => entry.NotEvaluated && entry.Result is null);
    }

    /// <summary>A decision with no trace, no rule and no options writes none of those members and reads back equal.</summary>
    [Fact]
    public void PrintJson_DecisionWithoutTraceRuleOrOptions_OmitsThoseMembersAndRoundTrips_Test()
    {
        DecisionRecord written = new(new Decision(TruthValue.True, EquatableArray<Fault>.Empty));

        string json = written.PrintJson();
        DecisionRecord record = Read(json);

        JsonObject root = JsonNode.Parse(json)!.AsObject();
        Assert.Equal(["formatVersion", "argumentValues", "result", "faults"], root.Select(member => member.Key));
        Assert.Equal(written, record);
    }

    /// <summary>By default a faulting term is written with the name and kind of each literal argument but not its value, and the record says so.</summary>
    [Fact]
    public async Task PrintJson_DefaultOptions_OmitsLiteralArgumentValuesOfFaultingTerms_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(FaultingRule);
        DecisionRecord written = new(await EvaluateAsync(rule, new EvaluationOptions(Mode: EvaluationMode.Exhaustive)));

        JsonObject root = JsonNode.Parse(written.PrintJson())!.AsObject();

        Assert.Equal("omitted", root["argumentValues"]!.GetValue<string>());
        JsonArray arguments = root["faults"]![0]!["term"]!["arguments"]!.AsArray();
        Assert.Equal(7, arguments.Count);
        Assert.All(arguments, argument => Assert.Equal(["name", "kind"], argument!.AsObject().Select(member => member.Key)));
        Assert.Contains(
            arguments,
            argument => argument!["name"]!.GetValue<string>() == "d" && argument["kind"]!.GetValue<string>() == "Decimal"
        );
    }

    /// <summary>A record written without the values reads back with fault terms that keep their predicate and variables but have no literal arguments.</summary>
    [Fact]
    public async Task TryParseJson_RecordWrittenWithoutArgumentValues_ReadsFaultTermsWithoutLiteralArguments_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(FaultingRule);
        DecisionRecord written = new(await EvaluateAsync(rule, new EvaluationOptions(Mode: EvaluationMode.Exhaustive)));

        DecisionRecord record = Read(written.PrintJson());

        Assert.Equal(written.Decision.Result, record.Decision.Result);
        Assert.Equal(written.Decision.Trace, record.Decision.Trace);
        TermIdentity check = record.Decision.Faults[0].Term;
        Assert.Equal("check", check.PredicateName);
        Assert.Empty(check.Arguments);
        Assert.Equal(written.Decision.Faults[1], record.Decision.Faults[1]);
    }

    /// <summary>With the opt-in each literal argument value is written in its JSON form: numbers, booleans, strings and arrays.</summary>
    [Fact]
    public async Task PrintJson_IncludeArgumentValues_WritesEachLiteralValue_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(FaultingRule);
        DecisionRecord written = new(await EvaluateAsync(rule, new EvaluationOptions(Mode: EvaluationMode.Exhaustive)));

        using JsonDocument document = JsonDocument.Parse(
            written.PrintJson(new DecisionJsonOptions(IncludeArgumentValues: true))
        );

        JsonElement root = document.RootElement;
        Assert.Equal("included", root.GetProperty("argumentValues").GetString());
        using JsonElement.ArrayEnumerator arguments = root.GetProperty("faults")[0]
            .GetProperty("term")
            .GetProperty("arguments")
            .EnumerateArray();
        Dictionary<string, string> values = arguments.ToDictionary(
            a => a.GetProperty("name").GetString()!,
            a => Compact(a.GetProperty("value").GetRawText())
        );
        Assert.Equal("true", values["b"]);
        Assert.Equal("1.50", values["d"]);
        Assert.Equal("5", values["n"]);
        Assert.Equal("[1,2]", values["list"]);
        Assert.Equal("[\"x\",\"y\"]", values["names"]);
        Assert.Equal("\"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", values["id"]);
        Assert.Equal("\"2025-01-02T03:04:05.0000000+02:00\"", values["at"]);
    }

    /// <summary>
    /// The trace keeps the redaction of the evaluation: a value that a data source resolved is not in the record unless the
    /// evaluation set <see cref="EvaluationOptions.IncludeResolvedValues"/>.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task PrintJson_ResolvedVariableValue_IsInTheTraceOnlyWhenTheEvaluationIncludedIt_Test(
        bool includeResolvedValues,
        bool expected
    )
    {
        CompiledRule<RuleTestContext> rule = Compile("takesInt64(v: from(\"user\", \"$.age\"))");
        EvaluationOptions options = new(IncludeResolvedValues: includeResolvedValues);
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            dataSources: new DataSources { ["user"] = new FakeDataSource().With("$.age", 4242L) },
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        string json = new DecisionRecord(decision, Options: options).PrintJson(
            new DecisionJsonOptions(IncludeArgumentValues: true)
        );

        Assert.Equal(expected, json.Contains("4242", StringComparison.Ordinal));
    }

    /// <summary>A fault reads back with a <see cref="RecordedFaultException"/> that keeps the type name and message, and a variable failure as a real <see cref="VariableResolutionException"/>.</summary>
    [Fact]
    public async Task TryParseJson_Faults_ReadBackWithTheRecordedTypeMessageAndVariableFailure_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(FaultingRule);
        DecisionRecord written = new(await EvaluateAsync(rule, new EvaluationOptions(Mode: EvaluationMode.Exhaustive)));

        DecisionRecord record = Read(written.PrintJson());

        RecordedFaultException recorded = Assert.IsType<RecordedFaultException>(record.Decision.Faults[0].Exception);
        Assert.Equal("System.InvalidOperationException", recorded.ExceptionType);
        Assert.Equal("boom", recorded.Message);
        Assert.Equal("System.InvalidOperationException", record.Decision.Faults[0].ExceptionType);
        VariableResolutionException variable = Assert.IsType<VariableResolutionException>(record.Decision.Faults[1].Exception);
        Assert.Equal(new VariableReference("user", "$.age"), variable.Reference);
        Assert.Equal(VariableFailureKind.UnsuppliedSource, variable.Kind);
    }

    /// <summary>The rule identity is the SHA-256 hash of the canonical text, so two spellings of one rule have the same hash; the name and version are the caller's.</summary>
    [Fact]
    public void RuleIdentityOf_TwoSpellingsOfOneRule_HaveTheSameCanonicalTextHash_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasRole(role: \"a\") AND hasRole(role: \"b\")");
        CompiledRule<RuleTestContext> respelled = Compile("(hasRole(role: \"a\")) and (hasRole(role: \"b\"))");

        RuleIdentity identity = RuleIdentity.Of(rule, "access", "3.1");

        string expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rule.CanonicalText)));
        Assert.Equal(new RuleIdentity(expected, "access", "3.1"), identity);
        Assert.Equal(identity.CanonicalTextSha256, RuleIdentity.Of(respelled).CanonicalTextSha256);
    }

    /// <summary>The <see cref="TextWriter"/> overload writes exactly the text of the string form.</summary>
    [Fact]
    public async Task PrintJson_TextWriter_WritesTheSameTextAsTheStringForm_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile(FaultingRule);
        DecisionRecord written = new(await EvaluateAsync(rule, EvaluationOptions.Default), RuleIdentity.Of(rule));
        await using StringWriter writer = new();

        written.PrintJson(writer);

        Assert.Equal(written.PrintJson(), writer.ToString());
        Assert.EndsWith("}\n", writer.ToString(), StringComparison.Ordinal);
    }

    /// <summary>The <see cref="TextWriter"/> overload rejects a null writer.</summary>
    [Fact]
    public void PrintJson_NullWriter_ThrowsArgumentNullException_Test()
    {
        DecisionRecord written = new(new Decision(TruthValue.True, EquatableArray<Fault>.Empty));

        Assert.Throws<ArgumentNullException>(() => written.PrintJson((TextWriter)null!));
    }

    /// <summary>Every record the writer produces, with and without the values, is valid under the published schema.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrintJson_WrittenRecord_IsValidUnderTheSchema_Test(bool includeArgumentValues)
    {
        CompiledRule<RuleTestContext> rule = Compile(FaultingRule);
        EvaluationOptions options = new(Mode: EvaluationMode.Exhaustive, Timeout: TimeSpan.FromMilliseconds(1500));
        DecisionRecord written = new(await EvaluateAsync(rule, options), RuleIdentity.Of(rule, "access"), options);
        using JsonDocument document = JsonDocument.Parse(written.PrintJson(new DecisionJsonOptions(includeArgumentValues)));

        EvaluationResults results = Schema.Evaluate(document.RootElement);

        Assert.True(results.IsValid);
    }

    /// <summary>Bad input is refused with the JSON path of the first problem, and the schema refuses the same documents.</summary>
    [Theory]
    [InlineData("not json", "$:")]
    [InlineData("""{"formatVersion":2,"argumentValues":"omitted","result":"True","faults":[]}""", "$.formatVersion:")]
    [InlineData("""{"formatVersion":1,"argumentValues":"omitted","faults":[]}""", "$.result:")]
    [InlineData("""{"formatVersion":1,"argumentValues":"omitted","result":"Maybe","faults":[]}""", "$.result:")]
    [InlineData("""{"formatVersion":1,"argumentValues":"omitted","result":"True","faults":[],"extra":1}""", "$.extra:")]
    [InlineData(
        """{"formatVersion":1,"rule":{"canonicalTextSha256":"ABC"},"argumentValues":"omitted","result":"True","faults":[]}""",
        "$.rule.canonicalTextSha256:"
    )]
    [InlineData(
        """{"formatVersion":1,"options":{"faultBudget":0,"mode":"ShortCircuit","timeout":null,"includeResolvedValues":false},"argumentValues":"omitted","result":"True","faults":[]}""",
        "$.options.faultBudget:"
    )]
    [InlineData(
        """{"formatVersion":1,"argumentValues":"omitted","result":"Unknown","faults":[{"term":{"predicate":"p","arguments":[{"name":"a","kind":"Int64","value":1}],"variables":[]},"exception":{"type":"X","message":"m"}}]}""",
        "$.faults[0].term.arguments[0]:"
    )]
    [InlineData(
        """{"formatVersion":1,"argumentValues":"included","result":"Unknown","faults":[{"term":{"predicate":"p","arguments":[{"name":"a","kind":"Int64","value":"1"}],"variables":[]},"exception":{"type":"X","message":"m"}}]}""",
        "$.faults[0].term.arguments[0].value:"
    )]
    [InlineData(
        """{"formatVersion":1,"argumentValues":"omitted","result":"True","faults":[],"traceTree":{"text":"a","result":null,"notEvaluated":false,"children":[]}}""",
        "$.traceTree.result:"
    )]
    public void TryParseJson_InvalidRecord_ReportsThePathOfTheProblem_Test(string json, string expectedPrefix)
    {
        bool read = DecisionRecord.TryParseJson(json, out DecisionRecord? record, out string? error);

        Assert.False(read);
        Assert.Null(record);
        Assert.StartsWith(expectedPrefix, error, StringComparison.Ordinal);
    }

    /// <summary>The schema refuses the structurally invalid records that the reader refuses.</summary>
    [Theory]
    [InlineData("""{"formatVersion":2,"argumentValues":"omitted","result":"True","faults":[]}""")]
    [InlineData("""{"formatVersion":1,"argumentValues":"omitted","faults":[]}""")]
    [InlineData("""{"formatVersion":1,"argumentValues":"omitted","result":"True","faults":[],"extra":1}""")]
    [InlineData(
        """{"formatVersion":1,"rule":{"canonicalTextSha256":"ABC"},"argumentValues":"omitted","result":"True","faults":[]}"""
    )]
    [InlineData(
        """{"formatVersion":1,"options":{"faultBudget":0,"mode":"ShortCircuit","timeout":null,"includeResolvedValues":false},"argumentValues":"omitted","result":"True","faults":[]}"""
    )]
    public void Schema_InvalidRecord_IsRefused_Test(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);

        Assert.False(Schema.Evaluate(document.RootElement).IsValid);
    }

    /// <summary>The JSON sample in <c>docs/decision-records.md</c> is the exact output of <see cref="DecisionRecord.PrintJson(DecisionJsonOptions?)"/> for the rule the page names.</summary>
    [Fact]
    public async Task PrintJson_DocumentationSample_MatchesThePage_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("lookup(id: 42) OR hasRole(role: \"admin\")");
        DecisionRecord written = new(
            await EvaluateAsync(rule, EvaluationOptions.Default),
            RuleIdentity.Of(rule, "access", "3"),
            EvaluationOptions.Default
        );
        string page = await File.ReadAllTextAsync(
            Path.Combine(DocExampleChecker.FindRepositoryRoot(), "docs", "decision-records.md"),
            TestContext.Current.CancellationToken
        );
        const string Fence = "```json\n";
        int start = page.IndexOf(Fence, StringComparison.Ordinal) + Fence.Length;
        string sample = page[start..page.IndexOf("```", start, StringComparison.Ordinal)];

        Assert.Equal(written.PrintJson(), sample);
    }

    private static DecisionRecord Read(string json)
    {
        Assert.True(DecisionRecord.TryParseJson(json, out DecisionRecord? record, out string? error), error);
        return record;
    }

    // The record is indented; none of the sample values holds a space, so dropping white space compacts the JSON.
    private static string Compact(string json)
    {
        return string.Concat(json.Where(c => !char.IsWhiteSpace(c)));
    }

    private static CompiledRule<RuleTestContext> Compile(string text)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "hasRole",
                    "hasRole",
                    "Always true.",
                    [new PredicateArgumentSchema("role", "The role.", LiteralKind.String)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Add(
                new PredicateSchema(
                    "check",
                    "check",
                    "Always throws.",
                    [
                        new PredicateArgumentSchema("n", "An integer.", LiteralKind.Int64),
                        new PredicateArgumentSchema("d", "A decimal.", LiteralKind.Decimal),
                        new PredicateArgumentSchema("b", "A boolean.", LiteralKind.Boolean),
                        new PredicateArgumentSchema("at", "An instant.", LiteralKind.DateTimeOffset),
                        new PredicateArgumentSchema("id", "A GUID.", LiteralKind.Guid),
                        new PredicateArgumentSchema("list", "Integers.", LiteralKind.Int64Array),
                        new PredicateArgumentSchema("names", "Strings.", LiteralKind.StringArray),
                    ]
                ),
                (_, _, _) => throw new InvalidOperationException("boom")
            )
            .Add(
                new PredicateSchema(
                    "lookup",
                    "lookup",
                    "Always throws.",
                    [new PredicateArgumentSchema("id", "An identifier.", LiteralKind.Int64)]
                ),
                (_, _, _) => throw new InvalidOperationException("Directory unavailable.")
            )
            .Add(
                new PredicateSchema(
                    "takesInt64",
                    "takesInt64",
                    "True for any value.",
                    [new PredicateArgumentSchema("v", "A value.", LiteralKind.Int64)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry, new CompilerOptions(DataSources: ["user"]));
        CompilationResult<RuleTestContext> result = compiler.Compile(text);
        Assert.True(result.Succeeded, string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        return result.CompiledRule!;
    }

    private static Task<Decision> EvaluateAsync(CompiledRule<RuleTestContext> rule, EvaluationOptions options)
    {
        return rule.EvaluateAsync(
            new RuleTestContext(),
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );
    }
}
