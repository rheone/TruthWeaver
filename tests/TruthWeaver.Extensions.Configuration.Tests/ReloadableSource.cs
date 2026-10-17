namespace TruthWeaver.Extensions.Configuration.Tests;

using Microsoft.Extensions.Configuration;

/// <summary>A configuration provider whose rules a test replaces, which raises the reload change token like a file provider.</summary>
internal sealed class ReloadableSource
{
    private readonly Provider provider = new();

    public ReloadableSource(params (string Name, string Text)[] rules)
    {
        this.provider.Load(rules);
    }

    /// <summary>Replaces every rule and raises the change token.</summary>
    public void Replace(params (string Name, string Text)[] rules)
    {
        this.provider.Load(rules);
        this.provider.Raise();
    }

    public IConfigurationSource AsConfigurationSource()
    {
        return new Source(this.provider);
    }

    private sealed class Source(Provider provider) : IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return provider;
        }
    }

    private sealed class Provider : ConfigurationProvider
    {
        public void Load(IEnumerable<(string Name, string Text)> rules)
        {
            this.Data = rules.ToDictionary(
                rule => "Rules:" + rule.Name,
                rule => (string?)rule.Text,
                StringComparer.OrdinalIgnoreCase
            );
        }

        public void Raise()
        {
            this.OnReload();
        }
    }
}
