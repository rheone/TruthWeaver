namespace TruthWeaver.Tool.Tui;

/// <summary>The part of the workbench state that saves the assignment as cases of a rule test file and runs the file.</summary>
internal sealed partial class Workbench
{
    private RuleTestSession? ruleTests;

    /// <summary>Occurs after the rule test session changed (its path, its name, its cases or its message).</summary>
    public event EventHandler? RuleTestsChanged;

    /// <summary>Gets the rule test file of the workbench. After a change, call <see cref="NotifyRuleTestsChanged"/>.</summary>
    public RuleTestSession RuleTests => this.ruleTests ??= new RuleTestSession();

    /// <summary>Tells the panes that the rule test session changed.</summary>
    public void NotifyRuleTestsChanged()
    {
        this.RuleTestsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Reads the cases of the rule test file at <see cref="RuleTestSession.FilePath"/>.</summary>
    public void OpenRuleTestFile()
    {
        _ = this.RuleTests.Open();
        this.FinishRuleTestStep();
    }

    /// <summary>Saves the current assignment as the case named <see cref="RuleTestSession.CaseName"/>.</summary>
    public void SaveCase()
    {
        // A case records the decision of the assignment as it is now, so the outcome is evaluated again.
        this.outcome = null;
        string ruleText = this.Document.Rule?.CanonicalText ?? string.Empty;
        _ = this.RuleTests.SaveCase(this.Assignment, this.Outcome, ruleText);
        this.FinishRuleTestStep();
    }

    /// <summary>Runs the cases of the rule test file on the open rule.</summary>
    /// <returns>A task that completes when the run is recorded.</returns>
    public async Task RunRuleTestsAsync()
    {
        _ = await this.RuleTests.RunAsync(this.Document, this.Settings).ConfigureAwait(false);
        this.FinishRuleTestStep();
    }

    /// <summary>Loads a case of the rule test file into the assignment.</summary>
    /// <param name="index">The index of the case in <see cref="RuleTestSession.Rows"/>.</param>
    public void LoadCase(int index)
    {
        if (this.RuleTests.LoadCase(index, this.Assignment))
        {
            this.NotifyAssignmentChanged();
        }

        this.FinishRuleTestStep();
    }

    /// <summary>Removes a case from the rule test file.</summary>
    /// <param name="index">The index of the case in <see cref="RuleTestSession.Rows"/>.</param>
    public void DeleteCase(int index)
    {
        _ = this.RuleTests.DeleteCase(index);
        this.FinishRuleTestStep();
    }

    private void FinishRuleTestStep()
    {
        this.SetStatus(this.RuleTests.Message);
        this.NotifyRuleTestsChanged();
    }
}
