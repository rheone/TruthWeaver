namespace TruthWeaver.Printing;

/// <summary>
/// The Mermaid <c>classDef</c> styles that <see cref="MermaidTreePrinter"/> uses for the evaluation
/// states and for the highlight and mute classes. Each property holds the style declaration of one
/// class, for example <c>fill:#d4edda,stroke:#28a745,color:#155724</c>. The printer adds the
/// <c>classDef</c> keyword, the class name and the closing semicolon.
/// </summary>
/// <remarks>
/// <para>
/// A style must not contain a line break or a semicolon. The printer replaces a line break with a
/// space and drops a semicolon, so a style cannot add a second Mermaid statement.
/// </para>
/// <para>
/// The presets use these colors.
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Light"/>: green <c>#d4edda</c> (true), red <c>#f8d7da</c> (false), yellow <c>#fff3cd</c> (unknown), gray <c>#e9ecef</c> (skipped).</description></item>
/// <item><description><see cref="ColorblindSafe"/>: Okabe-Ito blue <c>#0072B2</c> (true), vermilion <c>#D55E00</c> (false), yellow <c>#F0E442</c> family (unknown). The states differ in hue and in lightness, and they do not depend on a red and green pair.</description></item>
/// <item><description><see cref="Monochrome"/>: black and white only. A state is a stroke style: thick solid (true), dashed (false), dotted (unknown), thin dashed on gray (skipped).</description></item>
/// <item><description><see cref="Dark"/>: dark fills with light text and bright strokes, for a dark page background.</description></item>
/// </list>
/// </remarks>
public sealed record MermaidPalette
{
    /// <summary>Gets the style of a node that contributed <c>True</c>.</summary>
    public required string True { get; init; }

    /// <summary>Gets the style of a node that contributed <c>False</c>.</summary>
    public required string False { get; init; }

    /// <summary>Gets the style of a node that contributed <c>Unknown</c>.</summary>
    public required string Unknown { get; init; }

    /// <summary>Gets the style of a node that evaluation skipped through short-circuiting.</summary>
    public required string Skipped { get; init; }

    /// <summary>Gets the style that draws attention to a node.</summary>
    public required string Highlight { get; init; }

    /// <summary>Gets the style that pushes a node into the background.</summary>
    public required string Mute { get; init; }

    /// <summary>Gets the default palette: pale green, red, yellow and gray fills with dark text.</summary>
    public static MermaidPalette Light { get; } =
        new()
        {
            True = "fill:#d4edda,stroke:#28a745,color:#155724",
            False = "fill:#f8d7da,stroke:#dc3545,color:#721c24",
            Unknown = "fill:#fff3cd,stroke:#ffc107,color:#856404",
            Skipped = "fill:#e9ecef,stroke:#adb5bd,color:#6c757d,stroke-dasharray: 4 3",
            Highlight = "fill:#cce5ff,stroke:#004085,color:#002752,stroke-width:3px",
            Mute = "fill:#f8f9fa,stroke:#dee2e6,color:#5c636a",
        };

    /// <summary>Gets a palette that does not rely on a red and green pair, built from the Okabe-Ito colors.</summary>
    public static MermaidPalette ColorblindSafe { get; } =
        new()
        {
            True = "fill:#cfe8f7,stroke:#0072B2,color:#00304d",
            False = "fill:#f9dcc6,stroke:#D55E00,color:#5c2900",
            Unknown = "fill:#f7f2a8,stroke:#8c8300,color:#403c00",
            Skipped = "fill:#e9ecef,stroke:#adb5bd,color:#5c636a,stroke-dasharray: 4 3",
            Highlight = "fill:#f5dcea,stroke:#CC79A7,color:#4d1a37,stroke-width:3px",
            Mute = "fill:#f8f9fa,stroke:#dee2e6,color:#5c636a",
        };

    /// <summary>Gets a black and white palette that separates the states by stroke style, so a grayscale print stays readable.</summary>
    public static MermaidPalette Monochrome { get; } =
        new()
        {
            True = "fill:#fff,stroke:#000,color:#000,stroke-width:4px",
            False = "fill:#fff,stroke:#000,color:#000,stroke-width:2px,stroke-dasharray: 8 4",
            Unknown = "fill:#fff,stroke:#000,color:#000,stroke-width:2px,stroke-dasharray: 2 4",
            Skipped = "fill:#eee,stroke:#666,color:#555,stroke-width:1px,stroke-dasharray: 4 3",
            Highlight = "fill:#000,stroke:#000,color:#fff",
            Mute = "fill:#f5f5f5,stroke:#999,color:#666,stroke-width:1px",
        };

    /// <summary>Gets a palette with dark fills and light text, for a dark page background.</summary>
    public static MermaidPalette Dark { get; } =
        new()
        {
            True = "fill:#14532d,stroke:#4ade80,color:#dcfce7",
            False = "fill:#7f1d1d,stroke:#f87171,color:#fee2e2",
            Unknown = "fill:#713f12,stroke:#facc15,color:#fef9c3",
            Skipped = "fill:#1f2937,stroke:#9ca3af,color:#d1d5db,stroke-dasharray: 4 3",
            Highlight = "fill:#1e3a8a,stroke:#60a5fa,color:#dbeafe,stroke-width:3px",
            Mute = "fill:#111827,stroke:#4b5563,color:#9ca3af",
        };
}
