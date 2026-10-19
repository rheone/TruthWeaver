namespace TruthWeaver.Tool.Tui.Tests.Controllers;

using TruthWeaver.Tool.Tui.Controllers;

/// <summary>How <see cref="TermCalls"/> names a predicate call and compares two calls.</summary>
public sealed class TermCallsTests
{
    /// <summary>The name of a call is the run of predicate marks at the start of the text, so it ends before the opening parenthesis.</summary>
    [Fact]
    public void NameOf_Call_ReturnsTheNameBeforeTheParenthesis_Test()
    {
        Assert.Equal("hasRole", TermCalls.NameOf("hasRole(role: \"admin\")"));
    }

    /// <summary>A bare name is its own name.</summary>
    [Fact]
    public void NameOf_BareName_ReturnsTheName_Test()
    {
        Assert.Equal("isSet", TermCalls.NameOf("isSet"));
    }

    /// <summary>White space between the name and the parenthesis is not part of the name.</summary>
    [Fact]
    public void NameOf_CallWithSpaces_ReturnsTheNameWithoutThem_Test()
    {
        Assert.Equal("hasRole", TermCalls.NameOf("hasRole  (  role : \"admin\" )"));
    }

    /// <summary>Calls that differ only in white space are the same call.</summary>
    [Fact]
    public void SameCall_CallsDifferingInSpacing_AreTheSame_Test()
    {
        Assert.True(TermCalls.SameCall("hasRole(role: \"admin\")", "hasRole( role:\"admin\" )"));
    }

    /// <summary>Calls with different arguments are not the same call, even when the spacing is equal.</summary>
    [Fact]
    public void SameCall_CallsDifferingInArgument_AreNotTheSame_Test()
    {
        Assert.False(TermCalls.SameCall("hasRole(role: \"admin\")", "hasRole(role: \"editor\")"));
    }
}
