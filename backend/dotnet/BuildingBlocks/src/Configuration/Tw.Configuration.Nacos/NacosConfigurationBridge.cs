using Tw.Configuration;

namespace Tw.Configuration.Nacos;

/// <summary>
/// 封装NacosConfigurationBridge相关的数据和行为
/// </summary>
public sealed class NacosConfigurationBridge
{
    /// <summary>
    /// 说明AcceptChange在当前类型中的职责
    /// </summary>
    /// <param name="key">用于定位目标数据或缓存项的键</param>
    /// <param name="source">用于提供source</param>
    /// <returns>方法计算得到的文本值</returns>
    public ConfigurationChangeEvent AcceptChange(string key, string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return new ConfigurationChangeEvent(key, source, DateTimeOffset.UtcNow);
    }
}
