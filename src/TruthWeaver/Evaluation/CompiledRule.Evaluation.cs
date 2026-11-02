namespace TruthWeaver.Evaluation;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Diagnostics;
using TruthWeaver.Json;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Rewriting;

public sealed partial class CompiledRule<TContext>
{
    /// <summary>
    /// Evaluates this rule against a context. Every argument after <paramref name="context"/> is optional, so a
    /// caller supplies only what the rule needs. A variable reference (<c>from("source", "query")</c>) resolves from the
    /// matching entry of <paramref name="dataSources"/> (ADR-0006). A reference whose source is not supplied,
    /// whose query matches nothing or too much, whose result does not fit the argument, or whose source fails makes
    /// its term <see cref="TruthValue.Unknown"/> and records a <see cref="Fault"/> carrying a
    /// <see cref="VariableResolutionException"/>; the fault never contains the resolved value.
    /// </summary>
    /// <param name="context">The application-supplied evaluation context.</param>
    /// <param name="services">
    /// The service provider to resolve class-based predicates from, fresh for this call. Never
    /// captured once at registration, so scoped dependencies (a <c>DbContext</c>, a scoped
    /// <c>HttpClient</c>) resolve correctly even though this <see cref="CompiledRule{TContext}"/>
    /// outlives any one scope (ADR-0002). <see langword="null"/> means an empty provider: a class-based
    /// predicate then faults (<see cref="TruthValue.Unknown"/> plus a <see cref="Fault"/>) as for any missing registration.
    /// </param>
    /// <param name="dataSources">The named data sources for this evaluation, or <see langword="null"/> when the rule has no variables.</param>
    /// <param name="options">Per-call evaluation options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>The evaluation's <see cref="Decision"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> has a <see cref="EvaluationOptions.FaultBudget"/> or <see cref="EvaluationOptions.MaxElementEvaluations"/> below 1, or an unusable <see cref="EvaluationOptions.Timeout"/>.</exception>
    /// <exception cref="OperationCanceledException">The caller's token, or <see cref="EvaluationOptions.Timeout"/>, cancelled the evaluation. Neither is recorded as a fault.</exception>
    public Task<Decision> EvaluateAsync(
        TContext context,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return this.EvaluateCoreAsync(
            context,
            services,
            dataSources,
            options,
            sharedState: null,
            sharedStateRequired: false,
            cancellationToken
        );
    }

    /// <summary>
    /// Evaluates this rule against each context of a sequence and yields one <see cref="Decision"/> per context, in the
    /// order of the contexts. Each context is one <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/> call with the same
    /// <paramref name="services"/>, <paramref name="dataSources"/> and <paramref name="options"/>, so
    /// <see cref="EvaluationOptions.Timeout"/> and <see cref="EvaluationOptions.FaultBudget"/> apply to each context
    /// separately. A predicate fault in one context makes only that decision <see cref="TruthValue.Unknown"/>; the
    /// other contexts are evaluated as usual.
    /// </summary>
    /// <remarks>
    /// At most <paramref name="maxDegreeOfParallelism"/> evaluations are in flight at one time, and a decision that
    /// finishes early waits for the decisions before it, so the output order never depends on timing. The sequence is
    /// read lazily, one context per free slot. The default of 1 evaluates one context at a time; use a higher value
    /// only when the predicates and <paramref name="services"/> are safe to call from several threads at once. When the
    /// consumer stops reading, or an evaluation throws, the evaluations still in flight are cancelled and awaited
    /// before the enumeration ends. Collect every decision with <c>ToArrayAsync</c> from <c>System.Linq</c>.
    /// </remarks>
    /// <param name="contexts">The contexts to evaluate, read lazily.</param>
    /// <param name="services">The service provider for every evaluation, as for <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/>. With a parallelism above 1 it is used from several threads.</param>
    /// <param name="dataSources">The data sources for every evaluation, or <see langword="null"/> when the rule has no variables. They are only read.</param>
    /// <param name="options">The options for every evaluation, or <see langword="null"/> for the defaults.</param>
    /// <param name="maxDegreeOfParallelism">The largest number of evaluations in flight at one time. The default is 1.</param>
    /// <param name="cancellationToken">A token that stops the batch. It reaches every evaluation in flight.</param>
    /// <returns>The decisions, one per context, in the order of <paramref name="contexts"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contexts"/> is <see langword="null"/>. Thrown when the method is called.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is below 1, or <paramref name="options"/> has a <see cref="EvaluationOptions.FaultBudget"/> or <see cref="EvaluationOptions.MaxElementEvaluations"/> below 1, or an unusable <see cref="EvaluationOptions.Timeout"/>. Thrown when the method is called.</exception>
    /// <exception cref="OperationCanceledException">The token cancelled the batch, or <see cref="EvaluationOptions.Timeout"/> expired for one context. Thrown during the enumeration; neither is recorded as a fault.</exception>
    public IAsyncEnumerable<Decision> EvaluateManyAsync(
        IEnumerable<TContext> contexts,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? options = null,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(contexts);
        return this.EvaluateManyAsync(
            contexts.ToAsyncEnumerable(),
            services,
            dataSources,
            options,
            maxDegreeOfParallelism,
            cancellationToken
        );
    }

