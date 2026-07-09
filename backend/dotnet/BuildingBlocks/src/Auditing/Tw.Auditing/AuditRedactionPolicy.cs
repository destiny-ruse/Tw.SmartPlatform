namespace Tw.Auditing;

/// <summary>表示 AuditRedactionPolicy 类型</summary>
public static class AuditRedactionPolicy
{
    /// <summary>执行 Redact 操作</summary>
    /// <param name="details">details 参数</param>
    /// <returns>Redact 的执行结果</returns>
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
