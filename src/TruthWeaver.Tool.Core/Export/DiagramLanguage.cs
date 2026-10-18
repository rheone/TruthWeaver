namespace TruthWeaver.Tool.Core.Export;

/// <summary>The language of a diagram that <see cref="ImageRenderer"/> renders, which selects the external renderer.</summary>
public enum DiagramLanguage
{
    /// <summary>Graphviz DOT, rendered by the Graphviz <c>dot</c> program.</summary>
    Dot,

    /// <summary>A Mermaid flowchart, rendered by the Mermaid CLI program <c>mmdc</c>.</summary>
    Mermaid,
}
