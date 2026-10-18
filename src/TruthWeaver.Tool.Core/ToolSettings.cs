namespace TruthWeaver.Tool.Core;

using TruthWeaver.Compilation;
using TruthWeaver.Printing;

/// <summary>
/// The resolved tool settings: every value has a source. <see cref="Resolve"/> applies the precedence: the command line
/// over the configuration file over the defaults.
/// </summary>
public sealed record ToolSettings
{
    /// <summary>Gets the defaults: the library's own defaults for each value, and no schema file.</summary>
    public static ToolSettings Default { get; } = new();

    /// <summary>Gets the operator spelling for printed output. The default is <see cref="Printing.OperatorStyle.Word"/>.</summary>
    public OperatorStyle OperatorStyle { get; init; } = OperatorStyle.Word;

    /// <summary>
    /// Gets how printed output writes the constants, or <see langword="null"/> (the default) for the default of each
    /// printer: <see cref="Printing.ConstantStyle.Letters"/> for equations and truth tables, and
    /// <see cref="Printing.ConstantStyle.Word"/> for rule text and trees.
    /// </summary>
    public ConstantStyle? ConstantStyle { get; init; }

    /// <summary>Gets the notation of printed equations. The default is <see cref="EquationDialect.Unicode"/>.</summary>
    public EquationDialect Dialect { get; init; } = EquationDialect.Unicode;

    /// <summary>Gets the color set of the terminal workbench. The default is <see cref="ToolTheme.Auto"/>.</summary>
    public ToolTheme Theme { get; init; } = ToolTheme.Auto;

    /// <summary>Gets the maximum tree depth. The default is the compiler default, 32.</summary>
    public int MaxDepth { get; init; } = CompilerOptions.Default.MaxDepth;

    /// <summary>Gets the maximum node count. The default is the compiler default, 512.</summary>
    public int MaxNodeCount { get; init; } = CompilerOptions.Default.MaxNodeCount;

    /// <summary>Gets the maximum number of distinct terms that the analysis considers. The default is the compiler default, 20.</summary>
    public int MaxAnalysisTerms { get; init; } = CompilerOptions.Default.MaxAnalysisTerms;

    /// <summary>Gets the maximum node count of a rewrite result. The default is the compiler default, 100,000.</summary>
    public int MaxRewriteNodeCount { get; init; } = CompilerOptions.Default.MaxRewriteNodeCount;

    /// <summary>Gets the path of the predicate schema file, or <see langword="null"/> (the default) for none.</summary>
    public string? SchemaFile { get; init; }

    /// <summary>
    /// Resolves the settings from two layers. For each value, the command line wins, then the configuration file, then
    /// <see cref="Default"/>.
    /// </summary>
    /// <param name="configurationFile">The layer from <c>truthweaver.json</c>, or <see langword="null"/> when there is no file.</param>
    /// <param name="commandLine">The layer from the command line, or <see langword="null"/> when it states nothing.</param>
    /// <returns>The resolved settings.</returns>
    public static ToolSettings Resolve(ToolConfiguration? configurationFile, ToolConfiguration? commandLine)
    {
        ToolConfiguration file = configurationFile ?? ToolConfiguration.Empty;
        ToolConfiguration command = commandLine ?? ToolConfiguration.Empty;
        return new ToolSettings
        {
            OperatorStyle = command.OperatorStyle ?? file.OperatorStyle ?? Default.OperatorStyle,
            ConstantStyle = command.ConstantStyle ?? file.ConstantStyle ?? Default.ConstantStyle,
            Dialect = command.Dialect ?? file.Dialect ?? Default.Dialect,
            Theme = command.Theme ?? file.Theme ?? Default.Theme,
            MaxDepth = command.MaxDepth ?? file.MaxDepth ?? Default.MaxDepth,
            MaxNodeCount = command.MaxNodeCount ?? file.MaxNodeCount ?? Default.MaxNodeCount,
            MaxAnalysisTerms = command.MaxAnalysisTerms ?? file.MaxAnalysisTerms ?? Default.MaxAnalysisTerms,
            MaxRewriteNodeCount = command.MaxRewriteNodeCount ?? file.MaxRewriteNodeCount ?? Default.MaxRewriteNodeCount,
            SchemaFile = command.SchemaFile ?? file.SchemaFile ?? Default.SchemaFile,
        };
    }

    /// <summary>
    /// Creates the compiler options for these settings: the limits, and <see cref="CompilationMode.Lenient"/>, because the
    /// tool has no host registry and accepts every predicate name (see <see cref="RuleConverter.CreateCompiler"/>).
    /// </summary>
    /// <returns>The compiler options.</returns>
    public CompilerOptions ToCompilerOptions()
    {
        return new CompilerOptions(
            MaxDepth: this.MaxDepth,
            MaxNodeCount: this.MaxNodeCount,
            MaxAnalysisTerms: this.MaxAnalysisTerms,
            Mode: CompilationMode.Lenient,
            MaxRewriteNodeCount: this.MaxRewriteNodeCount
        );
    }
}
