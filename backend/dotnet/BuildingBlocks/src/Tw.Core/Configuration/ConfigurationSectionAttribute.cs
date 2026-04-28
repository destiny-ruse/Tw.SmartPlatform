namespace Tw.Core.Configuration;

public sealed class ConfigurationSectionAttribute(string name) : Attribute
{
    public string Name { get; } = Tw.Core.Check.NotNullOrWhiteSpace(name);
}
