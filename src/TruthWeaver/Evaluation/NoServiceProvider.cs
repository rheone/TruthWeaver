namespace TruthWeaver.Evaluation;

/// <summary>The provider <see cref="CompiledRule{TContext}.EvaluateAsync"/> uses when a caller passes no services: it resolves nothing.</summary>
internal sealed class NoServiceProvider : IServiceProvider
{
    /// <summary>Gets the shared instance.</summary>
    public static NoServiceProvider Instance { get; } = new();

    /// <inheritdoc />
    public object? GetService(Type serviceType)
    {
        return null;
    }
}