    /// <summary>
    /// Evaluates this rule against each context of an asynchronous stream and yields one <see cref="Decision"/> per
    /// context, in the order the stream produces them. The behavior is the same as for the
    /// <see cref="IEnumerable{T}"/> overload of <c>EvaluateManyAsync</c>: options per context, bounded parallelism,
    /// stable order and cancellation of the evaluations in flight when the enumeration stops.
    /// </summary>
    /// <param name="contexts">The contexts to evaluate, read lazily with <paramref name="cancellationToken"/>.</param>
    /// <param name="services">The service provider for every evaluation, as for <see cref="EvaluateAsync(TContext, IServiceProvider?, DataSources?, EvaluationOptions?, CancellationToken)"/>. With a parallelism above 1 it is used from several threads.</param>
    /// <param name="dataSources">The data sources for every evaluation, or <see langword="null"/> when the rule has no variables. They are only read.</param>
    /// <param name="options">The options for every evaluation, or <see langword="null"/> for the defaults.</param>
    /// <param name="maxDegreeOfParallelism">The largest number of evaluations in flight at one time. The default is 1.</param>
    /// <param name="cancellationToken">A token that stops the batch. It reaches the stream and every evaluation in flight.</param>
    /// <returns>The decisions, one per context, in the order of <paramref name="contexts"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contexts"/> is <see langword="null"/>. Thrown when the method is called.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDegreeOfParallelism"/> is below 1, or <paramref name="options"/> has a <see cref="EvaluationOptions.FaultBudget"/> or <see cref="EvaluationOptions.MaxElementEvaluations"/> below 1, or an unusable <see cref="EvaluationOptions.Timeout"/>. Thrown when the method is called.</exception>
    /// <exception cref="OperationCanceledException">The token cancelled the batch, or <see cref="EvaluationOptions.Timeout"/> expired for one context. Thrown during the enumeration; neither is recorded as a fault.</exception>
    public IAsyncEnumerable<Decision> EvaluateManyAsync(
        IAsyncEnumerable<TContext> contexts,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? options = null,
        int maxDegreeOfParallelism = 1,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(contexts);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreeOfParallelism, 1);

        // Checked here, not in the iterator, so a bad option fails at the call rather than at the first read.
        options?.ThrowIfInvalid(nameof(options));

