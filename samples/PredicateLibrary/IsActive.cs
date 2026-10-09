namespace TruthWeaver.Samples.PredicateLibrary;

using TruthWeaver.Abstractions;

/// <summary>
/// Shape 1: a class that implements <see cref="IPredicate{TContext}"/>. The class carries its own
/// <see cref="Schema"/>, so the application registers the type and needs nothing else.
/// </summary>
public sealed class IsActive : IPredicate<Account>
{
    /// <summary>Gets the name, label and arguments of the predicate. A rule refers to it as <c>isActive</c>.</summary>
    public static PredicateSchema Schema => PredicateSchema.NoArguments("isActive", "Is active", "Is this account active?");

    /// <remarks>A missing flag is not a failure. The answer is Unknown, and the rule decides what that means.</remarks>
    /// <inheritdoc />
    public ValueTask<TruthValue> EvaluateAsync(Account context, PredicateArguments args, CancellationToken cancellationToken) =>
        ValueTask.FromResult(
            context.IsActive switch
            {
                true => TruthValue.True,
                false => TruthValue.False,
                null => TruthValue.Unknown,
            }
        );
}
