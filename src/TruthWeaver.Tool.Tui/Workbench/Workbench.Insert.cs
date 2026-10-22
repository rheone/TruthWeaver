namespace TruthWeaver.Tool.Tui.Workbench;

using TruthWeaver.Compilation;

/// <content>
/// The part of the workbench that puts a piece of rule text into the editor at the cursor: the Insert menu. The editor owns
/// the cursor and the selection, so the workbench only asks it to insert.
/// </content>
internal sealed partial class Workbench
{
    /// <summary>Occurs when text should go into the editor at the cursor, in place of the selection.</summary>
    public event EventHandler<string>? InsertRequested;

    /// <summary>
    /// Gets whether the open rule takes an insert, which is only rule text. For JSON or YAML the status line says that the
    /// Insert menu is not available and how to make it so.
    /// </summary>
    /// <returns><see langword="true"/> when the rule is rule text.</returns>
    public bool CanInsert()
    {
        if (this.document.Format == RuleFormat.Text)
        {
            return true;
        }

        this.SetStatus(
            $"Insert is not available for {FormatName(this.document.Format)}: it adds rule text. Convert the rule to rule text first (F4)."
        );
        return false;
    }

    /// <summary>
    /// Inserts rule text at the cursor of the editor, in place of the selection. The inserted text is one edit in the undo
    /// history. Only rule text takes an insert: in JSON or YAML the text would not be a rule, so nothing is inserted and
    /// the status line says why.
    /// </summary>
    /// <param name="text">The rule text.</param>
    /// <param name="what">What is inserted, for the status line.</param>
    /// <returns><see langword="true"/> when the editor took the text.</returns>
    public bool InsertText(string text, string what)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);
        ArgumentNullException.ThrowIfNull(what);

        if (!this.CanInsert())
        {
            return false;
        }

        if (this.InsertRequested is null)
        {
            this.SetStatus("Insert is not available: there is no editor.");
            return false;
        }

        this.InsertRequested.Invoke(this, text);
        this.SetStatus($"Inserted {what}. Ctrl+Z undoes it.");
        return true;
    }
}
