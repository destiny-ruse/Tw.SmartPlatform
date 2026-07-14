namespace Tw.Configuration.Json;

/// <summary>
/// 表示 JSON 配置路径违反安全治理规则
/// </summary>
public sealed class ConfigurationPathException : Exception
{
    /// <summary>
    /// 使用可供调用方诊断的路径治理失败原因创建异常
    /// </summary>
    /// <param name="message">不包含敏感配置内容的失败原因</param>
    public ConfigurationPathException(string message)
        : base(message)
    {
    }
}
