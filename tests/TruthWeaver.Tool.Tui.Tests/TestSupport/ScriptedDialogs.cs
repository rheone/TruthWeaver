namespace TruthWeaver.Tool.Tui.Tests.TestSupport;

using TruthWeaver.Tool.Tui.Hosting;
using TruthWeaver.Tool.Tui.Sessions;

/// <summary>Answers the extra workbench questions from queued values, so no dialog opens.</summary>
internal sealed class ScriptedDialogs : IWorkbenchDialogs
{
    /// <summary>Gets the answers to <see cref="AskText"/>, in order. An empty queue cancels.</summary>
    public Queue<string?> Texts { get; } = new();

    /// <summary>Gets the answers to <see cref="Choose"/>, in order. An empty queue cancels.</summary>
    public Queue<int?> Choices { get; } = new();

    /// <summary>Gets or sets the folder that <see cref="AskFolder"/> returns.</summary>
    public string? Folder { get; set; }

    /// <summary>Gets or sets a value indicating whether <see cref="Confirm"/> agrees.</summary>
    public bool Agree { get; set; }

    /// <summary>Gets the entries of the last list that <see cref="Choose"/> showed.</summary>
    public IReadOnlyList<string> LastItems { get; private set; } = [];

    /// <summary>Gets the text of the accept button of the last list that <see cref="Choose"/> showed.</summary>
    public string LastAccept { get; private set; } = string.Empty;

    /// <summary>Gets the filter that the last <see cref="ChooseFiltered"/> was given: the text of the search box to the indexes shown.</summary>
    public Func<string, IReadOnlyList<int>>? LastFilter { get; private set; }

    /// <summary>Gets the messages of the questions that <see cref="Confirm"/> was asked.</summary>
    public List<string> Confirmations { get; } = [];

    /// <summary>Gets the answers to <see cref="AskExport"/>, in order. An empty queue cancels.</summary>
    public Queue<ExportRequest?> ExportRequests { get; } = new();

    /// <summary>Gets what the last <see cref="AskExport"/> was given.</summary>
    public ExportDialogModel? LastExportModel { get; private set; }

    /// <inheritdoc />
    public string? AskText(string title, string initial)
    {
        return this.Texts.TryDequeue(out string? text) ? text : null;
    }

    /// <inheritdoc />
    public int? Choose(string title, IReadOnlyList<string> items, string accept = "_OK")
    {
        this.LastItems = items;
        this.LastAccept = accept;
        return this.Choices.TryDequeue(out int? choice) ? choice : null;
    }

    /// <inheritdoc />
    public int? ChooseFiltered(string title, IReadOnlyList<string> items, Func<string, IReadOnlyList<int>> filter)
    {
        this.LastItems = items;
        this.LastFilter = filter;
        return this.Choices.TryDequeue(out int? choice) ? choice : null;
    }

    /// <inheritdoc />
    public string? AskFolder(string title)
    {
        return this.Folder;
    }

    /// <inheritdoc />
    public bool Confirm(string title, string message, string confirm)
    {
        this.Confirmations.Add(message);
        return this.Agree;
    }

    /// <inheritdoc />
    public ExportRequest? AskExport(ExportDialogModel model)
    {
        this.LastExportModel = model;
        return this.ExportRequests.TryDequeue(out ExportRequest? request) ? request : null;
    }
}
