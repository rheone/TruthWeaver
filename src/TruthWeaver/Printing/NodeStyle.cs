namespace TruthWeaver.Printing;

/// <summary>
/// The style that a <see cref="MermaidOptions.NodeStyle"/> callback picks for one node: the built-in
/// highlight, the built-in mute, or a class name that the caller defines.
/// </summary>
public sealed record NodeStyle
{
    private NodeStyle(string className, bool isBuiltIn)
    {
        this.ClassName = className;
        this.IsBuiltIn = isBuiltIn;
    }

    /// <summary>Gets the style that draws attention to a node. The printer defines it from <see cref="MermaidPalette.Highlight"/>.</summary>
    public static NodeStyle Highlight { get; } = new("brHighlight", isBuiltIn: true);

    /// <summary>Gets the style that pushes a node into the background. The printer defines it from <see cref="MermaidPalette.Mute"/>.</summary>
    public static NodeStyle Mute { get; } = new("brMute", isBuiltIn: true);

    /// <summary>Gets the name of the Mermaid class that the printer assigns to the node.</summary>
    public string ClassName { get; }

    /// <summary>Gets a value indicating whether the printer defines the class from the palette.</summary>
    internal bool IsBuiltIn { get; }

    /// <summary>
    /// Creates a style that assigns the Mermaid class <paramref name="className"/>. The printer does not
    /// define the class. Add a <c>classDef</c> line for it to the output, or define it in the page that
    /// renders the diagram.
    /// </summary>
    /// <param name="className">The class name. It holds letters, digits, underscores and hyphens only.</param>
    /// <returns>The style.</returns>
    /// <exception cref="ArgumentException"><paramref name="className"/> is empty or holds another character.</exception>
    public static NodeStyle Custom(string className)
    {
        ArgumentException.ThrowIfNullOrEmpty(className);

        // A space, semicolon or line break would end the Mermaid class statement and start another one.
        if (className.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-')))
        {
            throw new ArgumentException("A class name holds letters, digits, underscores and hyphens only.", nameof(className));
        }

        return new NodeStyle(className, isBuiltIn: false);
    }
}
