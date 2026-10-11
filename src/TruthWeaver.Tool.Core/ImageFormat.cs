namespace TruthWeaver.Tool.Core;

/// <summary>The image format that <see cref="ImageRenderer"/> asks the external renderer for.</summary>
public enum ImageFormat
{
    /// <summary>Scalable Vector Graphics, <c>.svg</c>. The default.</summary>
    Svg,

    /// <summary>Portable Network Graphics, <c>.png</c>.</summary>
    Png,
}
