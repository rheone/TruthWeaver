namespace TruthWeaver.Tool.Core.Export;

/// <summary>The kind of output that an <see cref="ArtifactDefinition"/> is, so that a front end can list the artifacts under headings.</summary>
public enum ArtifactGroup
{
    /// <summary>The rule as rule text, JSON or YAML.</summary>
    Rule,

    /// <summary>The equation in a dialect, an envelope and a constant style.</summary>
    Equation,

    /// <summary>The text tree and the diagrams (Mermaid, DOT and HTML).</summary>
    Diagram,

    /// <summary>The truth table in a format.</summary>
    TruthTable,

    /// <summary>The statistics and the diagnostics.</summary>
    Analysis,

    /// <summary>The Markdown report.</summary>
    Report,
}
