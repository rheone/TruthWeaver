namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

/// <summary>
/// The tests that build Terminal.Gui views. Terminal.Gui keeps process-wide state (the application instance,
/// configuration and default key bindings), so these tests run one at a time and apart from every other test.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class TerminalCollection
{
    /// <summary>The name of the collection.</summary>
    public const string Name = "Terminal.Gui views";

    private TerminalCollection() { }
}
