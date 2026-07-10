namespace Tw.Auditing;

/// <summary>
/// 封装审计Redaction策略相关的数据和行为
/// </summary>
public static class AuditRedactionPolicy
{
    /// <summary>
    /// 说明Redact在当前类型中的职责
    /// </summary>
    /// <param name="details">用于提供details</param>
    /// <returns>方法计算得到的文本值</returns>
    public static string? Redact(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
        {
            return details;
        }

        return details.Contains("Password=", StringComparison.OrdinalIgnoreCase)
            || details.Contains("ConnectionStrings:", StringComparison.OrdinalIgnoreCase)
            ? "***"
            : details;
    }
}
