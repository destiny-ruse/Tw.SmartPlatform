namespace Tw.Gateway;

public static class GatewayHeaderSanitizer
{
    public static IReadOnlyDictionary<string, string> Sanitize(IReadOnlyDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        var sanitized = new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
        foreach (var header in GatewayHeaderPolicy.CallerSuppliedIdentityHeaders)
        {
            sanitized.Remove(header);
        }

        return sanitized;
    }
}
