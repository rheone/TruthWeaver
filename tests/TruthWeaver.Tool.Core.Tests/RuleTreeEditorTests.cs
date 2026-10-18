namespace TruthWeaver.Tool.Core.Tests;

using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Yaml;

/// <summary>The structural edits of a rule tree: each edit gives the expected tree, compiled, or is refused with a reason.</summary>
public sealed class RuleTreeEditorTests
{
    /// <summary>Replacing the operator of an AND with OR keeps the operands.</summary>
    [Fact]
    public void ReplaceOperator_AndWithOr_GivesOrOfTheSameOperands_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").ReplaceOperator("$", "or");

        AssertTree("a OR b", edit);
    }

    /// <summary>A threshold operator that needs a count gets the count 1 when the node had none.</summary>
    [Fact]
    public void ReplaceOperator_AndWithAtLeast_AddsTheCountOne_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").ReplaceOperator("$", "atLeast");

        AssertTree(
            """{"op": "atLeast", "k": 1, "operands": [{"predicate": "a"}, {"predicate": "b"}]}""",
            edit,
            RuleFormat.Json
        );
    }

    /// <summary>An operator that does not fit the operands (NOT of two operands) is refused with the compiler diagnostics.</summary>
    [Fact]
    public void ReplaceOperator_NotOfTwoOperands_IsRefusedWithDiagnostics_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").ReplaceOperator("$", "not");

        Assert.False(edit.Succeeded);
        Assert.StartsWith("Not applied: the edit would make an invalid rule.", edit.Message, StringComparison.Ordinal);
        Assert.NotEmpty(edit.Diagnostics);
    }

    /// <summary>A term has no operator to replace, so the edit is refused and says why.</summary>
    [Fact]
    public void ReplaceOperator_Term_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").ReplaceOperator("$.operands[0]", "or");

        Assert.False(edit.Succeeded);
        Assert.Contains("not an operator", edit.Message, StringComparison.Ordinal);
    }

    /// <summary>Wrapping an operand in NOT puts a NOT above it and selects the NOT.</summary>
    [Fact]
    public void WrapInNot_Operand_GivesTheNegatedOperand_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").WrapInNot("$.operands[1]");

        AssertTree("a AND NOT b", edit);
        Assert.Equal("$.operands[1]", edit.Path);
    }

    /// <summary>Wrapping the root in NOT negates the whole rule.</summary>
    [Fact]
    public void WrapInNot_Root_NegatesTheWholeRule_Test()
    {
        RuleTreeEdit edit = Editor("a OR b").WrapInNot("$");

        AssertTree("NOT (a OR b)", edit);
    }

    /// <summary>The note of a wrapped node stays on that node, one level down.</summary>
    [Fact]
    public void WrapInNot_NotedNode_KeepsTheNoteOnTheNode_Test()
    {
        CompiledRule<StubAssignment> rule = Compile(
            """{"op": "and", "operands": [{"predicate": "a", "note": "first"}, {"predicate": "b"}]}""",
            RuleFormat.Json
        );

        RuleTreeEdit edit = new RuleTreeEditor(rule).WrapInNot("$.operands[0]");

        Assert.True(edit.Succeeded);
        Assert.Equal("first", edit.Rule.Notes["$.operands[0].operands[0]"]);
    }

    /// <summary>Negating a NOT removes it.</summary>
    [Fact]
    public void Negate_Not_RemovesTheNot_Test()
    {
        RuleTreeEdit edit = Editor("a AND NOT b").Negate("$.operands[1]");

        AssertTree("a AND b", edit);
    }

    /// <summary>Negating an AND gives NAND, its exact negation under Strong K3.</summary>
    [Fact]
    public void Negate_And_GivesNand_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").Negate("$");

        AssertTree("""{"op": "nand", "operands": [{"predicate": "a"}, {"predicate": "b"}]}""", edit, RuleFormat.Json);
    }

    /// <summary>Negating the constant True gives False.</summary>
    [Fact]
    public void Negate_True_GivesFalse_Test()
    {
        RuleTreeEdit edit = Editor("a OR True").Negate("$.operands[1]");

        AssertTree("a OR False", edit);
    }

    /// <summary>Negating a term wraps it in NOT.</summary>
    [Fact]
    public void Negate_Term_WrapsItInNot_Test()
    {
        RuleTreeEdit edit = Editor("a OR b").Negate("$.operands[0]");

        AssertTree("NOT a OR b", edit);
    }

    /// <summary>Unknown is its own negation, so negating it is refused.</summary>
    [Fact]
    public void Negate_Unknown_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor("a OR Unknown").Negate("$.operands[1]");

        Assert.False(edit.Succeeded);
        Assert.Contains("NOT Unknown is Unknown", edit.Message, StringComparison.Ordinal);
    }

    /// <summary>Inserting an operand adds it after the last operand and selects it.</summary>
    [Fact]
    public void InsertOperand_Expression_AddsItAfterTheLastOperand_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").InsertOperand("$", "c");

        AssertTree("a AND b AND c", edit);
        Assert.Equal("$.operands[2]", edit.Path);
    }

    /// <summary>Inserting at an index puts the operand there.</summary>
    [Fact]
    public void InsertOperand_AtIndex_PutsTheOperandThere_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").InsertOperand("$", "c OR d", 0);

        AssertTree("(c OR d) AND a AND b", edit);
        Assert.Equal("$.operands[0]", edit.Path);
    }

    /// <summary>An operand that is not a valid expression is refused with its diagnostics.</summary>
    [Fact]
    public void InsertOperand_InvalidExpression_IsRefusedWithDiagnostics_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").InsertOperand("$", "c AND (");

        Assert.False(edit.Succeeded);
        Assert.StartsWith("Not a valid expression:", edit.Message, StringComparison.Ordinal);
        Assert.NotEmpty(edit.Diagnostics);
    }

    /// <summary>Deleting one of three operands keeps the operator with the other two.</summary>
    [Fact]
    public void Delete_OneOfThreeOperands_KeepsTheOtherTwo_Test()
    {
        RuleTreeEdit edit = Editor("a AND b AND c").Delete("$.operands[1]");

        AssertTree("a AND c", edit);
        Assert.Equal("$", edit.Path);
    }

    /// <summary>Deleting one side of a two-operand AND leaves the other side in its place.</summary>
    [Fact]
    public void Delete_OneOfTwoOperands_LeavesTheOtherOperand_Test()
    {
        RuleTreeEdit edit = Editor("x OR (a AND b)").Delete("$.operands[1].operands[0]");

        AssertTree("x OR b", edit);
    }

    /// <summary>The root cannot be deleted.</summary>
    [Fact]
    public void Delete_Root_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").Delete("$");

        Assert.False(edit.Succeeded);
        Assert.Contains("root cannot be deleted", edit.Message, StringComparison.Ordinal);
    }

    /// <summary>Swapping an operand with the next one exchanges them and follows the node.</summary>
    [Fact]
    public void Swap_WithNextOperand_ExchangesTheOperands_Test()
    {
        RuleTreeEdit edit = Editor("a IMPLIES b").Swap("$.operands[0]", 1);

        AssertTree("b IMPLIES a", edit);
        Assert.Equal("$.operands[1]", edit.Path);
    }

    /// <summary>A swap past the last operand is refused.</summary>
    [Fact]
    public void Swap_PastTheLastOperand_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").Swap("$.operands[1]", 1);

        Assert.False(edit.Succeeded);
    }

    /// <summary>Replacing a node with an expression puts the compiled expression in its place.</summary>
    [Fact]
    public void Replace_TermWithCall_PutsTheCallInItsPlace_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").Replace("$.operands[1]", "hasRole(role: \"admin\")");

        AssertTree("a AND hasRole(role: \"admin\")", edit);
    }

    /// <summary>Pasted text that does not compile is refused with its diagnostics and changes nothing.</summary>
    [Fact]
    public void Replace_InvalidText_IsRefusedWithDiagnostics_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").Replace("$.operands[1]", "c AND AND d");

        Assert.False(edit.Succeeded);
        Assert.Null(edit.Rule);
        Assert.NotEmpty(edit.Diagnostics);
    }

    /// <summary>Changing the literal of a constant gives the new constant.</summary>
    [Fact]
    public void ChangeLiteral_Constant_GivesTheNewConstant_Test()
    {
        RuleTreeEdit edit = Editor("a AND True").ChangeLiteral("$.operands[1]", "const", "unknown");

        AssertTree("a AND Unknown", edit);
    }

    /// <summary>Changing an argument of a term gives the term with the new value.</summary>
    [Fact]
    public void ChangeLiteral_TermArgument_GivesTheNewValue_Test()
    {
        RuleTreeEdit edit = Editor("hasRole(role: \"viewer\")").ChangeLiteral("$", "role", "admin");

        AssertTree("hasRole(role: \"admin\")", edit);
    }

    /// <summary>Changing the count of a threshold gives the new count.</summary>
    [Fact]
    public void ChangeLiteral_ThresholdCount_GivesTheNewCount_Test()
    {
        RuleTreeEdit edit = Editor(
                """{"op": "atLeast", "k": 1, "operands": [{"predicate": "a"}, {"predicate": "b"}]}""",
                RuleFormat.Json
            )
            .ChangeLiteral("$", "k", "2");

        AssertTree(
            """{"op": "atLeast", "k": 2, "operands": [{"predicate": "a"}, {"predicate": "b"}]}""",
            edit,
            RuleFormat.Json
        );
    }

    /// <summary>A count that is not a number makes an invalid rule, so the edit is refused.</summary>
    [Fact]
    public void ChangeLiteral_CountThatIsNotANumber_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor(
                """{"op": "atLeast", "k": 1, "operands": [{"predicate": "a"}, {"predicate": "b"}]}""",
                RuleFormat.Json
            )
            .ChangeLiteral("$", "k", "many");

        Assert.False(edit.Succeeded);
        Assert.NotEmpty(edit.Diagnostics);
    }

    /// <summary>A path that names no node is refused.</summary>
    [Fact]
    public void WrapInNot_PathThatNamesNoNode_IsRefused_Test()
    {
        RuleTreeEdit edit = Editor("a AND b").WrapInNot("$.operands[5]");

        Assert.False(edit.Succeeded);
        Assert.Contains("names no node", edit.Message, StringComparison.Ordinal);
    }

    private static RuleTreeEditor Editor(string text, RuleFormat format = RuleFormat.Text)
    {
        return new RuleTreeEditor(Compile(text, format));
    }

    private static CompiledRule<StubAssignment> Compile(string text, RuleFormat format = RuleFormat.Text)
    {
        CompilationResult<StubAssignment> result = RuleConverter.CreateCompiler().CompileAs(text, format);
        Assert.NotNull(result.Rule);
        return result.Rule;
    }

    // The edit compiled, and its tree equals the tree of the expected rule.
    private static void AssertTree(string expected, RuleTreeEdit edit, RuleFormat format = RuleFormat.Text)
    {
        Assert.True(edit.Succeeded, edit.Message);
        Assert.Equal(Compile(expected, format).PrintJson(includeNotes: false), edit.Rule.PrintJson(includeNotes: false));
    }
}
