namespace TruthWeaver.Policies;

using System.Collections.Immutable;
using TruthWeaver.Evaluation;

/// <summary>
/// An immutable set of <see cref="Policy{TContext}"/> values and the <see cref="CombiningAlgorithm"/> that combines
/// them. Build it once and evaluate it for many contexts. Evaluation is Strong Kleene (ADR-0015): a policy whose
/// applicability is <see cref="TruthValue.Unknown"/> never becomes a silent permit, and a fault in a rule is an
/// Unknown, as in <see cref="CompiledRule{TContext}.EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/>.
/// </summary>
/// <typeparam name="TContext">The context type of the rules.</typeparam>
public sealed class PolicySet<TContext>
{
    // Sorted once at construction: higher priority first, ties keep the order of the caller (OrderByDescending is stable).
    private readonly ImmutableArray<Policy<TContext>> ordered;

    /// <summary>Initializes a new instance of the <see cref="PolicySet{TContext}"/> class.</summary>
    /// <param name="id">The identifier of the set.</param>
    /// <param name="algorithm">How the results of the policies are combined.</param>
    /// <param name="policies">The policies. The set copies them.</param>
    /// <param name="target">
    /// A rule that selects the requests that the set is for, or <see langword="null"/> (the default) for every request.
    /// When the target is <see cref="TruthValue.False"/> the set is <see cref="PolicyOutcome.NotApplicable"/> and no
    /// policy is evaluated. When it is Unknown the set cannot be more certain than <see cref="PolicyOutcome.Indeterminate"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="id"/> is empty or white space, <paramref name="algorithm"/> is not defined, or two policies share an identifier.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="policies"/> is <see langword="null"/>.</exception>
    public PolicySet(
        string id,
        CombiningAlgorithm algorithm,
        IEnumerable<Policy<TContext>> policies,
        CompiledRule<TContext>? target = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(policies);
        if (!Enum.IsDefined(algorithm))
        {
            throw new ArgumentException("The algorithm is not a defined CombiningAlgorithm.", nameof(algorithm));
        }

        Policy<TContext>[] copy = [.. policies];
        HashSet<string> seen = [with(StringComparer.Ordinal)];
        foreach (Policy<TContext> policy in copy)
        {
            ArgumentNullException.ThrowIfNull(policy);
            if (!seen.Add(policy.Id))
            {
                throw new ArgumentException($"Two policies have the identifier '{policy.Id}'.", nameof(policies));
            }
        }

        this.Id = id;
        this.Algorithm = algorithm;
        this.Target = target;
        this.ordered = [.. copy.OrderByDescending(policy => policy.Priority)];
    }

    /// <summary>Gets the identifier of the set.</summary>
    public string Id { get; }

    /// <summary>Gets the algorithm that combines the results of the policies.</summary>
    public CombiningAlgorithm Algorithm { get; }

    /// <summary>Gets the rule that selects the requests of the set, or <see langword="null"/> for every request.</summary>
    public CompiledRule<TContext>? Target { get; }

    /// <summary>Gets the policies in evaluation order: higher priority first, then the order given to the constructor.</summary>
    public IReadOnlyList<Policy<TContext>> Policies => this.ordered;

    /// <summary>
    /// Evaluates the set for a context. Policies run one at a time in priority order, and the evaluation stops at the first
    /// result that the algorithm cannot change. The arguments after <paramref name="context"/> are passed to every rule
    /// as for <see cref="CompiledRule{TContext}.EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="context">The application-supplied evaluation context.</param>
    /// <param name="services">The service provider for class-based predicates, or <see langword="null"/> for none.</param>
    /// <param name="dataSources">The named data sources, or <see langword="null"/> when no rule has variables.</param>
    /// <param name="options">Per-call evaluation options applied to each rule, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>The combined outcome, the fail-closed decision and the answer of each policy that was evaluated.</returns>
    /// <exception cref="OperationCanceledException">The caller's token, or <see cref="EvaluationOptions.Timeout"/> of one rule, cancelled the evaluation.</exception>
    public async Task<PolicyDecision> EvaluateAsync(
        TContext context,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        List<Fault> faults = [];
        List<PolicyResult> results = [];

        // One EvaluationState for the whole set's evaluation: the set's target, every policy's target and every
        // policy's condition share it, so a term common to several of those rules runs once (feature backlog 08). The
        // fault budget and the per-term memo are then whole-set concepts, like EvaluationState already is for one
        // rule's own nested quantifiers.
        EvaluationState sharedState = new((options ?? EvaluationOptions.Default).IncludeTrace);

        TruthValue setTarget = TruthValue.True;
        if (this.Target is not null)
        {
            setTarget = await RunAsync(
                    this.Target,
                    context,
                    services,
                    dataSources,
                    options,
                    sharedState,
                    faults,
                    cancellationToken
                )
                .ConfigureAwait(false);
            if (setTarget == TruthValue.False)
            {
                return Build(PolicyOutcome.NotApplicable, faults, results);
            }
        }

        foreach (Policy<TContext> policy in this.ordered)
        {
            TruthValue applies = await AppliesAsync(
                    policy,
                    context,
                    services,
                    dataSources,
                    options,
                    sharedState,
                    faults,
                    cancellationToken
                )
                .ConfigureAwait(false);
            results.Add(new PolicyResult(policy.Id, policy.Effect, applies));

            if (this.Decides(policy, applies))
            {
                break;
            }
        }

        PolicyOutcome outcome = this.Combine(results);

        // An Unknown set target only matters when some policy would otherwise have answered.
        if (setTarget == TruthValue.Unknown && outcome != PolicyOutcome.NotApplicable)
        {
            outcome = PolicyOutcome.Indeterminate;
        }

        return Build(outcome, faults, results);
    }

