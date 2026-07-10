namespace Tw.Configuration;

/// <summary>
/// 封装ConfigurationSource策略相关的数据和行为
/// </summary>
public static class ConfigurationSourcePolicy
{
    /// <summary>
    /// 判断用户SecretsAllowed是否满足条件
    /// </summary>
    /// <param name="environmentName">用于提供环境Name</param>
    /// <returns>条件满足时返回 <see langword="true"/></returns>
    public static bool IsUserSecretsAllowed(string environmentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Local", StringComparison.OrdinalIgnoreCase);
    }
}