        return BatchEvaluator.EvaluateAsync(
            this,
            contexts,
            services,
            dataSources,
            options,
            maxDegreeOfParallelism,
            cancellationToken
        );
    }

    /// <summary>
    /// Evaluates this rule using <paramref name="sharedState"/> instead of a fresh <see cref="EvaluationState"/>, so
    /// its term memo and fault budget are shared with whatever else evaluates against the same state. Internal:
    /// visible to <c>TruthWeaver.Policies</c> via <c>InternalsVisibleTo</c>, which shares one state across the
    /// rules of a policy set evaluated for one context, so a term common to several rules is evaluated once
    /// (feature backlog 08). No public API exposes <see cref="EvaluationState"/>; a public caller always gets a
    /// fresh one from the overload above.
    /// </summary>
    /// <param name="context">The application-supplied evaluation context.</param>
    /// <param name="sharedState">The state to evaluate into, shared with other evaluations against the same context.</param>
    /// <param name="services">The service provider for class-based predicates, or <see langword="null"/> for none.</param>
    /// <param name="dataSources">The named data sources this rule's variables read from, or <see langword="null"/> when it has none.</param>
    /// <param name="options">Per-call evaluation options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">A token observed for cooperative cancellation.</param>
    /// <returns>The evaluation's <see cref="Decision"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sharedState"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> has a <see cref="EvaluationOptions.FaultBudget"/> or <see cref="EvaluationOptions.MaxElementEvaluations"/> below 1, or an unusable <see cref="EvaluationOptions.Timeout"/>.</exception>
    /// <exception cref="OperationCanceledException">The caller's token, or <see cref="EvaluationOptions.Timeout"/>, cancelled the evaluation. Neither is recorded as a fault.</exception>
    internal Task<Decision> EvaluateAsync(
        TContext context,
        EvaluationState sharedState,
        IServiceProvider? services = null,
        DataSources? dataSources = null,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return this.EvaluateCoreAsync(
            context,
            services,
            dataSources,
            options,
            sharedState,
            sharedStateRequired: true,
            cancellationToken
        );
    }

    // A token source linked to the caller's token that the timeout cancels, or null when there is no timeout.
    private static CancellationTokenSource? LinkTimeout(TimeSpan? timeout, CancellationToken cancellationToken)
    {
        if (timeout is not { } limit)
        {
            return null;
        }

        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(limit);
        return source;
    }

    // The one place that validates the options, picks the cancellation token (the caller's, or one linked to it that the
    // timeout cancels) and builds the evaluator, for the public overload and the shared-state overload alike. It is async
    // so that a bad argument faults the returned task, as it did when each overload was async itself.
    private async Task<Decision> EvaluateCoreAsync(
        TContext context,
        IServiceProvider? services,
        DataSources? dataSources,
        EvaluationOptions? options,
        EvaluationState? sharedState,
        bool sharedStateRequired,
        CancellationToken cancellationToken
    )
    {
        if (sharedStateRequired)
        {
            ArgumentNullException.ThrowIfNull(sharedState);
            sharedState.AddLibraryLocators(this.libraryLocators);
        }

        EvaluationOptions effectiveOptions = options ?? EvaluationOptions.Default;
        effectiveOptions.ThrowIfInvalid(nameof(options));

        using CancellationTokenSource? timeoutSource = LinkTimeout(effectiveOptions.Timeout, cancellationToken);

        Evaluator<TContext> evaluator = new(
            context,
            services ?? NoServiceProvider.Instance,
            this.registry,
            effectiveOptions,
            timeoutSource?.Token ?? cancellationToken,
            this.logger,
            dataSources,
            sharedState ?? this.NewEvaluationState(effectiveOptions)
        );
        return await evaluator.EvaluateAsync(this.Root).ConfigureAwait(false);
    }

    // The state of one evaluation of this rule alone, with the library locators of its nodes for the trace entries.
    private EvaluationState NewEvaluationState(EvaluationOptions options)
    {
        EvaluationState state = new(options.IncludeTrace);
        state.AddLibraryLocators(this.libraryLocators);
        return state;
    }
}
