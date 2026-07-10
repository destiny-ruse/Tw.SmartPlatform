namespace Tw.Configuration;

/// <summary>
/// 定义ConfigurationGovernance的能力边界
/// </summary>
public interface IConfigurationGovernance
{
    /// <summary>
    /// 判断SourceAllowed是否满足条件
    /// </summary>
    /// <param name="sourceName">用于提供sourceName</param>
    /// <param name="environmentName">用于提供环境Name</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    bool IsSourceAllowed(string sourceName, string environmentName);
}
