namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class FaultTests
{
    /// <summary>A fault records the faulting term and its exception.</summary>
    [Fact]
    public void Fault_Construction_RecordsTermAndException_Test()
    {
        TermIdentity term = new("hasRole", []);
        InvalidOperationException exception = new("boom");

        Fault fault = new(term, exception);

        Assert.Same(term, fault.Term);
        Assert.Same(exception, fault.Exception);
    }
}
