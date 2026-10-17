namespace TruthWeaver.AspNetCore;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using TruthWeaver.Evaluation;

/// <summary>A <see cref="RuleRequirement"/> whose rule reads a context of type <typeparamref name="TContext"/>.</summary>
/// <typeparam name="TContext">The context type of the rule.</typeparam>
public sealed class RuleRequirement<TContext> : RuleRequirement
{
    private readonly Func<AuthorizationHandlerContext, HttpContext?, TContext> map;

    /// <summary>Initializes a new instance of the <see cref="RuleRequirement{TContext}"/> class.</summary>
    /// <param name="rule">The compiled rule.</param>
    /// <param name="map">Builds the context of the rule from the authorization check and the HTTP context.</param>
    /// <param name="options">The options, or <see langword="null"/> for <see cref="RuleRequirementOptions.Default"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> or <paramref name="map"/> is <see langword="null"/>.</exception>
    public RuleRequirement(
        CompiledRule<TContext> rule,
        Func<AuthorizationHandlerContext, HttpContext?, TContext> map,
        RuleRequirementOptions? options = null
    )
        : base(NameOf(rule, options), options ?? RuleRequirementOptions.Default)
    {
        ArgumentNullException.ThrowIfNull(map);
        this.Rule = rule;
        this.map = map;
    }

    /// <summary>Gets the compiled rule.</summary>
    public CompiledRule<TContext> Rule { get; }

    /// <inheritdoc/>
    internal override Task<Decision> EvaluateAsync(
        AuthorizationHandlerContext context,
        HttpContext? http,
        IServiceProvider services,
        CancellationToken cancellationToken
    )
    {
        TContext ruleContext = this.map(context, http);
        return this.Rule.EvaluateAsync(
            ruleContext,
            services,
            options: this.EvaluationOptions,
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Checks the rule before the base constructor runs, and picks the name: the option, else <see cref="RuleRequirement.DefaultName"/>.
    /// The rule text is never the default name, because a problem-details response shows the name to the client.
    /// </summary>
    private static string NameOf(CompiledRule<TContext> rule, RuleRequirementOptions? options)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return options?.Name ?? DefaultName;
    }
}
