namespace TruthWeaver.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.DependencyInjection;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>Ticket 12: scoped DI predicate resolution and DI registration extensions.</summary>
public sealed class ScopedResolutionAndRegistrationTests
{
    /// <summary>A class based predicate registered by type evaluates correctly.</summary>
    [Fact]
    public async Task ClassPredicate_RegisteredByType_EvaluatesCorrectly_Test()
    {
        IScopedFlag flag = Substitute.For<IScopedFlag>();
        flag.Value.Returns(true);
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(ScopedFlagPredicate)).Returns(new ScopedFlagPredicate(flag));

        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<ScopedFlagPredicate>().Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            services,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>A class based predicate is resolved fresh from the service provider on every evaluation.</summary>
    [Fact]
    public async Task ClassPredicate_Resolution_IsFreshOnEveryEvaluation_Test()
    {
        List<ScopedFlagPredicate> resolvedInstances = [];
        IServiceProvider services = Substitute.For<IServiceProvider>();
        services
            .GetService(typeof(ScopedFlagPredicate))
            .Returns(_ =>
            {
                IScopedFlag flag = Substitute.For<IScopedFlag>();
                flag.Value.Returns(true);
                ScopedFlagPredicate instance = new(flag);
                resolvedInstances.Add(instance);
                return instance;
            });

        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<ScopedFlagPredicate>().Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        await rule.EvaluateAsync(new RuleTestContext(), services, cancellationToken: TestContext.Current.CancellationToken);
        await rule.EvaluateAsync(new RuleTestContext(), services, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, resolvedInstances.Count);
        Assert.NotSame(resolvedInstances[0], resolvedInstances[1]);
    }

    /// <summary>A class based predicate with no service registration is absorbed as a fault naming the unresolved type.</summary>
    [Fact]
    public async Task ClassPredicate_UnresolvedService_IsAbsorbedAsFaultNamingType_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<ScopedFlagPredicate>().Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            TestSupport.EmptyServiceProvider.Instance,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.Equal("scopedFlag", fault.Term.PredicateName);
        InvalidOperationException exception = Assert.IsType<InvalidOperationException>(fault.Exception);
        Assert.Contains(nameof(ScopedFlagPredicate), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>A rule built from a lambda predicate evaluates with only a context: no services, data sources or options are needed.</summary>
    [Fact]
    public async Task EvaluateAsync_WithOnlyAContext_RunsARuleThatNeedsNothingElse_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>
                .CreateBuilder()
                .Add(
                    PredicateSchema.NoArguments("always", "Always", "Always true."),
                    (_, _, _) => ValueTask.FromResult(TruthValue.True)
                )
                .Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("always").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>A null service provider acts as an empty one: a class-based predicate yields Unknown plus a fault, never an exception.</summary>
    [Fact]
    public async Task EvaluateAsync_WithNullServices_FaultsAClassBasedPredicateInsteadOfThrowing_Test()
    {
        RuleCompiler<RuleTestContext> compiler = new(
            PredicateRegistry<RuleTestContext>.CreateBuilder().Add<ScopedFlagPredicate>().Build()
        );
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            services: null,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.Unknown, decision.Result);
        Fault fault = Assert.Single(decision.Faults);
        Assert.IsType<InvalidOperationException>(fault.Exception);
    }

    /// <summary>The service collection extension wires a compiler and rule using only container resolved services.</summary>
    [Fact]
    public async Task ServiceCollection_Extension_WiresCompilerFromContainer_Test()
    {
        ServiceCollection services = new();
        services.AddScoped<IScopedFlag>(_ => new StubScopedFlag(true));

        // The predicate registry only records ScopedFlagPredicate's type; the host application's
        // container must separately register that concrete type as a resolvable service, exactly as
        // it would for any other class-based dependency (ADR-0002).
        services.AddScoped<ScopedFlagPredicate>();
        services.AddTruthWeaver<RuleTestContext>(builder => builder.Add<ScopedFlagPredicate>());

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        RuleCompiler<RuleTestContext> compiler = scope.ServiceProvider.GetRequiredService<RuleCompiler<RuleTestContext>>();
        CompiledRule<RuleTestContext> rule = compiler.Compile("scopedFlag").CompiledRule!;

        Decision decision = await rule.EvaluateAsync(
            new RuleTestContext(),
            scope.ServiceProvider,
            cancellationToken: TestContext.Current.CancellationToken
        );

        Assert.Equal(TruthValue.True, decision.Result);
    }

    /// <summary>The service collection extension threads supplied compiler options through to the resolved compiler.</summary>
    [Fact]
    public void ServiceCollection_Extension_ThreadsCompilerOptions_Test()
    {
        ServiceCollection services = new();
        services.AddTruthWeaver<RuleTestContext>(_ => { }, new CompilerOptions(Mode: CompilationMode.Lenient));

        using ServiceProvider provider = services.BuildServiceProvider();
        RuleCompiler<RuleTestContext> compiler = provider.GetRequiredService<RuleCompiler<RuleTestContext>>();

        // Under Strict (the default), an unregistered predicate name is a compile Error and
        // CompiledRule is null; under Lenient it compiles successfully as an always-Unknown term.
        // Observing the latter confirms the supplied options — not the defaults — reached the compiler.
        CompilationResult<RuleTestContext> result = compiler.Compile("noSuchPredicate");

        Assert.NotNull(result.CompiledRule);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>The service collection extension resolves a registered logger and the compiler uses it.</summary>
    [Fact]
    public void ServiceCollection_Extension_ResolvesRegisteredLogger_Test()
    {
        ILogger<RuleCompiler<RuleTestContext>> logger = Substitute.For<ILogger<RuleCompiler<RuleTestContext>>>();
        logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

        ServiceCollection services = new();
        services.AddSingleton(logger);
        services.AddTruthWeaver<RuleTestContext>(_ => { });

        using ServiceProvider provider = services.BuildServiceProvider();
        RuleCompiler<RuleTestContext> compiler = provider.GetRequiredService<RuleCompiler<RuleTestContext>>();

        compiler.Compile("noSuchPredicate");

        LoggerTestExtensions.LoggedCall call = Assert.Single(logger.GetLoggedCalls());
        Assert.Equal(LogLevel.Error, call.Level);
    }

    /// <summary>The service collection extension resolves and compiles without error when no logger is registered.</summary>
    [Fact]
    public void ServiceCollection_Extension_CompilesWithoutRegisteredLogger_Test()
    {
        ServiceCollection services = new();
        services.AddTruthWeaver<RuleTestContext>(builder => builder.Add<ScopedFlagPredicate>());
        services.AddScoped<IScopedFlag>(_ => new StubScopedFlag(true));
        services.AddScoped<ScopedFlagPredicate>();

        using ServiceProvider provider = services.BuildServiceProvider();
        RuleCompiler<RuleTestContext> compiler = provider.GetRequiredService<RuleCompiler<RuleTestContext>>();

        CompilationResult<RuleTestContext> result = compiler.Compile("scopedFlag");

        Assert.NotNull(result.CompiledRule);
        Assert.Empty(result.Diagnostics);
    }

    private sealed class StubScopedFlag(bool value) : IScopedFlag
    {
        public bool Value { get; } = value;
    }
}
