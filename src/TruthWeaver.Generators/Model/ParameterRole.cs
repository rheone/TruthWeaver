namespace TruthWeaver.Generators.Model;

/// <summary>What a parameter of a predicate method receives from the generated evaluation delegate.</summary>
internal enum ParameterRole
{
    /// <summary>The application context, the first parameter.</summary>
    Context,

    /// <summary>A named rule argument, read from <c>PredicateArguments</c>.</summary>
    Argument,

    /// <summary>The cancellation token of the evaluation.</summary>
    CancellationToken,
}
