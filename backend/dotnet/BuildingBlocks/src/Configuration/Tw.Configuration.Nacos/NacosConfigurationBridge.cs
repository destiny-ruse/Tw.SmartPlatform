using Tw.Configuration;

namespace Tw.Configuration.Nacos;

public sealed class NacosConfigurationBridge
{
    public ConfigurationChangeEvent AcceptChange(string key, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return new ConfigurationChangeEvent(key, source, DateTimeOffset.UtcNow);
    }
}
