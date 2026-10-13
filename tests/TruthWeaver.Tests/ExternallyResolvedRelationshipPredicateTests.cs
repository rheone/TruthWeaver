namespace TruthWeaver.Tests;

using NSubstitute;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// Externally-resolved-value predicates tickets 03-06: worked-example tests for the pattern where a
/// rule-text literal argument and/or a <c>TContext</c>-supplied value is a key resolved live through a
/// constructor-injected service, rather than a value already ready to use.
/// </summary>
/// <remarks>
/// Term identity for this pattern's literal argument is an ordinary case of the general term-identity
/// rule already covered end to end by <see cref="ArgumentsAndMemoizationTests"/> (same predicate name +
/// same literal argument value = same term, regardless of what a class-based predicate's injected
/// dependency happens to resolve it to at evaluation time) — nothing about external resolution changes
/// that rule, so it is not re-asserted here.
/// </remarks>
public sealed class ExternallyResolvedRelationshipPredicateTests
{
    private static readonly Guid ResourceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ActualManagerId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid DifferentManagerId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>A candidate resolved as the resource's actual manager evaluates to true.</summary>
    [Fact]
    public async Task Evaluate_CandidateResolvedAsActualManager_ReturnsTrue_Test()
    {
        IServiceProvider services = ServicesResolvingManagerAs(ActualManagerId);
        CompiledRule<ResourceContext> rule = CompileIsManagedByCandidate();

        Decision decision = await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>A candidate resolved as a different manager evaluates to false.</summary>
    [Fact]
    public async Task Evaluate_CandidateResolvedAsDifferentManager_ReturnsFalse_Test()
    {
        IServiceProvider services = ServicesResolvingManagerAs(DifferentManagerId);
        CompiledRule<ResourceContext> rule = CompileIsManagedByCandidate();

        Decision decision = await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>The injected lookup service is resolved fresh from the service provider on every evaluation.</summary>
    [Fact]
    public async Task Resolution_InjectedService_ResolvedFreshEachEvaluation_Test()
    {
        List<IsManagedByCandidate> resolvedInstances = [];
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services
            .GetService(typeof(IsManagedByCandidate))
            .Returns(_ =>
            {
                IManagerLookupService lookup = Substitute.For<IManagerLookupService>();
                lookup.ResolveManagerIdAsync(ResourceId, Arg.Any<CancellationToken>()).Returns(ActualManagerId);
                IsManagedByCandidate instance = new(lookup);
                resolvedInstances.Add(instance);
                return instance;
            });

        CompiledRule<ResourceContext> rule = CompileIsManagedByCandidate();

        await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );
        await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(2, resolvedInstances.Count);
        Assert.NotSame(resolvedInstances[0], resolvedInstances[1]);
    }

    /// <summary>An external lookup failure is absorbed as a fault and the term evaluates to unknown.</summary>
    [Fact]
    public async Task Evaluate_LookupFailure_IsAbsorbedAsFaultAndUnknown_Test()
    {
        IServiceProvider services = ServicesWithThrowingManagerLookup();
        CompiledRule<ResourceContext> rule = CompileIsManagedByCandidate();

        Decision decision = await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(decision.IsSatisfied);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("isManagedByCandidate", fault.Term.PredicateName);
        Assert.IsType<TimeoutException>(fault.Exception);
    }

    /// <summary>An external lookup failure still lets an otherwise determinate rule reach a decision.</summary>
    [Fact]
    public async Task Evaluate_LookupFailure_StillReachesDecision_Test()
    {
        IServiceProvider services = ServicesWithThrowingManagerLookup();
        RuleCompiler<ResourceContext> compiler = new(
            PredicateRegistry<ResourceContext>.CreateBuilder().Add<IsManagedByCandidate>().Build()
        );
        CompiledRule<ResourceContext> rule = compiler
            .Compile("isManagedByCandidate(candidateManagerId: \"22222222-2222-2222-2222-222222222222\") OR true")
            .CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new ResourceContext(ResourceId),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        // "Unknown OR True" is True (CONTEXT.md#failure-model-summary): a fault that can't affect the
        // outcome must not turn a transient lookup blip into a denial.
        Assert.Equal(TruthValue.True, decision.Result);
        Assert.True(decision.IsSatisfied);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("isManagedByCandidate", fault.Term.PredicateName);
    }

    // Deliberately the same shape as the two-sided IsManagedByCandidate tests above (context anchor +
    // literal key + injected live resolution + comparison) — just with a String key rather than a
    // Guid, and a non-identity Decimal comparison value read straight off the context rather than a
    // second resolved side. The point is the pattern's generality, not a different pattern.

    /// <summary>A context amount within the resolved limit evaluates to true.</summary>
    [Fact]
    public async Task Evaluate_AmountWithinResolvedLimit_ReturnsTrue_Test()
    {
        IServiceProvider services = ServicesResolvingLimitAs(500m);
        CompiledRule<PurchaseRequestContext> rule = CompileIsWithinBudget();

        Decision decision = await rule.EvaluateAsync(
            new PurchaseRequestContext(Amount: 500m),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>A context amount above the resolved limit evaluates to false.</summary>
    [Fact]
    public async Task Evaluate_AmountExceedingResolvedLimit_ReturnsFalse_Test()
    {
        IServiceProvider services = ServicesResolvingLimitAs(500m);
        CompiledRule<PurchaseRequestContext> rule = CompileIsWithinBudget();

        Decision decision = await rule.EvaluateAsync(
            new PurchaseRequestContext(Amount: 500.01m),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    // The simplest instance of the same pattern family as the two tests above: a literal key +
    // injected live resolution, but with no second value and no context dependency at all — the
    // resolved value *is* the answer, not a different pattern.

    /// <summary>A flag resolved as true evaluates to true.</summary>
    [Fact]
    public async Task Evaluate_FlagResolvedTrue_ReturnsTrue_Test()
    {
        IServiceProvider services = ServicesResolvingFlagAs(true);
        CompiledRule<RuleTestContext> rule = CompileIsFeatureEnabled();

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>A flag resolved as false evaluates to false.</summary>
    [Fact]
    public async Task Evaluate_FlagResolvedFalse_ReturnsFalse_Test()
    {
        IServiceProvider services = ServicesResolvingFlagAs(false);
        CompiledRule<RuleTestContext> rule = CompileIsFeatureEnabled();

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>The feature flag lookup service is resolved fresh from the service provider on every evaluation.</summary>
    [Fact]
    public async Task Resolution_FeatureFlagService_ResolvedFreshEachEvaluation_Test()
    {
        List<IsFeatureEnabled> resolvedInstances = [];
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services
            .GetService(typeof(IsFeatureEnabled))
            .Returns(_ =>
            {
                IFeatureFlagService flags = Substitute.For<IFeatureFlagService>();
                flags.IsEnabledAsync("new-checkout", Arg.Any<CancellationToken>()).Returns(true);
                IsFeatureEnabled instance = new(flags);
                resolvedInstances.Add(instance);
                return instance;
            });

        CompiledRule<RuleTestContext> rule = CompileIsFeatureEnabled();

        await rule.EvaluateAsync(new RuleTestContext(), services, cancellationToken: TestContext.Current.CancellationToken);
        await rule.EvaluateAsync(new RuleTestContext(), services, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, resolvedInstances.Count);
        Assert.NotSame(resolvedInstances[0], resolvedInstances[1]);
    }

    private static IServiceProvider ServicesResolvingManagerAs(Guid resolvedManagerId)
    {
        IManagerLookupService lookup = Substitute.For<IManagerLookupService>();
        lookup.ResolveManagerIdAsync(ResourceId, Arg.Any<CancellationToken>()).Returns(resolvedManagerId);

        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IsManagedByCandidate)).Returns(new IsManagedByCandidate(lookup));
        return services;
    }

    private static CompiledRule<ResourceContext> CompileIsManagedByCandidate()
    {
        RuleCompiler<ResourceContext> compiler = new(
            PredicateRegistry<ResourceContext>.CreateBuilder().Add<IsManagedByCandidate>().Build()
        );
        return compiler
            .Compile("isManagedByCandidate(candidateManagerId: \"22222222-2222-2222-2222-222222222222\")")
            .CompiledRule!;
    }

    private static IServiceProvider ServicesWithThrowingManagerLookup()
    {
        IManagerLookupService lookup = Substitute.For<IManagerLookupService>();
        lookup
            .ResolveManagerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns<Guid>(_ => throw new TimeoutException("The manager lookup service timed out."));

        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IsManagedByCandidate)).Returns(new IsManagedByCandidate(lookup));
        return services;
    }

    private static IServiceProvider ServicesResolvingLimitAs(decimal resolvedLimit)
    {
        IBudgetLookupService lookup = Substitute.For<IBudgetLookupService>();
        lookup.ResolveLimitAsync("CC-100", Arg.Any<CancellationToken>()).Returns(resolvedLimit);

        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IsWithinBudget)).Returns(new IsWithinBudget(lookup));
        return services;
    }

    private static CompiledRule<PurchaseRequestContext> CompileIsWithinBudget()
    {
        RuleCompiler<PurchaseRequestContext> compiler = new(
            PredicateRegistry<PurchaseRequestContext>.CreateBuilder().Add<IsWithinBudget>().Build()
        );
        return compiler.Compile("isWithinBudget(costCenterCode: \"CC-100\")").CompiledRule!;
    }

    private static IServiceProvider ServicesResolvingFlagAs(bool resolvedValue)
    {
        IFeatureFlagService flags = Substitute.For<IFeatureFlagService>();
        flags.IsEnabledAsync("new-checkout", Arg.Any<CancellationToken>()).Returns(resolvedValue);

        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IsFeatureEnabled)).Returns(new IsFeatureEnabled(flags));
        return services;
    }

    private static CompiledRule<RuleTestContext> CompileIsFeatureEnabled()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<IsFeatureEnabled>().Build()
        );
        return compiler.Compile("isFeatureEnabled(flagKey: \"new-checkout\")").CompiledRule!;
    }
}
