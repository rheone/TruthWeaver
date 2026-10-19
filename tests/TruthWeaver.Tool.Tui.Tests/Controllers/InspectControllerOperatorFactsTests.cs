namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Compilation;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Output;
using TruthWeaver.Tool.Tui.Controllers;
using TruthWeaver.Tool.Tui.Sessions;
using TruthWeaver.Yaml;

/// <summary>
/// The three hand-written operator-fact tables of <see cref="InspectController"/> (<see cref="InspectController.CardinalityOperators"/>,
/// <see cref="InspectController.ExternalOperators"/> and <see cref="InspectController.OperatorSpellings"/>), checked
/// against the library's own operator list instead of trusted on faith. The outline of a compiled rule carries no
/// "counts its operands" or "is external" fact, so these tables stay hand-written (ticket 12's decision); these tests
/// are the safety net that catches the tables drifting from the library when an operator is added, renamed or removed.
/// </summary>
public sealed class InspectControllerOperatorFactsTests
{
    /// <summary>The member data for <see cref="OperatorSpellings_EachSymbol_ParsesToTheOperatorItClaims_Test"/>: every entry of the map.</summary>
    public static TheoryData<string, string> Spellings()
    {
        TheoryData<string, string> data = [];
        foreach (KeyValuePair<string, string> spelling in InspectController.OperatorSpellings)
        {
            data.Add(spelling.Key, spelling.Value);
        }

        return data;
    }

    /// <summary>
    /// The Cardinality group of the Insert menu is read from <c>OperatorDefinitions</c> by hand when the menu is written, so
    /// it is the closest the library comes to a "this operator counts its operands" fact. The table must name exactly that
    /// group, so an operator added to or removed from the group is caught here instead of only showing a wrong inspect text.
    /// </summary>
    [Fact]
    public void CardinalityOperators_ComparedWithTheInsertMenu_MatchTheCardinalityGroup_Test()
    {
        HashSet<string> expected = InsertItems
            .Operators.Single(g => g.Title == "Cardinality")
            .Items.Select(i => i.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(
            expected.SetEquals(InspectController.CardinalityOperators),
            $"expected [{string.Join(", ", expected)}] got [{string.Join(", ", InspectController.CardinalityOperators)}]"
        );
    }

    /// <summary>
    /// ADR-0005 names the external operators as COALESCE and the four Is* operators. The Is* operators are exactly the
    /// Inspection group of the Insert menu; COALESCE sits in the Other group with If, which is not external. The test
    /// names COALESCE once, by the one exception the decision itself documents, and takes every other name from the
    /// library's own grouping, so an Is*-shaped operator added later is caught without touching this test.
    /// </summary>
    [Fact]
    public void ExternalOperators_ComparedWithTheInsertMenu_MatchTheInspectionGroupPlusCoalesce_Test()
    {
        HashSet<string> expected = InsertItems
            .Operators.Single(g => g.Title == "Inspection")
            .Items.Select(i => i.Name)
            .Append("COALESCE")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(
            expected.SetEquals(InspectController.ExternalOperators),
            $"expected [{string.Join(", ", expected)}] got [{string.Join(", ", InspectController.ExternalOperators)}]"
        );
    }

    /// <summary>
    /// Every symbol of <see cref="InspectController.OperatorSpellings"/> must parse to the operator it claims (ticket 12's
    /// decision: a test checks the symbol-to-name map against the parser). A symbol that drifted from the grammar, or a
    /// name that no longer matches the operator's own label, fails here instead of only giving a wrong inspect text.
    /// </summary>
    [Theory]
    [MemberData(nameof(Spellings))]
    public void OperatorSpellings_EachSymbol_ParsesToTheOperatorItClaims_Test(string symbol, string name)
    {
        string text = symbol is "!" or "¬" ? $"{symbol}a" : $"a {symbol} b";

        CompilationResult<StubAssignment> result = RuleConverter
            .CreateCompiler(ToolSettings.Default)
            .CompileAs(text, RuleFormat.Text);

        Assert.True(result.Rule is not null, $"'{text}': {string.Join("; ", result.Diagnostics.Select(d => d.Message))}");
        string label = result.Rule.Outline().Label;
        Assert.StartsWith(name, label, StringComparison.OrdinalIgnoreCase);
    }
}
