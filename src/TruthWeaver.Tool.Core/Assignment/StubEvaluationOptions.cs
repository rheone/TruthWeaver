namespace TruthWeaver.Tool.Core.Assignment;

using TruthWeaver.Evaluation;
using TruthWeaver.Tool.Core.Configuration;
using TruthWeaver.Tool.Core.Loading;
using DataSourceSet = TruthWeaver.Abstractions.DataSources;

/// <summary>The options of <see cref="StubEvaluator"/>.</summary>
public sealed record StubEvaluationOptions
{
    /// <summary>Gets the default options: no stub values (every term is <c>Unknown</c>), no data files and the default settings.</summary>
    public static StubEvaluationOptions Default { get; } = new();

    /// <summary>Gets the stub values. The default is <see cref="StubAssignment.Empty"/>, so every term is <c>Unknown</c>.</summary>
    public StubAssignment Stubs { get; init; } = StubAssignment.Empty;

    /// <summary>
    /// Gets the data sources for the <c>from("source", "query")</c> references of the rule, such as the result of
    /// <see cref="DataSourceFiles.LoadAll"/>, or <see langword="null"/> for none. A reference to a source that is not here
    /// is a fault, and its term is <c>Unknown</c>.
    /// </summary>
    public DataSourceSet? DataSources { get; init; }

    /// <summary>Gets the evaluation options: the mode, the fault budget, the timeout and the resolved values in the trace.</summary>
    public EvaluationOptions Evaluation { get; init; } = EvaluationOptions.Default;

    /// <summary>Gets the settings: the compile limits and the schema file. The default is <see cref="ToolSettings.Default"/>.</summary>
    public ToolSettings Settings { get; init; } = ToolSettings.Default;

    /// <summary>
    /// Gets a predicate schema file that is already read, or <see langword="null"/> to read the file of
    /// <see cref="ToolSettings.SchemaFile"/> when one is set. The file gives the kind of an argument that the rule does not
    /// show, such as the argument of a <c>from(...)</c> reference.
    /// </summary>
    public PredicateSchemaFile? PredicateSchema { get; init; }
}
