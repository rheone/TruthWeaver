namespace TruthWeaver.Tool.Core.Output;

using TruthWeaver.Compilation;
using TruthWeaver.Registry;
using TruthWeaver.Tool.Core.Assignment;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using TruthWeaver.Yaml;

/// <summary>
/// Compiles and converts loaded rules. The tool has no host registry, so it compiles permissively: every predicate name
/// is accepted and every argument is kept by value, or reported with ArgumentTypeMismatch when it has no exact
/// kind (<see cref="CompilationMode.Lenient"/> over an empty registry), and a
/// <c>from("source", "query")</c> reference needs no declared source. Syntax errors, operator errors and limits are still
/// diagnostics.
/// </summary>
/// <remarks>
/// A conversion is lossless: the result compiles to a tree equal to the source's, with the same notes. It does not keep the
/// layout of the source, its spelling (operator case, aliases) or YAML <c>#</c> comments.
/// </remarks>
public static class RuleConverter
{
    /// <summary>Creates the permissive compiler that the tool uses, with the limits of <paramref name="settings"/>.</summary>
    /// <param name="settings">The settings, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>A compiler over an empty registry in <see cref="CompilationMode.Lenient"/>.</returns>
    public static RuleCompiler<StubAssignment> CreateCompiler(ToolSettings? settings = null)
    {
        PredicateRegistry<StubAssignment> registry = PredicateRegistry<StubAssignment>.CreateBuilder().Build();
        return new RuleCompiler<StubAssignment>(registry, (settings ?? ToolSettings.Default).ToCompilerOptions());
    }

    /// <summary>Compiles a loaded rule in its own format.</summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="settings">The settings, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <returns>The compilation result. Diagnostic spans point into <see cref="LoadedRule.Text"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static CompilationResult<StubAssignment> Compile(LoadedRule rule, ToolSettings? settings = null)
    {
        ArgumentNullException.ThrowIfNull(rule);

        return CreateCompiler(settings).CompileAs(rule.Text, rule.Format);
    }

    /// <summary>Converts a loaded rule to another format, or tidies it in its own format.</summary>
    /// <param name="rule">The loaded rule.</param>
    /// <param name="to">The format to convert to.</param>
    /// <param name="settings">The settings, or <see langword="null"/> for <see cref="ToolSettings.Default"/>.</param>
    /// <param name="includeNotes"><see langword="true"/> (the default) to keep the notes of the rule.</param>
    /// <returns>
    /// The converted text as the library printer writes it (rule text in the word style, as <c>PrintRuleText()</c> prints
    /// it), or no text and the compile diagnostics. Write it with <see cref="OutputWriter"/> for line feeds and a final newline.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="to"/> is not a defined <see cref="RuleFormat"/>.</exception>
    public static ToolResult<string> Convert(
        LoadedRule rule,
        RuleFormat to,
        ToolSettings? settings = null,
        bool includeNotes = true
    )
    {
        ArgumentNullException.ThrowIfNull(rule);

        RuleConversionResult converted = CreateCompiler(settings).Convert(rule.Text, rule.Format, to, includeNotes);
        return new ToolResult<string>(converted.Text, converted.Diagnostics);
    }
}
