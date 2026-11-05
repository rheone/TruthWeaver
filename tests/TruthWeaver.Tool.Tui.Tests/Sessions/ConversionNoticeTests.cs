namespace TruthWeaver.Tool.Tui.Tests.Sessions;

using TruthWeaver.Tool.Tui.Sessions;

public sealed class ConversionNoticeTests
{
    /// <summary>The comments of a well-formed YAML text are counted.</summary>
    [Fact]
    public void CountYamlComments_WellFormedYaml_CountsTheComments_Test()
    {
        int count = ConversionNotice.CountYamlComments("# one\nop: and # two\n");

        Assert.Equal(2, count);
    }

    /// <summary>A text with an unclosed flow sequence does not throw a scanner exception.</summary>
    [Fact]
    public void CountYamlComments_UnclosedFlowSequence_DoesNotThrow_Test()
    {
        Exception? thrown = Record.Exception(() => ConversionNotice.CountYamlComments("op: [and, or\nother: x\n"));

        Assert.Null(thrown);
    }
}
