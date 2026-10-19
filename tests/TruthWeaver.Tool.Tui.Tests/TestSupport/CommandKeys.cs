namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

using Terminal.Gui.Input;
using TruthWeaver.Tool.Tui.Workbench;

/// <summary>
/// The keys of the commands that tests press, read from <see cref="WorkbenchCommands.Default"/>. The commands are the only
/// source of a key, so a test never repeats one and a changed key needs no change in the tests.
/// </summary>
internal static class CommandKeys
{
    /// <summary>Gets the key of the Inspect command.</summary>
    public static Key Inspect => Copy(Of("Inspect").Key);

    /// <summary>Gets the second key of the Inspect command.</summary>
    public static Key InspectSecond => Second("Inspect");

    /// <summary>Gets the key of the Next evaluation step command.</summary>
    public static Key NextStep => Copy(Of("Next evaluation step").Key);

    /// <summary>Gets the key of the Previous evaluation step command.</summary>
    public static Key PreviousStep => Copy(Of("Previous evaluation step").Key);

    // The default command with this title; a missing title fails the test with the title in the message.
    private static WorkbenchCommand Of(string title)
    {
        return WorkbenchCommands.Default.Single(c => c.Title == title);
    }

    private static Key Second(string title)
    {
        return Copy(Of(title).SecondKey ?? throw new InvalidOperationException($"The command '{title}' has no second key."));
    }

    // A key event is mutable (the window marks it handled), so a test gets its own copy and never changes the key that the
    // command keeps for the life of the process.
    private static Key Copy(Key key)
    {
        return new Key(key.KeyCode);
    }
}
