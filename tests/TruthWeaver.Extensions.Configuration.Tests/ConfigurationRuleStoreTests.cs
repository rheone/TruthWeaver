namespace TruthWeaver.Extensions.Configuration.Tests;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.DependencyInjection;
using TruthWeaver.Evaluation;
using TruthWeaver.Testing;

/// <summary>
/// The configuration rule store: loading named rules, caching by text hash, atomic swap on reload, keeping the last good
/// rule on a bad reload, and failing closed for a missing rule.
/// </summary>
public sealed class ConfigurationRuleStoreTests
{
    /// <summary>A rule in the section is compiled on construction and evaluates.</summary>
    [Fact]
    public async Task EvaluateAsync_RuleInSection_EvaluatesLiveRule_Test()
    {
        ReloadableSource source = new(("allow", "yes"));
        using ConfigurationRuleStore<int> store = CreateStore(source);

        Decision decision = await store.EvaluateAsync("allow", 0, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(decision.IsSatisfied);
    }

    /// <summary>The name match ignores case, like configuration keys.</summary>
    [Fact]
    public void TryGet_NameInOtherCase_FindsRule_Test()
    {
        ReloadableSource source = new(("Allow", "yes"));
        using ConfigurationRuleStore<int> store = CreateStore(source);

        Assert.True(store.TryGet("allow", out _));
    }

    /// <summary>A configuration reload with changed text swaps in the new rule.</summary>
    [Fact]
    public async Task Reload_ChangedText_SwapsInNewRule_Test()
    {
        ReloadableSource source = new(("rule", "yes"));
        using ConfigurationRuleStore<int> store = CreateStore(source);

        source.Replace(("rule", "no"));
        Decision decision = await store.EvaluateAsync("rule", 0, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(TruthValue.False, decision.Result);
    }

    /// <summary>A reload with the same text keeps the same compiled rule instance, so nothing is compiled again.</summary>
    [Fact]
    public void Reload_UnchangedText_KeepsCompiledRuleInstance_Test()
    {
        ReloadableSource source = new(("rule", "yes"), ("other", "no"));
        using ConfigurationRuleStore<int> store = CreateStore(source);
        store.TryGet("rule", out CompiledRule<int>? before);

        source.Replace(("rule", "yes"), ("other", "yes"));
        store.TryGet("rule", out CompiledRule<int>? after);

        Assert.Same(before, after);
    }

    /// <summary>The reload callback reports a changed rule as loaded and an unchanged rule as unchanged.</summary>
    [Fact]
    public void Reload_Callback_ReportsOutcomePerRule_Test()
    {
        List<RuleReloadReport> reports = [];
        ReloadableSource source = new(("rule", "yes"), ("other", "no"));
        using ConfigurationRuleStore<int> store = CreateStore(source, onReload: batch => reports.AddRange(batch));
        reports.Clear();

        source.Replace(("rule", "yes"), ("other", "yes"));

        Assert.Contains(reports, r => r is { Name: "rule", Outcome: RuleReloadOutcome.Unchanged });
        Assert.Contains(reports, r => r is { Name: "other", Outcome: RuleReloadOutcome.Loaded });
    }

    /// <summary>A reload whose text does not compile keeps the last good rule live.</summary>
    [Fact]
    public async Task Reload_RuleDoesNotCompile_KeepsLastGoodRule_Test()
    {
        ReloadableSource source = new(("rule", "yes"));
        using ConfigurationRuleStore<int> store = CreateStore(source);

        source.Replace(("rule", "yes AND ("));
        Decision decision = await store.EvaluateAsync("rule", 0, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(decision.IsSatisfied);
    }

    /// <summary>A bad reload reports the diagnostics to the callback and an error to the logger.</summary>
    [Fact]
    public void Reload_RuleDoesNotCompile_ReportsDiagnosticsToCallbackAndLogger_Test()
    {
        List<RuleReloadReport> reports = [];
        using ListLoggerProvider logs = new();
        ReloadableSource source = new(("rule", "yes"));
        using ConfigurationRuleStore<int> store = CreateStore(source, logs, batch => reports.AddRange(batch));

        source.Replace(("rule", "yes AND ("));

        RuleReloadReport failed = Assert.Single(reports, r => r.Outcome == RuleReloadOutcome.Failed);
        Assert.NotEmpty(failed.Diagnostics);
        Assert.Contains(logs.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("rule", StringComparison.Ordinal));
    }

    /// <summary>A rule that never compiled is missing, so evaluating it fails closed.</summary>
    [Fact]
    public async Task EvaluateAsync_FirstLoadDoesNotCompile_FailsClosed_Test()
    {
        ReloadableSource source = new(("rule", "yes AND ("));
        using ConfigurationRuleStore<int> store = CreateStore(source);

        Decision decision = await store.EvaluateAsync("rule", 0, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(decision.IsSatisfied);
        Assert.Equal(TruthValue.Unknown, decision.Result);
        Assert.False(store.TryGet("rule", out _));
    }

    /// <summary>A name that is not in the store gives Unknown, which is not satisfied.</summary>
    [Fact]
    public async Task EvaluateAsync_UnknownName_FailsClosed_Test()
    {
        ReloadableSource source = new(("rule", "yes"));
        using ConfigurationRuleStore<int> store = CreateStore(source);

        Decision decision = await store.EvaluateAsync("missing", 0, cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(decision.IsSatisfied);
    }

    /// <summary>A rule removed from the configuration leaves the store.</summary>
    [Fact]
    public void Reload_RuleRemoved_RemovesRule_Test()
    {
        ReloadableSource source = new(("rule", "yes"), ("other", "no"));
        using ConfigurationRuleStore<int> store = CreateStore(source);

        source.Replace(("other", "no"));

        Assert.False(store.TryGet("rule", out _));
        Assert.True(store.TryGet("other", out _));
    }

    /// <summary>After disposal the store no longer reacts to configuration changes.</summary>
    [Fact]
    public void Dispose_ThenReload_DoesNotReloadRules_Test()
    {
        int reloads = 0;
        ReloadableSource source = new(("rule", "yes"));
        using (CreateStore(source, onReload: _ => reloads++))
        {
            // The store is disposed when the block ends; its initial load counted once.
        }

        source.Replace(("rule", "yes"), ("added", "no"));

        Assert.Equal(1, reloads);
    }

    /// <summary>The container extension builds a store over a section path.</summary>
    [Fact]
    public async Task AddTruthWeaverRuleStore_SectionPath_ResolvesStoreFromContainer_Test()
    {
        ReloadableSource source = new(("rule", "yes"));
        IConfiguration configuration = new ConfigurationBuilder().Add(source.AsConfigurationSource()).Build();
        ServiceCollection services = new();
        services.AddSingleton(configuration);
        services.AddTruthWeaver<int>(Register);
        services.AddTruthWeaverRuleStore<int>("Rules");
        await using ServiceProvider provider = services.BuildServiceProvider();

        ConfigurationRuleStore<int> store = provider.GetRequiredService<ConfigurationRuleStore<int>>();

        Assert.True(store.TryGet("rule", out _));
    }

    private static void Register(TruthWeaver.Registry.PredicateRegistryBuilder<int> builder)
    {
        (
            TruthWeaver.Abstractions.PredicateSchema yesSchema,
            Func<int, PredicateArguments, CancellationToken, ValueTask<TruthValue>>? yes
        ) = FakePredicates.Returning<int>("yes", true);
        (
            TruthWeaver.Abstractions.PredicateSchema noSchema,
            Func<int, PredicateArguments, CancellationToken, ValueTask<TruthValue>>? no
        ) = FakePredicates.Returning<int>("no", false);
        builder.Add(yesSchema, yes);
        builder.Add(noSchema, no);
    }

    private static ConfigurationRuleStore<int> CreateStore(
        ReloadableSource source,
        ListLoggerProvider? logs = null,
        Action<IReadOnlyList<RuleReloadReport>>? onReload = null
    )
    {
        IConfiguration configuration = new ConfigurationBuilder().Add(source.AsConfigurationSource()).Build();
        TruthWeaver.Registry.PredicateRegistryBuilder<int> builder =
            TruthWeaver.Registry.PredicateRegistry<int>.CreateBuilder();
        Register(builder);
        RuleCompiler<int> compiler = new(builder.Build());
        return new ConfigurationRuleStore<int>(
            configuration.GetSection("Rules"),
            compiler,
            logs?.CreateLogger<ConfigurationRuleStore<int>>(),
            onReload
        );
    }
}
