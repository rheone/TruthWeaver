namespace TruthWeaver.Samples.AspNetCore;

/// <summary>
/// A stand-in for an external on-call roster service. Carol is on call. Set <see cref="Offline"/> to simulate an outage:
/// the lookup then throws, the <c>isOnCall</c> term becomes a fault, and the rule is <c>Unknown</c>.
/// </summary>
public sealed class OnCallRoster
{
    private static readonly string[] OnCall = ["carol"];

    /// <summary>Gets or sets a value indicating whether the roster service is offline.</summary>
    public bool Offline { get; set; }

    /// <summary>Looks up whether a user is on call.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <returns><see langword="true"/> when the user is on call.</returns>
    /// <exception cref="InvalidOperationException">The roster service is offline.</exception>
    public bool IsOnCall(string userId)
    {
        if (this.Offline)
        {
            throw new InvalidOperationException("The on-call roster service is offline.");
        }

        return OnCall.Contains(userId, StringComparer.Ordinal);
    }
}
