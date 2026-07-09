namespace Tw.AspNetCore.Mvc.Security;

public static class AntiforgeryPolicy
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET",
        "HEAD",
        "OPTIONS",
        "TRACE"
    };

    public static bool RequiresValidation(string method, string authenticationScheme)
    {
        return !SafeMethods.Contains(method)
            && string.Equals(authenticationScheme, "Cookies", StringComparison.OrdinalIgnoreCase);
    }
}
