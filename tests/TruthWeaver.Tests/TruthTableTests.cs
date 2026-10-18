namespace TruthWeaver.Tests;

using System.Text.Json;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Testing.Fuzzing;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintTruthTable(TruthTableOptions?)"/>: the rows, the column and row order, the size
/// cap, the five formats and the <see cref="TextWriter"/> and <c>Try</c> forms.
/// </summary>
public sealed class TruthTableTests
{
    private static readonly TruthTableOptions Words = new()
    {
        Format = TruthTableFormat.Json,
        Constants = new ConstantFormat { Style = ConstantStyle.Word },
    };

    /// <summary>The default table is the Strong K3 table in Markdown: nine rows for two terms, the first term changing slowest, <c>False</c> before <c>Unknown</c> before <c>True</c>.</summary>
    [Fact]
    public void PrintTruthTable_DefaultOptionsTwoTerms_PrintsTheNineRowK3MarkdownTable_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        string table = rule.PrintTruthTable();

        Assert.Equal(
            """
            | a | b | Result |
            | --- | --- | --- |
            | F | F | F |
            | F | U | F |
            | F | T | F |
            | U | F | F |
            | U | U | U |
            | U | T | U |
            | T | F | F |
            | T | U | U |
            | T | T | T |
            """,
            table
        );
    }

    /// <summary>The classical option prints the four rows over <c>False</c> and <c>True</c> only.</summary>
    [Fact]
    public void PrintTruthTable_ClassicalOption_PrintsTheFourRowTable_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a OR b");

        string table = rule.PrintTruthTable(new TruthTableOptions { Classical = true });

        Assert.Equal(
            """
            | a | b | Result |
            | --- | --- | --- |
            | F | F | F |
            | F | T | T |
            | T | F | T |
            | T | T | T |
            """,
            table
        );
    }

    /// <summary>A classical table of a rule that holds the constant <c>Unknown</c> still shows <c>Unknown</c> in the result.</summary>
    [Fact]
    public void PrintTruthTable_ClassicalOptionWithUnknownConstant_ShowsUnknownInTheResult_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND Unknown");

        string table = rule.PrintTruthTable(new TruthTableOptions { Classical = true });

        Assert.Equal("| a | Result |\n| --- | --- |\n| F | F |\n| T | U |", table);
    }

    /// <summary>A rule with no term has one row and only the result column.</summary>
    [Fact]
    public void PrintTruthTable_RuleWithoutTerms_PrintsOneRowWithOnlyTheResult_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True OR Unknown");

        string table = rule.PrintTruthTable();

        Assert.Equal("| Result |\n| --- |\n| T |", table);
    }

    /// <summary>The columns follow the first occurrence of each term, depth first and left to right, and a repeated term has one column.</summary>
    [Fact]
    public void PrintTruthTable_RepeatedAndNestedTerms_OrdersColumnsByFirstOccurrence_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("(c AND a) OR (b AND a)");

        string header = rule.PrintTruthTable().Split('\n')[0];

        Assert.Equal("| c | a | b | Result |", header);
    }

    /// <summary>The columns are in the order of the legend of <c>PrintEquationWithLegend</c>, so a lettered table is read with that legend.</summary>
    [Fact]
    public void PrintTruthTable_SimpleVariables_UsesTheLettersOfTheEquationLegend_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("(hasCrust(crust: \"thin\") AND c) OR NOT a");
        EquationWithLegend legend = rule.PrintEquationWithLegend();

        using JsonDocument table = JsonDocument.Parse(rule.PrintTruthTable(Words with { SimpleVariables = true }));

        JsonElement[] terms = [.. Items(table.RootElement.GetProperty("terms"))];
        Assert.Equal(legend.Legend.Select(e => e.Variable), terms.Select(t => t.GetProperty("name").GetString()));
        Assert.Equal(legend.Legend.Select(e => e.TermText), terms.Select(t => t.GetProperty("term").GetString()));
    }

    /// <summary>With argument values hidden, a header is the predicate name only.</summary>
    [Fact]
    public void PrintTruthTable_ArgumentValuesHidden_ShowsThePredicateNameInTheHeader_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\")");

        string withValues = rule.PrintTruthTable().Split('\n')[0];
        string withoutValues = rule.PrintTruthTable(new TruthTableOptions { ShowArgumentValues = false }).Split('\n')[0];

        Assert.Equal("| hasCrust(crust: \"thin\") | Result |", withValues);
        Assert.Equal("| hasCrust | Result |", withoutValues);
    }

    /// <summary>The constant style sets the text of the cells.</summary>
    [Theory]
    [InlineData(ConstantStyle.Word, "| False | False |\n| Unknown | Unknown |\n| True | True |")]
    [InlineData(ConstantStyle.Letters, "| F | F |\n| U | U |\n| T | T |")]
    [InlineData(ConstantStyle.LogicSymbols, "| ⊥ | ⊥ |\n| U | U |\n| ⊤ | ⊤ |")]
    public void PrintTruthTable_ConstantStyle_WritesTheCellsInTheStyle_Test(ConstantStyle style, string expectedRows)
    {
        CompiledRule<RuleTestContext> rule = Compile("a");

        string table = rule.PrintTruthTable(new TruthTableOptions { Constants = new ConstantFormat { Style = style } });

        Assert.Equal(expectedRows, string.Join('\n', table.Split('\n').Skip(2)));
    }

    /// <summary>
    /// Over generated rules, every row's result is the value that <c>EvaluateAsync</c> gives with predicates that answer
    /// the row's term values.
    /// </summary>
    [Fact]
    public async Task PrintTruthTable_GeneratedRules_EveryRowEqualsEvaluateAsyncWithTheSameStubs_Test()
    {
        Random random = new(20261011);
        int checkedRows = 0;
        string[] names = ["a", "b", "c"];

        for (int i = 0; i < 120; i++)
        {
            GeneratedRule generated = K3RuleGenerator.GenerateRule(random, depth: 3, names);
            K3Rule? stubs = K3Rule.TryCreate(generated.Text, names.Length);
            if (stubs is null)
            {
                continue;
            }

            using JsonDocument table = JsonDocument.Parse(stubs.Compiled.PrintTruthTable(Words));
            string[] columns =
            [
                .. Items(table.RootElement.GetProperty("terms")).Select(t => t.GetProperty("name").GetString()!),
            ];
            foreach (JsonElement row in Items(table.RootElement.GetProperty("rows")))
            {
                // The stubs read the assignment by input position; a term the rule does not use stays False.
                TruthValue[] assignment = new TruthValue[names.Length];
                string?[] cells = [.. Items(row.GetProperty("values")).Select(v => v.GetString())];
                for (int column = 0; column < columns.Length; column++)
                {
                    assignment[Array.IndexOf(names, columns[column])] = Enum.Parse<TruthValue>(cells[column]!);
                }

                Decision decision = await stubs.EvaluateAsync(assignment, TestContext.Current.CancellationToken);

                Assert.True(
                    Enum.Parse<TruthValue>(row.GetProperty("result").GetString()!) == decision.Result,
                    $"{generated.Text} @ [{string.Join(", ", cells)}]"
                );
                checkedRows++;
            }
        }

        // Guard against a vacuous pass.
        Assert.True(checkedRows > 500, $"only {checkedRows} rows were checked");
    }

    /// <summary>The rows of a built-in connective equal the independent K3 oracle.</summary>
    [Fact]
    public void PrintTruthTable_Connectives_RowsEqualTheK3Oracle_Test()
    {
        (string Text, Func<TruthValue, TruthValue, TruthValue> Oracle)[] cases =
        [
            ("a XOR b", K3Oracle.Xor),
            ("a IMPLIES b", K3Oracle.Implies),
            ("a EQUIVALENT b", K3Oracle.Equivalent),
            ("a NAND b", K3Oracle.Nand),
            ("a NOR b", K3Oracle.Nor),
            ("a AND b", (x, y) => K3Oracle.And([x, y])),
            ("a OR b", (x, y) => K3Oracle.Or([x, y])),
        ];

        foreach ((string text, Func<TruthValue, TruthValue, TruthValue> oracle) in cases)
        {
            using JsonDocument table = JsonDocument.Parse(Compile(text).PrintTruthTable(Words));
            JsonElement[] rows = [.. Items(table.RootElement.GetProperty("rows"))];

            Assert.Equal(9, rows.Length);
            foreach ((TruthValue[] assignment, JsonElement row) in K3Oracle.Assignments(2).Zip(rows))
            {
                Assert.Equal(oracle(assignment[0], assignment[1]).ToString(), row.GetProperty("result").GetString());
            }
        }
    }

    /// <summary>A rule with a variable reference prints with the reference as a free term: no data source is read.</summary>
    [Fact]
    public void PrintTruthTable_RuleWithVariableReference_TreatsTheTermAsFree_Test()
    {
        DataSourceDeclarations declarations = [];
        declarations.Add("user");
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .Add(
                new PredicateSchema(
                    "ageAtLeast",
                    "Age at least",
                    "Test predicate, always true.",
                    [new PredicateArgumentSchema("min", "The minimum age.", LiteralKind.Int64)]
                ),
                (_, _, _) => ValueTask.FromResult(TruthValue.True)
            )
            .Build();
        CompiledRule<RuleTestContext> rule = new RuleCompiler<RuleTestContext>(
            registry,
            new CompilerOptions(DataSources: declarations)
        )
            .Compile("ageAtLeast(min: from(\"user\", \"$.minAge\"))")
            .CompiledRule!;

        string table = rule.PrintTruthTable();

        Assert.Equal(
            "| ageAtLeast(min: from(\"user\", \"$.minAge\")) | Result |\n| --- | --- |\n| F | F |\n| U | U |\n| T | T |",
            table
        );
    }

    /// <summary>A rule with exactly the default cap of terms prints in full, with no diagnostic.</summary>
    [Fact]
    public void TryPrintTruthTable_EightTerms_PrintsAllRowsWithoutDiagnostics_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileTerms(8);

        bool printed = rule.TryPrintTruthTable(null, out string? text, out IReadOnlyList<Diagnostic> diagnostics);

        Assert.True(printed);
        Assert.Empty(diagnostics);
        Assert.Equal(2 + 6561, text!.Split('\n').Length);
    }

    /// <summary>A rule over the cap is refused with a <c>TRE0035</c> error and no text, unless the options allow a larger table.</summary>
    [Fact]
    public void TryPrintTruthTable_NineTermsWithoutTheOption_IsRefusedWithAnError_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileTerms(9);

        bool printed = rule.TryPrintTruthTable(null, out string? text, out IReadOnlyList<Diagnostic> diagnostics);

        Assert.False(printed);
        Assert.Null(text);
        Diagnostic error = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.TruthTableTooLarge, error.Code);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("19,683 rows", error.Message, StringComparison.Ordinal);
        Assert.Contains("AllowLargerTable", error.Message, StringComparison.Ordinal);
    }

    /// <summary>With the larger-table option, a rule over the cap prints in full and the result carries a <c>TRE0034</c> warning.</summary>
    [Fact]
    public void TryPrintTruthTable_NineTermsWithTheOption_PrintsAllRowsWithAWarning_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileTerms(9);

        bool printed = rule.TryPrintTruthTable(
            new TruthTableOptions { AllowLargerTable = true },
            out string? text,
            out IReadOnlyList<Diagnostic> diagnostics
        );

        Assert.True(printed);
        Assert.Equal(2 + 19683, text!.Split('\n').Length);
        Diagnostic warning = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.TruthTableCapRaised, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Contains("19,683 rows", warning.Message, StringComparison.Ordinal);
    }

    /// <summary>The classical option counts its own rows: nine terms are 512 rows, and the cap still applies to the term count.</summary>
    [Fact]
    public void TryPrintTruthTable_NineTermsClassicalWithTheOption_PrintsFiveHundredTwelveRows_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileTerms(9);

        bool printed = rule.TryPrintTruthTable(
            new TruthTableOptions { AllowLargerTable = true, Classical = true },
            out string? text,
            out IReadOnlyList<Diagnostic> diagnostics
        );

        Assert.True(printed);
        Assert.Equal(2 + 512, text!.Split('\n').Length);
        Assert.Contains("512 rows", Assert.Single(diagnostics).Message, StringComparison.Ordinal);
    }

    /// <summary>The hard limit is 12 terms: 12 print with the option, 13 are refused whatever the options say.</summary>
    [Fact]
    public void TryPrintTruthTable_HardLimit_TwelveTermsPrintAndThirteenAreRefused_Test()
    {
        TruthTableOptions options = new() { AllowLargerTable = true, Classical = true };

        bool twelve = CompileTerms(12).TryPrintTruthTable(options, out string? text, out _);
        bool thirteen = CompileTerms(13)
            .TryPrintTruthTable(options, out string? none, out IReadOnlyList<Diagnostic> diagnostics);

        Assert.True(twelve);
        Assert.Equal(2 + 4096, text!.Split('\n').Length);
        Assert.False(thirteen);
        Assert.Null(none);
        Diagnostic error = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticCodes.TruthTableTooLarge, error.Code);
        Assert.Contains("hard limit of 12", error.Message, StringComparison.Ordinal);
    }

    /// <summary>The throwing form reports a refused table as an <see cref="InvalidOperationException"/> that carries the diagnostic message.</summary>
    [Fact]
    public void PrintTruthTable_NineTermsWithoutTheOption_ThrowsInvalidOperationException_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileTerms(9);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => rule.PrintTruthTable());

        Assert.Contains("cap of 8", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A refused table writes nothing to the writer.</summary>
    [Fact]
    public void TryPrintTruthTable_RefusedTableWithWriter_WritesNothing_Test()
    {
        CompiledRule<RuleTestContext> rule = CompileTerms(9);
        using StringWriter writer = new();

        bool printed = rule.TryPrintTruthTable(writer, null, out IReadOnlyList<Diagnostic> diagnostics);

        Assert.False(printed);
        Assert.Equal(string.Empty, writer.ToString());
        Assert.Single(diagnostics);
    }

    /// <summary>The <see cref="TextWriter"/> overload writes exactly the string overload's text, in every format.</summary>
    [Theory]
    [InlineData(TruthTableFormat.Markdown)]
    [InlineData(TruthTableFormat.Csv)]
    [InlineData(TruthTableFormat.Json)]
    [InlineData(TruthTableFormat.Html)]
    [InlineData(TruthTableFormat.Latex)]
    public void PrintTruthTable_WriterOverload_WritesTheSameTextAsTheStringOverload_Test(TruthTableFormat format)
    {
        CompiledRule<RuleTestContext> rule = Compile("a IMPLIES (b OR NOT c)");
        TruthTableOptions options = new() { Format = format };
        using StringWriter writer = new();

        rule.PrintTruthTable(writer, options);

        Assert.Equal(rule.PrintTruthTable(options), writer.ToString());
    }

    /// <summary>The same table prints identically on every call.</summary>
    [Fact]
    public void PrintTruthTable_CalledTwice_GivesTheSameText_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a XOR (b AND c)");

        Assert.Equal(rule.PrintTruthTable(), rule.PrintTruthTable());
        Assert.DoesNotContain('\r', rule.PrintTruthTable());
    }

    /// <summary>A null writer is an <see cref="ArgumentNullException"/>.</summary>
    [Fact]
    public void PrintTruthTable_NullWriter_ThrowsArgumentNullException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a");

        Assert.Throws<ArgumentNullException>(() => rule.PrintTruthTable(null!, null));
        Assert.Throws<ArgumentNullException>(() => rule.TryPrintTruthTable(null!, null, out _));
    }

    /// <summary>A format value that is not defined is an <see cref="ArgumentOutOfRangeException"/>, and nothing is written.</summary>
    [Fact]
    public void PrintTruthTable_UndefinedFormat_ThrowsArgumentOutOfRangeException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a");
        using StringWriter writer = new();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            rule.PrintTruthTable(writer, new TruthTableOptions { Format = (TruthTableFormat)99 })
        );
        Assert.Equal(string.Empty, writer.ToString());
    }

    /// <summary>The Markdown table escapes the characters that Markdown reads as markup.</summary>
    [Fact]
    public void PrintTruthTable_MarkdownTermWithMarkup_EscapesTheHeader_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"a|b_c\")");

        string header = rule.PrintTruthTable().Split('\n')[0];

        Assert.Equal("| hasCrust(crust: \"a\\|b\\_c\") | Result |", header);
    }

    /// <summary>The CSV table quotes a header with a comma or a quote, and uses a line feed between rows.</summary>
    [Fact]
    public void PrintTruthTable_Csv_QuotesFieldsAndUsesLineFeeds_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"x\") AND a");

        string[] lines = rule.PrintTruthTable(new TruthTableOptions { Format = TruthTableFormat.Csv }).Split('\n');

        Assert.Equal("\"hasCrust(crust: \"\"x\"\")\",a,Result", lines[0]);
        Assert.Equal("F,F,F", lines[1]);
        Assert.Equal(10, lines.Length);
    }

    /// <summary>The JSON table has the logic, the terms with their full call and the rows.</summary>
    [Fact]
    public void PrintTruthTable_Json_HasLogicTermsAndRows_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        string json = rule.PrintTruthTable(new TruthTableOptions { Format = TruthTableFormat.Json, Classical = true });

        Assert.Equal(
            """
            {
              "logic": "classical",
              "terms": [
                { "name": "a", "term": "a" },
                { "name": "b", "term": "b" }
              ],
              "rows": [
                { "values": ["F", "F"], "result": "F" },
                { "values": ["F", "T"], "result": "F" },
                { "values": ["T", "F"], "result": "F" },
                { "values": ["T", "T"], "result": "T" }
              ]
            }
            """,
            json
        );
        using JsonDocument parsed = JsonDocument.Parse(json);
        Assert.Equal(4, parsed.RootElement.GetProperty("rows").GetArrayLength());
    }

    /// <summary>A JSON table of a rule with no term is valid JSON with an empty term list.</summary>
    [Fact]
    public void PrintTruthTable_JsonRuleWithoutTerms_HasEmptyTermsAndOneRow_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("Unknown");

        using JsonDocument parsed = JsonDocument.Parse(
            rule.PrintTruthTable(new TruthTableOptions { Format = TruthTableFormat.Json })
        );

        Assert.Equal(0, parsed.RootElement.GetProperty("terms").GetArrayLength());
        JsonElement row = Assert.Single(Items(parsed.RootElement.GetProperty("rows")));
        Assert.Equal("U", row.GetProperty("result").GetString());
    }

    /// <summary>The HTML table has a state class on each cell, escapes the header, and puts the full call of a lettered term in the title.</summary>
    [Fact]
    public void PrintTruthTable_Html_WritesClassesAndEscapedHeaders_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"<b>\")");

        string plain = rule.PrintTruthTable(new TruthTableOptions { Format = TruthTableFormat.Html, Classical = true });
        string lettered = rule.PrintTruthTable(
            new TruthTableOptions
            {
                Format = TruthTableFormat.Html,
                Classical = true,
                SimpleVariables = true,
            }
        );

        Assert.Equal(
            """
            <table class="tw-truth-table">
            <thead>
            <tr><th>hasCrust(crust: &quot;&lt;b&gt;&quot;)</th><th>Result</th></tr>
            </thead>
            <tbody>
            <tr><td class="tw-false">F</td><td class="tw-false tw-result">F</td></tr>
            <tr><td class="tw-true">T</td><td class="tw-true tw-result">T</td></tr>
            </tbody>
            </table>
            """,
            plain
        );
        Assert.Contains("<th title=\"hasCrust(crust: &quot;&lt;b&gt;&quot;)\">p</th>", lettered, StringComparison.Ordinal);
    }

    /// <summary>The LaTeX table is a <c>tabular</c> with math-mode cells, a rule before the result column and one row per line.</summary>
    [Fact]
    public void PrintTruthTable_Latex_WritesATabularEnvironment_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a OR b");

        string latex = rule.PrintTruthTable(new TruthTableOptions { Format = TruthTableFormat.Latex, Classical = true });

        Assert.Equal(
            """
            \begin{tabular}{cc|c}
            $\text{a}$ & $\text{b}$ & Result \\
            \hline
            $\text{F}$ & $\text{F}$ & $\text{F}$ \\
            $\text{F}$ & $\text{T}$ & $\text{T}$ \\
            $\text{T}$ & $\text{F}$ & $\text{T}$ \\
            $\text{T}$ & $\text{T}$ & $\text{T}$ \\
            \end{tabular}
            """,
            latex
        );
    }

    /// <summary>The LaTeX table writes the logic symbols as commands.</summary>
    [Fact]
    public void PrintTruthTable_LatexLogicSymbols_WritesTopAndBottomCommands_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a");

        string latex = rule.PrintTruthTable(
            new TruthTableOptions
            {
                Format = TruthTableFormat.Latex,
                Classical = true,
                Constants = new ConstantFormat { Style = ConstantStyle.LogicSymbols },
            }
        );

        Assert.Contains("$\\bot$ & $\\bot$ \\\\", latex, StringComparison.Ordinal);
        Assert.Contains("$\\top$ & $\\top$ \\\\", latex, StringComparison.Ordinal);
    }

    private static List<JsonElement> Items(JsonElement array)
    {
        List<JsonElement> items = [];
        for (int i = 0; i < array.GetArrayLength(); i++)
        {
            items.Add(array[i]);
        }

        return items;
    }

    private static CompiledRule<RuleTestContext> CompileTerms(int count)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        for (int i = 0; i < count; i++)
        {
            builder = builder.AddConstant($"term{i}", true);
        }

        RuleCompiler<RuleTestContext> compiler = new(builder.Build());
        return compiler.Compile(string.Join(" AND ", Enumerable.Range(0, count).Select(i => $"term{i}"))).CompiledRule!;
    }

    private static CompiledRule<RuleTestContext> Compile(string dsl)
    {
        PredicateRegistry<RuleTestContext> registry = PredicateRegistry<RuleTestContext>
            .CreateBuilder()
            .AddConstant("a", true)
            .AddConstant("b", true)
            .AddConstant("c", true)
            .AddStringArgPredicate("hasCrust", "crust", "thin")
            .Build();
        RuleCompiler<RuleTestContext> compiler = new(registry);
        return compiler.Compile(dsl).CompiledRule!;
    }
}
