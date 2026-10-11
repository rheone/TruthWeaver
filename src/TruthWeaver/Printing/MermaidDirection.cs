namespace TruthWeaver.Printing;

/// <summary>The layout direction of a Mermaid <c>flowchart</c> produced by <see cref="MermaidTreePrinter"/>.</summary>
public enum MermaidDirection
{
    /// <summary>Top to bottom (<c>TD</c>). This is the default.</summary>
    TopDown,

    /// <summary>Left to right (<c>LR</c>).</summary>
    LeftRight,

    /// <summary>Bottom to top (<c>BT</c>).</summary>
    BottomTop,

    /// <summary>Right to left (<c>RL</c>).</summary>
    RightLeft,
}
