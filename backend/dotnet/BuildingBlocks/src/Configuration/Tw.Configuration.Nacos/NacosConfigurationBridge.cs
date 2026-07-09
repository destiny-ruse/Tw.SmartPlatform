using Tw.Configuration;

namespace Tw.Configuration.Nacos;

/// <summary>表示 NacosConfigurationBridge 类型</summary>
public sealed class NacosConfigurationBridge
{
    /// <summary>执行 AcceptChange 操作</summary>
    /// <param name="key">key 参数</param>
    /// <param name="source">source 参数</param>
    /// <returns>AcceptChange 的执行结果</returns>
    public ConfigurationChangeEvent AcceptChange(string key, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return new ConfigurationChangeEvent(key, source, DateTimeOffset.UtcNow);
    }
}
