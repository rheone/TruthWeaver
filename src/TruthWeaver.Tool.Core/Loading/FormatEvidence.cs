namespace TruthWeaver.Tool.Core.Loading;

/// <summary>What decided the format of an input, in the order <see cref="FormatDetector"/> tries them.</summary>
public enum FormatEvidence
{
    /// <summary>The caller named the format, for example with a <c>--format</c> option.</summary>
    Override,

    /// <summary>The file extension named the format.</summary>
    Extension,

    /// <summary>The content of the input decided the format.</summary>
    Content,
}
