namespace Tw.Configuration;

/// <summary>表示 ConfigurationSourcePolicy 类型</summary>
public static class ConfigurationSourcePolicy
{
    /// <summary>执行 IsUserSecretsAllowed 操作</summary>
    /// <param name="environmentName">environmentName 参数</param>
    /// <returns>IsUserSecretsAllowed 的执行结果</returns>
    public static bool IsUserSecretsAllowed(string environmentName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);

        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Local", StringComparison.OrdinalIgnoreCase);
    }
}
