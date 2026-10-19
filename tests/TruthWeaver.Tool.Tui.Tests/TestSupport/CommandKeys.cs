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
    public static Key InspectSecond => SecondOf("Inspect");

    /// <summary>Gets the key of the Next evaluation step command.</summary>
    public static Key NextStep => Copy(Of("Next evaluation step").Key);

    /// <summary>Gets the key of the Previous evaluation step command.</summary>
    public static Key PreviousStep => Copy(Of("Previous evaluation step").Key);

    /// <summary>Gets a copy of the key of the default command with this title, which a test may press without changing the command.</summary>
    /// <param name="title">The title of the command.</param>
    /// <returns>A new key event for the key.</returns>
    public static Key PrimaryOf(string title)
    {
        return Copy(Of(title).Key);
    }

    /// <summary>Gets a copy of the second key of the default command with this title; the command must have a second key.</summary>
    /// <param name="title">The title of the command.</param>
    /// <returns>A new key event for the second key.</returns>
    public static Key SecondOf(string title)
    {
        return Copy(Of(title).SecondKey ?? throw new InvalidOperationException($"The command '{title}' has no second key."));
    }

    /// <summary>Gets a copy of the second key of a command, which a test may press without changing the command.</summary>
    /// <param name="command">The command; it must have a second key.</param>
    /// <returns>A new key event for the second key.</returns>
    public static Key SecondOf(WorkbenchCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return Copy(
            command.SecondKey ?? throw new InvalidOperationException($"The command '{command.Title}' has no second key.")
        );
    }

    // The default command with this title; a missing title fails the test with the title in the message.
    private static WorkbenchCommand Of(string title)
    {
        return WorkbenchCommands.Default.Single(c => c.Title == title);
    }

    // A key event is mutable (the window marks it handled), so a test gets its own copy and never changes the key that the
    // command keeps for the life of the process.
    private static Key Copy(Key key)
    {
        return new Key(key.KeyCode);
    }
}
