namespace TruthWeaver.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// <see cref="CompiledRule{TContext}.PrintHtml(HtmlOptions?)"/>: an HTML fragment of a rule with classed spans, entities
/// for the logic symbols, escaped text and no script or inline style.
/// </summary>
public sealed class HtmlPrinterTests
{
    /// <summary>Each operator, term and constant sits in a span with its own class, and a nested connective is grouped.</summary>
    [Fact]
    public void PrintHtml_DefaultOptions_WrapsEachNodeRoleInAClassedSpan_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND (b OR True)");

        string html = rule.PrintHtml();

        Assert.Equal(
            "<span class=\"tw-rule\"><span class=\"tw-term\">a</span> <span class=\"tw-operator\">AND</span> "
                + "(<span class=\"tw-term\">b</span> <span class=\"tw-operator\">OR</span> <span class=\"tw-constant\">True</span>)</span>",
            html
        );
    }

    /// <summary><see cref="HtmlOptions.Default"/> equals a new instance, so it gives the output of the option-less call.</summary>
    [Fact]
    public void PrintHtml_DefaultOptionsInstance_GivesTheOptionlessOutput_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND b");

        Assert.Equal(new HtmlOptions(), HtmlOptions.Default);
        Assert.Equal(rule.PrintHtml(), rule.PrintHtml(HtmlOptions.Default));
    }

    /// <summary>The symbolic style writes the operators as named HTML entities, not as raw glyphs.</summary>
    [Fact]
    public void PrintHtml_SymbolicStyle_WritesOperatorsAsNamedEntities_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a AND (b XOR c)");

        string html = rule.PrintHtml(new HtmlOptions { OperatorStyle = OperatorStyle.Symbolic });

        Assert.Equal(
            "<span class=\"tw-rule\"><span class=\"tw-operator\">&not;</span><span class=\"tw-term\">a</span> "
                + "<span class=\"tw-operator\">&and;</span> (<span class=\"tw-term\">b</span> "
                + "<span class=\"tw-operator\">&oplus;</span> <span class=\"tw-term\">c</span>)</span>",
            html
        );
    }

    /// <summary>The word style puts a space between NOT and its operand.</summary>
    [Fact]
    public void PrintHtml_WordNot_SeparatesTheOperandWithASpace_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("NOT a");

        string html = rule.PrintHtml();

        Assert.Equal(
            "<span class=\"tw-rule\"><span class=\"tw-operator\">NOT</span> <span class=\"tw-term\">a</span></span>",
            html
        );
    }

    /// <summary>An operator without an infix symbol prints in function-call form, with its parameters first.</summary>
    [Fact]
    public void PrintHtml_ThresholdOperator_PrintsAsAFunctionCall_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("AtLeast(2, a, b, c)");

        string html = rule.PrintHtml();

        Assert.Equal(
            "<span class=\"tw-rule\"><span class=\"tw-operator\">AtLeast(2, </span><span class=\"tw-term\">a</span>, "
                + "<span class=\"tw-term\">b</span>, <span class=\"tw-term\">c</span>)</span>",
            html
        );
    }

    /// <summary>Markup characters in a term label are escaped, so they cannot become elements.</summary>
    [Fact]
    public void Print_MarkupInTermLabel_IsEscaped_Test()
    {
        OutlineNode term = new("<b onclick=\"x()\">&'</b>", "description", []);

        string html = HtmlPrinter.Print(term, HtmlOptions.Default);

        Assert.Equal(
            "<span class=\"tw-rule\"><span class=\"tw-term\">&lt;b onclick=&quot;x()&quot;&gt;&amp;&#39;&lt;/b&gt;</span></span>",
            html
        );
    }

    /// <summary>Markup characters in an argument value are escaped, and the fragment has no script element and no style attribute.</summary>
    [Fact]
    public void PrintHtml_MarkupInArgumentValue_IsEscapedAndHasNoScriptOrStyle_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"<script>alert('x')</script> & \\\"y\\\"\") AND a");

        string html = rule.PrintHtml();

        Assert.Contains("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt; &amp; ", html);
        Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("style=", html, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The same escape applies to a constant override string.</summary>
    [Fact]
    public void PrintHtml_MarkupInConstantOverride_IsEscaped_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a AND True");

        string html = rule.PrintHtml(new HtmlOptions { Constants = new ConstantFormat { TrueText = "<i>yes</i>" } });

        Assert.Contains("<span class=\"tw-constant\">&lt;i&gt;yes&lt;/i&gt;</span>", html);
        Assert.DoesNotContain("<i>", html);
    }

    /// <summary>With argument values hidden, a term prints as its label only.</summary>
    [Fact]
    public void PrintHtml_ArgumentValuesHidden_PrintsOnlyTheLabel_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"thin\")");

        string html = rule.PrintHtml(new HtmlOptions { ShowArgumentValues = false });

        Assert.Equal("<span class=\"tw-rule\"><span class=\"tw-term\">hasCrust</span></span>", html);
    }

    /// <summary>The constant format changes the constant text and nothing else.</summary>
    [Fact]
    public void PrintHtml_LogicSymbolConstants_WritesNamedEntities_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("True OR False");

        string html = rule.PrintHtml(new HtmlOptions { Constants = new ConstantFormat { Style = ConstantStyle.LogicSymbols } });

        Assert.Equal(
            "<span class=\"tw-rule\"><span class=\"tw-constant\">&top;</span> <span class=\"tw-operator\">OR</span> "
                + "<span class=\"tw-constant\">&perp;</span></span>",
            html
        );
    }

    /// <summary>Simple variables print letters, share a letter between equal terms and keep the full term in the title.</summary>
    [Fact]
    public void PrintHtml_SimpleVariables_PrintsLettersWithTheTermInTheTitle_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("hasCrust(crust: \"a&b\") AND (b OR hasCrust(crust: \"a&b\"))");

        string html = rule.PrintHtml(new HtmlOptions { SimpleVariables = true });

        Assert.Equal(
            "<span class=\"tw-rule\"><span class=\"tw-term\" title=\"hasCrust (crust: &quot;a&amp;b&quot;)\">p</span> "
                + "<span class=\"tw-operator\">AND</span> (<span class=\"tw-term\" title=\"b\">q</span> "
                + "<span class=\"tw-operator\">OR</span> <span class=\"tw-term\" title=\"hasCrust (crust: &quot;a&amp;b&quot;)\">p</span>)</span>",
            html
        );
    }

    /// <summary>After the eleventh distinct term, the letters repeat with a subscript.</summary>
    [Fact]
    public void PrintHtml_SimpleVariablesPastTheLetters_UsesASubscript_Test()
    {
        string dsl = string.Join(" AND ", Enumerable.Range(0, 12).Select(i => $"hasCrust(crust: \"v{i}\")"));
        CompiledRule<RuleTestContext> rule = Compile(dsl);

        string html = rule.PrintHtml(new HtmlOptions { SimpleVariables = true });

        Assert.Contains(">z</span>", html);
        Assert.Contains(">p<sub>1</sub></span>", html);
    }

    /// <summary>A decision adds the state class of each node, and a short-circuited node is marked skipped.</summary>
    [Fact]
    public async Task PrintHtml_WithDecision_AddsTheStateClassOfEachNode_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("False AND a");
        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        string html = rule.PrintHtml(decision);

        Assert.Equal(
            "<span class=\"tw-rule\"><span class=\"tw-constant tw-false\">False</span> <span class=\"tw-operator tw-false\">AND</span> "
                + "<span class=\"tw-term tw-skipped\">a</span></span>",
            html
        );
    }

    /// <summary>A decision with no trace tree is a caller error.</summary>
    [Fact]
    public void PrintHtml_DecisionWithoutTraceTree_ThrowsArgumentException_Test()
    {
        CompiledRule<RuleTestContext> rule = Compile("a");

        Assert.Throws<ArgumentException>(() => rule.PrintHtml(new Decision(TruthValue.True, [])));
    }

    /// <summary>The escape routine writes each logic glyph as its named entity and replaces control characters.</summary>
    [Theory]
    [InlineData("∧", "&and;")]
    [InlineData("∨", "&or;")]
    [InlineData("¬", "&not;")]
    [InlineData("⊕", "&oplus;")]
    [InlineData("↔", "&harr;")]
    [InlineData("→", "&rarr;")]
    [InlineData("↑", "&uarr;")]
    [InlineData("↓", "&darr;")]
    [InlineData("⊤", "&top;")]
    [InlineData("⊥", "&perp;")]
    [InlineData("<>&\"'", "&lt;&gt;&amp;&quot;&#39;")]
    [InlineData("a\nb\tc\rd", "a&#10;b&#9;c&#13;d")]
    [InlineData("\u0001\u007F", "��")]
    [InlineData("é 😀", "é 😀")]
    public void Escape_Text_WritesEntitiesAndReplacesControlCharacters_Test(string text, string expected)
    {
        Assert.Equal(expected, HtmlText.Escape(text));
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
