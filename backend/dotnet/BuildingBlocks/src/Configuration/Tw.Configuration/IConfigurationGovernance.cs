namespace Tw.Configuration;

/// <summary>定义 IConfigurationGovernance 契约</summary>
public interface IConfigurationGovernance
{
    /// <summary>执行 IsSourceAllowed 操作</summary>
    /// <param name="sourceName">sourceName 参数</param>
    /// <param name="environmentName">environmentName 参数</param>
    /// <returns>IsSourceAllowed 的执行结果</returns>
    bool IsSourceAllowed(string sourceName, string environmentName);
}