    // Evaluates one rule against the set's shared state and attributes to the caller's aggregate fault list only
    // the faults of the terms this rule itself references. A term shared with an earlier rule in the set faults at
    // most once there (the memo answers a repeat without invoking the predicate again), but every rule that
    // references it still lists that one fault in its own decision, exactly as it would if the rules did not share
    // state (feature backlog 08).
    private static async Task<TruthValue> RunAsync(
        CompiledRule<TContext> rule,
        TContext context,
        IServiceProvider? services,
        DataSources? dataSources,
        EvaluationOptions? options,
        EvaluationState sharedState,
        List<Fault> faults,
        CancellationToken cancellationToken
    )
    {
        Decision decision = await rule.EvaluateAsync(context, sharedState, services, dataSources, options, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlySet<TermIdentity> ownTerms = rule.TermIdentities;
        faults.AddRange(decision.Faults.Where(fault => ownTerms.Contains(fault.Term)));
        return decision.Result;
    }

    private static PolicyDecision Build(PolicyOutcome outcome, List<Fault> faults, List<PolicyResult> results)
    {
        // A permit is True, an Indeterminate is Unknown, and a deny or nothing applicable is False (default deny).
        TruthValue result = outcome switch
        {
            PolicyOutcome.Permit => TruthValue.True,
            PolicyOutcome.Indeterminate => TruthValue.Unknown,
            _ => TruthValue.False,
        };
        return new PolicyDecision(
            outcome,
            new Decision(result, new EquatableArray<Fault>(faults)),
            new EquatableArray<PolicyResult>(results)
        );
    }

    // The target is checked first so that a policy outside its target never runs its condition (and never records its faults).
    private static async Task<TruthValue> AppliesAsync(
        Policy<TContext> policy,
        TContext context,
        IServiceProvider? services,
        DataSources? dataSources,
        EvaluationOptions? options,
        EvaluationState sharedState,
        List<Fault> faults,
        CancellationToken cancellationToken
    )
    {
        TruthValue target = TruthValue.True;
        if (policy.Target is not null)
        {
            target = await RunAsync(
                    policy.Target,
                    context,
                    services,
                    dataSources,
                    options,
                    sharedState,
                    faults,
                    cancellationToken
                )
                .ConfigureAwait(false);
            if (target == TruthValue.False)
            {
                return TruthValue.False;
            }
        }

        TruthValue condition = await RunAsync(
                policy.Condition,
                context,
                services,
                dataSources,
                options,
                sharedState,
                faults,
                cancellationToken
            )
            .ConfigureAwait(false);

        // Strong Kleene AND of target and condition: a False condition wins over an Unknown target.
        if (condition == TruthValue.False)
        {
            return TruthValue.False;
        }

        return target == TruthValue.True ? condition : TruthValue.Unknown;
    }

    private static bool Has(List<PolicyResult> results, PolicyEffect effect, TruthValue applies)
    {
        return results.Exists(result => result.Effect == effect && result.Applies == applies);
    }

    private static PolicyOutcome OutcomeOf(PolicyEffect effect)
    {
        return effect == PolicyEffect.Permit ? PolicyOutcome.Permit : PolicyOutcome.Deny;
    }

    // True when the result cannot be changed by a later policy, so the walk stops.
    private bool Decides(Policy<TContext> policy, TruthValue applies)
    {
        return this.Algorithm switch
        {
            CombiningAlgorithm.DenyOverrides => applies == TruthValue.True && policy.Effect == PolicyEffect.Deny,
            CombiningAlgorithm.PermitOverrides => applies == TruthValue.True && policy.Effect == PolicyEffect.Permit,
            _ => applies != TruthValue.False,
        };
    }

    private PolicyOutcome Combine(List<PolicyResult> results)
    {
        if (this.Algorithm == CombiningAlgorithm.FirstApplicable)
        {
            // The walk stopped at the only result that is not False, so it is the last one, if any.
            if (results.Count == 0)
            {
                return PolicyOutcome.NotApplicable;
            }

            PolicyResult last = results[^1];
            return last.Applies switch
            {
                TruthValue.True => OutcomeOf(last.Effect),
                TruthValue.Unknown => PolicyOutcome.Indeterminate,
                _ => PolicyOutcome.NotApplicable,
            };
        }

        // The winning effect is checked first, definite before Unknown, and only then the other effect. An Unknown
        // policy of the winning effect blocks the other effect, so it never becomes a silent permit or deny.
        PolicyEffect winning = this.Algorithm == CombiningAlgorithm.DenyOverrides ? PolicyEffect.Deny : PolicyEffect.Permit;
        PolicyEffect other = winning == PolicyEffect.Deny ? PolicyEffect.Permit : PolicyEffect.Deny;

        if (Has(results, winning, TruthValue.True))
        {
            return OutcomeOf(winning);
        }

        if (Has(results, winning, TruthValue.Unknown))
        {
            return PolicyOutcome.Indeterminate;
        }

        if (Has(results, other, TruthValue.True))
        {
            return OutcomeOf(other);
        }

        return Has(results, other, TruthValue.Unknown) ? PolicyOutcome.Indeterminate : PolicyOutcome.NotApplicable;
    }
}
