namespace Tw.Core.Configuration;

/// <summary>
/// Identifies the configuration section that should bind to an options type.
/// </summary>
/// <param name="name">The non-empty configuration section name used by callers during options binding.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ConfigurationSectionAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the configuration section name associated with the decorated options type.
    /// </summary>
    public string Name { get; } = Tw.Core.Check.NotNullOrWhiteSpace(name);
}
