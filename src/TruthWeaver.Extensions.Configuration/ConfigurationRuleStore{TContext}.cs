namespace TruthWeaver.Extensions.Configuration;

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;

/// <summary>
/// Holds named rules from an <see cref="IConfiguration"/> section. Each child key of the section is a rule name and its
/// value is the rule text. The store compiles every rule once, keeps the compiled rule in a cache keyed by the hash of
/// the rule text, and swaps the whole set of rules atomically when the configuration reloads (the change token of the
/// section), so a reader sees either the old set or the new set and never a mix.
/// </summary>
/// <remarks>
/// A reload compiles only the rules whose text changed. A rule that does not compile keeps its last good compiled rule
/// live, and the diagnostics go to the logger and to the reload callback. A rule that never compiled is missing, and
/// <see cref="EvaluateAsync"/> then answers Unknown, which <see cref="Decision.IsSatisfied"/> reads as not satisfied.
/// The store reads rule text only; it has no JSON or YAML mode.
/// </remarks>
/// <typeparam name="TContext">The application context type that the rules evaluate against.</typeparam>
public sealed class ConfigurationRuleStore<TContext> : IDisposable
{
    private readonly IConfiguration section;
    private readonly RuleCompiler<TContext> compiler;
    private readonly ILogger logger;
    private readonly Action<IReadOnlyList<RuleReloadReport>>? onReload;
    private readonly Lock reloadLock = new();
    private readonly IDisposable changeRegistration;

    // Replaced as a whole on each reload. The dictionary is never mutated after it is published, so readers need no lock.
    private IReadOnlyDictionary<string, CachedRule> rules = new Dictionary<string, CachedRule>(
        StringComparer.OrdinalIgnoreCase
    );

    /// <summary>Initializes a new instance of the <see cref="ConfigurationRuleStore{TContext}"/> class and loads the rules once.</summary>
    /// <param name="section">The configuration section whose child keys are rule names and whose values are rule text.</param>
    /// <param name="compiler">The compiler that compiles each rule text.</param>
    /// <param name="logger">A logger for load, failure and removal messages, or <see langword="null"/> to log nowhere.</param>
    /// <param name="onReload">
    /// Called after the initial load and after each reload with one report for each rule, including the compiler
    /// diagnostics of a rule that failed. It runs on the thread that raised the change. It must not throw.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="section"/> or <paramref name="compiler"/> is <see langword="null"/>.</exception>
    public ConfigurationRuleStore(
        IConfiguration section,
        RuleCompiler<TContext> compiler,
        ILogger<ConfigurationRuleStore<TContext>>? logger = null,
        Action<IReadOnlyList<RuleReloadReport>>? onReload = null
    )
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(compiler);
        this.section = section;
        this.compiler = compiler;
        this.logger = (ILogger?)logger ?? NullLogger.Instance;
        this.onReload = onReload;

        this.Reload();

        // The token is single use, so OnChange asks for a fresh token each time the callback runs.
        this.changeRegistration = ChangeToken.OnChange(this.section.GetReloadToken, this.Reload);
    }

    /// <summary>Gets the names of the rules that are live now.</summary>
    public IReadOnlyCollection<string> Names => [.. this.rules.Keys];

    /// <summary>Gets the live compiled rule for a name.</summary>
    /// <param name="name">The rule name. The match ignores case, like configuration keys.</param>
    /// <param name="rule">The compiled rule, or <see langword="null"/> when the store holds no rule with that name.</param>
    /// <returns><see langword="true"/> when the rule is live.</returns>
    public bool TryGet(string name, [NotNullWhen(true)] out CompiledRule<TContext>? rule)
    {
        if (this.rules.TryGetValue(name, out CachedRule? cached))
        {
            rule = cached.Rule;
            return true;
        }

        rule = null;
        return false;
    }

    /// <summary>
    /// Evaluates the live rule with the given name. A name that the store does not hold fails closed: the decision is
    /// <see cref="TruthValue.Unknown"/> with no faults, so <see cref="Decision.IsSatisfied"/> is <see langword="false"/>.
    /// </summary>
    /// <param name="name">The rule name.</param>
    /// <param name="context">The application context to evaluate against.</param>
    /// <param name="services">The service provider for predicates, or <see langword="null"/> for none.</param>
    /// <param name="options">The evaluation options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">A token that cancels the evaluation.</param>
    /// <returns>The decision of the live rule, or Unknown when the rule is missing.</returns>
    public Task<Decision> EvaluateAsync(
        string name,
        TContext context,
        IServiceProvider? services = null,
        EvaluationOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!this.TryGet(name, out CompiledRule<TContext>? rule))
        {
            RuleStoreLog.RuleMissing(this.logger, name);
            return Task.FromResult(new Decision(TruthValue.Unknown, []));
        }

        return rule.EvaluateAsync(context, services, options: options, cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.changeRegistration.Dispose();
    }

    private static string HashOf(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private void Reload()
    {
        List<RuleReloadReport> reports = [];
        lock (this.reloadLock)
        {
            IReadOnlyDictionary<string, CachedRule> previous = this.rules;
            Dictionary<string, CachedRule> next = [with(StringComparer.OrdinalIgnoreCase)];
            HashSet<string> seen = [with(StringComparer.OrdinalIgnoreCase)];

            foreach (IConfigurationSection child in this.section.GetChildren())
            {
                // A key with no value (a nested section) is not a rule.
                if (child.Value is not { } text)
                {
                    continue;
                }

                seen.Add(child.Key);
                reports.Add(this.LoadRule(child.Key, text, previous, next));
            }

            foreach (string removed in previous.Keys.Where(name => !seen.Contains(name)))
            {
                RuleStoreLog.RuleRemoved(this.logger, removed);
                reports.Add(new RuleReloadReport(removed, RuleReloadOutcome.Removed, []));
            }

            // One reference write publishes the whole new set.
            this.rules = next;
        }

        this.onReload?.Invoke(reports);
    }

    private RuleReloadReport LoadRule(
        string name,
        string text,
        IReadOnlyDictionary<string, CachedRule> previous,
        Dictionary<string, CachedRule> next
    )
    {
        string hash = HashOf(text);
        previous.TryGetValue(name, out CachedRule? existing);

        // Same text, same compiled rule: no compile.
        if (existing is not null && string.Equals(existing.Hash, hash, StringComparison.Ordinal))
        {
            next[name] = existing;
            return new RuleReloadReport(name, RuleReloadOutcome.Unchanged, []);
        }

        CompilationResult<TContext> result = this.compiler.Compile(text);
        if (result.CompiledRule is { } rule)
        {
            next[name] = new CachedRule(hash, rule);
            RuleStoreLog.RuleLoaded(this.logger, name);
            return new RuleReloadReport(name, RuleReloadOutcome.Loaded, result.Diagnostics);
        }

        // A bad reload keeps the last good rule, if there is one.
        if (existing is not null)
        {
            next[name] = existing;
        }

        RuleStoreLog.RuleFailed(this.logger, name, existing is not null, result.FormatDiagnostics(text));
        return new RuleReloadReport(name, RuleReloadOutcome.Failed, result.Diagnostics);
    }

    private sealed record CachedRule(string Hash, CompiledRule<TContext> Rule);
}
