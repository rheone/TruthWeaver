namespace TruthWeaver.Samples.AspNetCore;

using System.Security.Claims;
using TruthWeaver.Abstractions;
using TruthWeaver.AspNetCore;

/// <summary>
/// A class-based predicate with a service from the container: is the user on call? The container creates it for each
/// evaluation from the request services, so it can use the <see cref="OnCallRoster"/>.
/// </summary>
/// <param name="roster">The roster service.</param>
public sealed class IsOnCall(OnCallRoster roster) : IPredicate<AuthorizationContext<Document>>
{
    /// <summary>Gets the schema. A rule refers to the predicate as <c>isOnCall</c>.</summary>
    public static PredicateSchema Schema =>
        PredicateSchema.NoArguments("isOnCall", "Is on call", "Is the user on call, according to the roster service?");

    /// <inheritdoc/>
    public ValueTask<TruthValue> EvaluateAsync(
        AuthorizationContext<Document> context,
        PredicateArguments args,
        CancellationToken cancellationToken
    )
    {
        // A user without an identifier cannot be looked up, so the answer is Unknown, not False.
        string? userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
        {
            return ValueTask.FromResult(TruthValue.Unknown);
        }

        // An exception from the roster is not caught here: the evaluator records it as a fault, and the term is Unknown.
        return ValueTask.FromResult(roster.IsOnCall(userId) ? TruthValue.True : TruthValue.False);
    }
}
