namespace TruthWeaver.Tool.Core;

using TruthWeaver.Printing;

/// <summary>
/// One layer of tool settings: the values that one source states. A <c>truthweaver.json</c> file is one layer
/// (<see cref="ToolConfigurationFile.Load"/>) and the command line is another. A <see langword="null"/> member means "this
/// layer does not say"; <see cref="ToolSettings.Resolve"/> fills it from a lower layer or the default.
/// </summary>
public sealed record ToolConfiguration
{
    /// <summary>Gets an empty layer, which states nothing.</summary>
    public static ToolConfiguration Empty { get; } = new();

    /// <summary>Gets the operator spelling for printed output (<c>operatorStyle</c>).</summary>
    public OperatorStyle? OperatorStyle { get; init; }

    /// <summary>Gets how printed output writes the constants (<c>constantStyle</c>).</summary>
    public ConstantStyle? ConstantStyle { get; init; }

    /// <summary>Gets the notation of printed equations (<c>dialect</c>).</summary>
    public EquationDialect? Dialect { get; init; }

    /// <summary>Gets the color set of the terminal workbench (<c>theme</c>).</summary>
    public ToolTheme? Theme { get; init; }

    /// <summary>Gets the maximum tree depth (<c>limits.maxDepth</c>).</summary>
    public int? MaxDepth { get; init; }

    /// <summary>Gets the maximum node count (<c>limits.maxNodeCount</c>).</summary>
    public int? MaxNodeCount { get; init; }

    /// <summary>Gets the maximum number of distinct terms that the analysis considers (<c>limits.maxAnalysisTerms</c>).</summary>
    public int? MaxAnalysisTerms { get; init; }

    /// <summary>Gets the maximum node count of a rewrite result (<c>limits.maxRewriteNodeCount</c>).</summary>
    public int? MaxRewriteNodeCount { get; init; }

    /// <summary>
    /// Gets the path of the optional predicate schema file (<c>schemaFile</c>). <see cref="ToolConfigurationFile.Load"/>
    /// makes a relative path absolute from the directory of the configuration file.
    /// </summary>
    public string? SchemaFile { get; init; }
}
