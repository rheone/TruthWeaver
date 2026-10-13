namespace TruthWeaver.Tool.Core;

/// <summary>The options of <see cref="ImageRenderer"/>.</summary>
public sealed record ImageRenderOptions
{
    /// <summary>Gets the default options: SVG, the <c>PATH</c> environment variable, and a time limit of one minute.</summary>
    public static ImageRenderOptions Default { get; } = new();

    /// <summary>Gets the image format. The default is <see cref="ImageFormat.Svg"/>.</summary>
    public ImageFormat Format { get; init; } = ImageFormat.Svg;

    /// <summary>
    /// Gets the directories to look in for the renderer, in the form of the <c>PATH</c> environment variable (separated by
    /// <see cref="Path.PathSeparator"/>), or <see langword="null"/> (the default) for the <c>PATH</c> environment variable.
    /// </summary>
    public string? SearchPath { get; init; }

    /// <summary>
    /// Gets the time that the renderer has to finish. The renderer is stopped after it, and the render fails. The default is
    /// one minute.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(1);
}
