namespace TruthWeaver.AspNetCore;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TruthWeaver.Evaluation;

/// <summary>
/// An authorization requirement that a TruthWeaver rule decides. The requirement is met only when the rule is
/// <see cref="TruthValue.True"/>, or <see cref="TruthValue.Unknown"/> when <see cref="AllowUnknown"/> is set. Create one
/// with <c>RequireRule</c> or <c>AddTruthWeaverPolicy</c>; the handler that <c>AddTruthWeaverAuthorization</c> registers
/// evaluates it.
/// </summary>
public abstract class RuleRequirement : IAuthorizationRequirement
{
    /// <summary>The name of a requirement whose options give no name and that no named policy created.</summary>
    public const string DefaultName = "rule";

    /// <summary>Initializes a new instance of the <see cref="RuleRequirement"/> class.</summary>
    /// <param name="name">The name for logs and problem-details responses.</param>
    /// <param name="options">The options of the requirement.</param>
    private protected RuleRequirement(string name, RuleRequirementOptions options)
    {
        this.Name = name;
        this.AllowUnknown = options.AllowUnknown;
        this.EvaluationOptions = options.Evaluation;
    }

    /// <summary>Gets the name of the requirement, for logs and problem-details responses.</summary>
    public string Name { get; }

    /// <summary>Gets a value indicating whether an <see cref="TruthValue.Unknown"/> result meets the requirement.</summary>
    public bool AllowUnknown { get; }

    /// <summary>Gets the evaluation options of each check, or <see langword="null"/> for the defaults.</summary>
    public EvaluationOptions? EvaluationOptions { get; }

    /// <summary>Gets a value indicating whether <paramref name="decision"/> meets this requirement.</summary>
    /// <param name="decision">The decision of the rule.</param>
    /// <returns>
    /// <see langword="true"/> for a <see cref="TruthValue.True"/> result, and for <see cref="TruthValue.Unknown"/> when
    /// <see cref="AllowUnknown"/> is set; otherwise <see langword="false"/>.
    /// </returns>
    public bool IsMetBy(Decision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return decision.Result == TruthValue.True || (this.AllowUnknown && decision.Result == TruthValue.Unknown);
    }

    /// <summary>Maps the authorization check to the context of the rule and evaluates the rule.</summary>
    /// <param name="context">The authorization check.</param>
    /// <param name="http">The HTTP context of the request, or <see langword="null"/> outside a request.</param>
    /// <param name="services">The service provider of the evaluation.</param>
    /// <param name="cancellationToken">Cancels the evaluation.</param>
    /// <returns>The decision of the rule.</returns>
    internal abstract Task<Decision> EvaluateAsync(
        AuthorizationHandlerContext context,
        HttpContext? http,
        IServiceProvider services,
        CancellationToken cancellationToken
    );
}
