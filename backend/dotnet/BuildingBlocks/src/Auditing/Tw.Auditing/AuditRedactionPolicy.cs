namespace Tw.Auditing;

public static class AuditRedactionPolicy
{
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
