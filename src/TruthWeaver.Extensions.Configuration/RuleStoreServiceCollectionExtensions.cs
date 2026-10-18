namespace TruthWeaver.Extensions.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TruthWeaver.Compilation;

/// <summary><see cref="IServiceCollection"/> extensions that register a <see cref="ConfigurationRuleStore{TContext}"/>.</summary>
public static class RuleStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="ConfigurationRuleStore{TContext}"/> over the configuration section at
    /// <paramref name="sectionPath"/>. The container must hold an <see cref="IConfiguration"/> and a
    /// <see cref="RuleCompiler{TContext}"/> (see <c>AddTruthWeaver</c>). The container disposes the store.
    /// </summary>
    /// <typeparam name="TContext">The application context type that the rules evaluate against.</typeparam>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="sectionPath">The path of the configuration section that holds the rules, such as <c>Rules</c>.</param>
    /// <param name="onReload">A callback for each load and reload, or <see langword="null"/> for none. It must not throw.</param>
    /// <returns><paramref name="services"/>, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="sectionPath"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddTruthWeaverRuleStore<TContext>(
        this IServiceCollection services,
        string sectionPath,
        Action<IReadOnlyList<RuleReloadReport>>? onReload = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(sectionPath);
        services.AddSingleton(sp => new ConfigurationRuleStore<TContext>(
            sp.GetRequiredService<IConfiguration>().GetSection(sectionPath),
            sp.GetRequiredService<RuleCompiler<TContext>>(),
            sp.GetService<ILogger<ConfigurationRuleStore<TContext>>>(),
            onReload
        ));
        return services;
    }
}
