namespace TruthWeaver.Tests;

using System.Diagnostics;
using System.Reflection;
using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Diagnostics;
using TruthWeaver.Printing;

/// <summary>
/// The debugger display strings are plain text inside an attribute, so the compiler never checks them. These tests
/// evaluate each backing <c>DebuggerDisplay</c> member, so a broken or renamed member fails here and not in a debug session.
/// </summary>
public sealed class DebuggerDisplayTests
{
    /// <summary>A fault shows its term, the exception type and the message, not the exception's stack trace.</summary>
    [Fact]
    public void DebuggerDisplay_Fault_ShowsTermExceptionTypeAndMessageOnly_Test()
    {
        Fault fault = new(new TermIdentity("IsAdmin", []), ThrownException());

        string display = DisplayOf(fault);

        Assert.Contains("IsAdmin", display, StringComparison.Ordinal);
        Assert.EndsWith("InvalidOperationException: boom", display, StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", display, StringComparison.Ordinal);
    }

    /// <summary>A decision shows its result and the number of faults.</summary>
    [Fact]
    public void DebuggerDisplay_Decision_ShowsResultAndFaultCount_Test()
    {
        Decision decision = new(TruthValue.Unknown, [new Fault(new TermIdentity("IsAdmin", []), ThrownException())]);

        Assert.Equal("Unknown (1 faults)", DisplayOf(decision));
    }

    /// <summary>An evaluated trace node shows its text, its result and its child count.</summary>
    [Fact]
    public void DebuggerDisplay_EvaluatedTraceNode_ShowsTextResultAndChildCount_Test()
    {
        TraceNode leaf = new("IsAdmin()", TruthValue.True, false, []);
        TraceNode node = new("AND", TruthValue.False, false, [leaf, leaf]);

        Assert.Equal("AND => False (2 children)", DisplayOf(node));
    }

    /// <summary>A trace node skipped by short-circuiting says so instead of showing a result.</summary>
    [Fact]
    public void DebuggerDisplay_SkippedTraceNode_SaysNotEvaluated_Test()
    {
        TraceNode node = new("IsAdmin()", null, true, []);

        Assert.Equal("IsAdmin() => not evaluated (0 children)", DisplayOf(node));
    }

    /// <summary>A trace entry shows its text and its result.</summary>
    [Fact]
    public void DebuggerDisplay_TraceEntry_ShowsTextAndResult_Test()
    {
        Assert.Equal("IsAdmin() => False", DisplayOf(new TraceEntry("IsAdmin()", TruthValue.False, false)));
        Assert.Equal("IsAdmin() => not evaluated", DisplayOf(new TraceEntry("IsAdmin()", null, true)));
    }

    /// <summary>A diagnostic shows its code, severity and message.</summary>
    [Fact]
    public void DebuggerDisplay_Diagnostic_ShowsCodeSeverityAndMessage_Test()
    {
        Diagnostic diagnostic = Diagnostic.Error("TW9999", "Something is wrong.", new SourceSpan(0, 1), expected: "x");

        Assert.Equal("TW9999 Error: Something is wrong.", DisplayOf(diagnostic));
    }

    /// <summary>Every expression node, whatever its subtype, shows its canonical rule text.</summary>
    [Fact]
    public void DebuggerDisplay_Expression_ShowsCanonicalRuleText_Test()
    {
        Expression expression = new NotExpression(new ConstantExpression(TruthValue.True));

        Assert.Equal(CanonicalPrinter.Print(expression), DisplayOf(expression));
    }

    /// <summary>The display attribute reaches subtypes of <see cref="Expression"/> and points at the backing member.</summary>
    [Fact]
    public void DebuggerDisplayAttribute_ExpressionSubtype_PointsAtTheBackingMember_Test()
    {
        DebuggerDisplayAttribute? attribute = typeof(NotExpression).GetCustomAttribute<DebuggerDisplayAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("{DebuggerDisplay,nq}", attribute.Value);
    }

    // Reads the private DebuggerDisplay member the attribute names, searching the base types so a record subtype works.
    private static string DisplayOf(object value)
    {
        for (Type? type = value.GetType(); type is not null; type = type.BaseType)
        {
            PropertyInfo? property = type.GetProperty(
                "DebuggerDisplay",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
            );
            if (property is not null)
            {
                return (string)property.GetValue(value)!;
            }
        }

        throw new InvalidOperationException($"{value.GetType().Name} has no DebuggerDisplay member.");
    }

    // A thrown (not merely constructed) exception, so it carries a real stack trace the display must leave out.
    private static InvalidOperationException ThrownException()
    {
        try
        {
            throw new InvalidOperationException("boom");
        }
        catch (InvalidOperationException ex)
        {
            return ex;
        }
    }
}
