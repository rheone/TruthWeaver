namespace TruthWeaver.Tool.Core.Tests.Diagnostics;

using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Diagnostics;

/// <summary>An error fails a command, a warning fails it only in strict mode, and a tool error makes the tool unusable.</summary>
public sealed class SeverityPolicyTests
{
    /// <summary>The exit code follows the most severe diagnostic and the strict mode.</summary>
    /// <param name="severity">The severity of the one diagnostic.</param>
    /// <param name="strict">Whether strict mode is on.</param>
    /// <param name="expected">The exit code.</param>
    [Theory]
    [InlineData(DiagnosticSeverity.Info, false, ToolExitCode.Success)]
    [InlineData(DiagnosticSeverity.Info, true, ToolExitCode.Success)]
    [InlineData(DiagnosticSeverity.Warning, false, ToolExitCode.Success)]
    [InlineData(DiagnosticSeverity.Warning, true, ToolExitCode.Failed)]
    [InlineData(DiagnosticSeverity.Error, false, ToolExitCode.Failed)]
    [InlineData(DiagnosticSeverity.Error, true, ToolExitCode.Failed)]
    public void ExitCodeFor_RuleDiagnostic_FollowsSeverityAndStrictMode_Test(
        DiagnosticSeverity severity,
        bool strict,
        ToolExitCode expected
    )
    {
        Diagnostic diagnostic = new(DiagnosticCodes.SyntaxError, severity, "message", SourceSpan.None);

        ToolExitCode code = SeverityPolicy.ExitCodeFor([diagnostic], strict);

        Assert.Equal(expected, code);
    }

    /// <summary>No diagnostic is success.</summary>
    [Fact]
    public void ExitCodeFor_NoDiagnostics_IsSuccess_Test()
    {
        Assert.Equal(ToolExitCode.Success, SeverityPolicy.ExitCodeFor([], strict: true));
    }

    /// <summary>A tool error about the input wins over a rule error.</summary>
    [Fact]
    public void ExitCodeFor_InputErrorAndRuleError_IsUnusable_Test()
    {
        Diagnostic rule = Diagnostic.Error(DiagnosticCodes.SyntaxError, "rule", SourceSpan.None);
        Diagnostic input = Diagnostic.Error(ToolDiagnosticCodes.InputUnreadable, "input", SourceSpan.None);

        Assert.Equal(ToolExitCode.Unusable, SeverityPolicy.ExitCodeFor([rule, input], strict: false));
    }

    /// <summary>A tool finding about the rule itself, such as a tree schema violation, is a failure, not an unusable tool.</summary>
    [Fact]
    public void ExitCodeFor_TreeSchemaViolation_IsFailed_Test()
    {
        Diagnostic finding = Diagnostic.Error(ToolDiagnosticCodes.TreeSchemaViolation, "schema", SourceSpan.None);

        Assert.Equal(ToolExitCode.Failed, SeverityPolicy.ExitCodeFor([finding], strict: false));
    }

    /// <summary>A tool warning does not make the tool unusable; in strict mode it fails the command.</summary>
    [Fact]
    public void ExitCodeFor_ToolWarningInStrictMode_IsFailed_Test()
    {
        Diagnostic warning = Diagnostic.Warning(ToolDiagnosticCodes.UnknownConfigurationProperty, "w", SourceSpan.None);

        Assert.Equal(ToolExitCode.Failed, SeverityPolicy.ExitCodeFor([warning], strict: true));
    }
}
