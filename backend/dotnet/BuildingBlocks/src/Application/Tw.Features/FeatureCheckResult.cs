namespace Tw.Features;

/// <summary>
/// Feature 检查结果
/// </summary>
/// <param name="Enabled">Feature 是否启用</param>
/// <param name="Code">稳定结果码</param>
/// <param name="Message">安全结果消息</param>
public sealed record FeatureCheckResult(bool Enabled, string Code, string Message)
{
    /// <summary>
    /// 创建启用结果
    /// </summary>
    /// <returns>启用结果</returns>
    public static FeatureCheckResult EnabledResult() => new(true, "SYSTEM:000000", "success");

    /// <summary>
    /// 创建禁用结果
    /// </summary>
    /// <param name="feature">Feature 名称</param>
    /// <returns>禁用结果</returns>
    public static FeatureCheckResult Disabled(string feature) => new(false, "FEATURE:000001", $"功能未启用：{feature}");
}
