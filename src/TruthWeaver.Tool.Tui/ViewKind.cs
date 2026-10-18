namespace TruthWeaver.Tool.Tui;

/// <summary>The forms in which the view pane shows the rule.</summary>
internal enum ViewKind
{
    /// <summary>The indented plain text tree.</summary>
    Tree,

    /// <summary>The rule text in the operator style of the view settings.</summary>
    RuleText,

    /// <summary>The equation in one dialect.</summary>
    Equation,

    /// <summary>The Mermaid flowchart text.</summary>
    Mermaid,

    /// <summary>The Graphviz DOT text.</summary>
    Dot,

    /// <summary>The HTML tree.</summary>
    Html,

    /// <summary>The JSON tree format.</summary>
    Json,

    /// <summary>The YAML tree format.</summary>
    Yaml,
}
